Shader "CustomEffects/PerspectiveMappingShader"
{
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float2 _BotLeftCorner;
        float2 _BotRightCorner;
        float2 _TopLeftCorner;
        float2 _TopRightCorner;
        float _ShowGrid;
        float2 _GridSize;

        float4 _ClearColor = (1.0,0,0,1.0);

        float4x4 _HomographyMatrix;
        
        float remap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            // S'assurer que nous ne divisons pas par zéro
            float oldRange = oldMax - oldMin;
            if (oldRange == 0)
                return newMin;
            
            float newRange = newMax - newMin;
            
            // Normaliser la valeur entre 0 et 1, puis la mapper sur la nouvelle plage
            float normalizedValue = (value - oldMin) / oldRange;
            return newMin + (normalizedValue * newRange);
        }

        float ComputeGrid( float2 uv )
        {
            // Grid.
            float2 pos = uv * _GridSize;
            float2 offPos = pos + 0.5;
            float2 f  = abs( frac( offPos ) - 0.5 );		// Frac is the fractoinal part, like f = v - floor(v)
            float2 df = fwidth( offPos ) * 1.2;	// Fwidth (fragment width): sum of approximate window-space partial derivatives magnitudes
            float2 g  = smoothstep( -df, df, f );			// Grid

            // Cross.
            float l1 = ( uv.y - uv.x ) / 1.4142135624;
            l1 = saturate( smoothstep( -df, df, abs( l1 * 10 ) ) );
            float l2 = ( uv.x + uv.y - 1 ) / 1.4142135624;
            l2 = saturate( smoothstep( -df, df, abs( l2 * 10 ) ) );  

            // Circle.
            float minSize = _GridSize.x > _GridSize.y ? _GridSize.y : _GridSize.x;
            float2 fromCenter = _GridSize-pos*2;
            float radius = length( fromCenter );
            df *= 2; // Thicker stroke.
            float c = saturate( smoothstep( -df, df, abs(radius - minSize) ) );

            return 1 - saturate( g.x * g.y * c * l1 * l2 );
        }

        
        float4 ClearBackground(Varyings input) : SV_Target  {
            return _ClearColor;
        }

        Varyings VertexMethod(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
            float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

            output.positionCS = mul(_HomographyMatrix, pos);
            output.texcoord   = DYNAMIC_SCALING_APPLY_SCALEBIAS(uv);

            return output;
        }


        float4 PerspectiveMapping (Varyings input) : SV_Target
        {

            float3 color = (0,0,0);

            float2 uvTransformed = input.texcoord.xy;
            uvTransformed.x = remap(uvTransformed.x, 0.5,1,0,1);
            uvTransformed.y = remap(uvTransformed.y, 0,0.5,0,1);

            color.rgb = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvTransformed).rgb;

            float grid = ComputeGrid( uvTransformed ) * _ShowGrid;
            color = color + grid;

            if(uvTransformed.x<0 || uvTransformed.y<0 || uvTransformed.x>1 || uvTransformed.y>1)
                color.rgb =_ClearColor;

            // ANTIALIASING
            float2 pos = uvTransformed + 0.5;
			float2 f  = abs( frac( pos ) - 0.5 );
			float2 df = fwidth( pos ) * 0.7;
			float2 e  = smoothstep( -df, df, f );
            
			color.rgb = lerp( color.rgb, _ClearColor, 1-saturate( e.x * e.y ) );


            return float4(color.rgb, 1);
        }

        float4 CopyTexture (Varyings input) : SV_Target
        {
            return float4(SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb, 1);
        }
    
    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off

        Pass
        {
            Name "ClearBackgroundPass"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment ClearBackground;
            
            ENDHLSL
        }

        Pass
        {
            Name "PerspectiveMappingPass"

            HLSLPROGRAM
            
            #pragma vertex VertexMethod
            #pragma fragment PerspectiveMapping
            
            ENDHLSL
        }
        
        Pass
        {
            Name "PerspectiveMappingPassRewrite"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment CopyTexture
            
            ENDHLSL
        }
    }
}