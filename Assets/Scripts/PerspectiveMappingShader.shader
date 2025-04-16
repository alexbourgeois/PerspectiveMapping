Shader "CustomEffects/PerspectiveMappingShader"
{
    HLSLINCLUDE

        #if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12) || defined(SHADER_API_D3D9)
            #define IS_DIRECTX 1
        #elif defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
            #define IS_OPENGL 1
        #elif defined(SHADER_API_VULKAN)
            #define IS_VULKAN 1
        #endif

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        
        float _ShowGrid;
        float _GridSize;
        float _AspectRatio;
        float _LineWidth;
        
        float _TestPatternTexCoeff;
        TEXTURE2D(_TestPatternTex);
        SAMPLER(sampler_TestPatternTex);

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
            // N = _GridSize : en grid space, la taille d'une cellule vaut 1.
            float N = _GridSize;
            
            // --- GRILLE ---
            // Passage en grid space en tenant compte de l'aspect :
            float2 gridCoord = uv * float2( N * _AspectRatio, N );
            // On récupère la position dans la cellule (valeurs entre 0 et 1)
            float2 f = frac( gridCoord );
            // d = distance minimale à un bord (soit vers 0 ou 1)
            float2 d = min( f, 1.0 - f );
            // On souhaite tracer une ligne si la distance est inférieure à _LineWidth/2.
            // La fonction step(edge, x) retourne 0 si x < edge, et 1 sinon.
            float lineX = 1.0 - step( _LineWidth * 0.5, d.x );
            float lineY = 1.0 - step( _LineWidth * 0.5, d.y );
            // Si l'une des deux directions présente un trait (valeur 1), on considère que la cellule est sur une ligne.
            // On définit alors grid = 0 dans la zone de trait, et grid = 1 en fond.
            float grid = 1.0 - max( lineX, lineY );
            
            // --- CROIX ---
            // Première diagonale (de bas à gauche vers haut à droite) :
            // La distance perpendiculaire à la droite passant par le centre est :
            float d1 = abs(uv.y - uv.x ) / 1.4142135624;
            // On dessine la ligne si d1 < _LineWidth/2
            float l1 = step( _LineWidth / 20.0, d1 );  // l1 = 0 sur le trait, 1 en fond.
            
            // Seconde diagonale (de haut à gauche vers bas à droite) :
            float d2 = abs( ( uv.x + uv.y - 1.0 ) ) / 1.4142135624;
            float l2 = step( _LineWidth / 20.0, d2 );
            
            // La croix sera présente (valeur 0) si l'une des deux diagonales est en mode "trait"
            float cross = l1 * l2;
            
            // --- CERCLE ---
            float2 pos = uv * float2( N * _AspectRatio, N );
            float2 dim = float2( N * _AspectRatio, N );
            float minSize = min( dim.x , dim.y );

            // La distance au centre (ici, on calcule à partir d'une position centrée)
            float2 fromCenter = dim - pos * 2.0;
            float radius = length( fromCenter );

            float circleThreshold = _LineWidth / (0.125 * N);
            float circle = step( circleThreshold, abs(radius - minSize) );
            
            // --- COMBINAISON ---
            // Pour chaque élément, 0 signifie "trait" et 1 "fond".
            // Le produit grid * cross * circle vaut 1 uniquement en fond (aucun trait).
            // On retourne alors 1 - produit, ce qui donne 1 (blanc) sur les traits et 0 (noir) en fond.
            return 1.0 - saturate( grid * cross * circle );
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
            
            #if IS_OPENGL
                //uvTransformed.x = remap(uvTransformed.x, 0,1,-1,1);
                //uvTransformed.y = remap(uvTransformed.y, 0,1,-1,1); 
            #endif
            
            #if IS_VULKAN
                //uvTransformed.y = -uvTransformed.y;
            #endif
            color.rgb = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvTransformed).rgb * (1.0-_TestPatternTexCoeff);
            color.rgb += SAMPLE_TEXTURE2D(_TestPatternTex, sampler_LinearClamp, uvTransformed).rgb *_TestPatternTexCoeff;

            float grid = ComputeGrid( uvTransformed ) * _ShowGrid;
            color = color + grid;
            
            #if IS_DIRECTX || IS_VULKAN
                if(uvTransformed.x<0 || uvTransformed.y<0 || uvTransformed.x>1 || uvTransformed.y>1)
                    color.rgb =_ClearColor;
            #endif

            //TODO fix ANTIALIASING 
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
