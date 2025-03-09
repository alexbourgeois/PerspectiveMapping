using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

[Serializable]
public class PerspectiveMappingSettings
{
    public Vector2 bottomLeftCorner = Vector2.zero;
    public Vector2 topLeftCorner = Vector2.up;
    public Vector2 topRightCorner = Vector2.up + Vector2.right;
    public Vector2 bottomRightCorner = Vector2.right;

    
    public readonly Vector2[] sourcePoints = new []{ //lower-left, upper-left, upper-right, lower-right.
		new Vector2( 0, 1 ), //top left
		new Vector2( 0, 0 ), //bot left
		new Vector2( 1, 0 ), //bot right
		new Vector2( 1, 1 ), //top right
	};

    Matrix4x4 matrix;
}

public class PerspectiveMappingFeature : ScriptableRendererFeature
{

    [SerializeField] private PerspectiveMappingSettings settings;
    [SerializeField] private Shader shader;
    private Material material;
    private PerspectiveMappingRenderPass perspectiveMappingRenderPass;

    public override void Create()
    {
        if (shader == null)
        {
            return;
        }
        material = new Material(shader);
        perspectiveMappingRenderPass = new PerspectiveMappingRenderPass(material, settings);

        perspectiveMappingRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (perspectiveMappingRenderPass == null)
        { 
            return;
        }                
        if (renderingData.cameraData.cameraType == CameraType.Game) // Use this to select which camera
        {
            renderer.EnqueuePass(perspectiveMappingRenderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }
}

