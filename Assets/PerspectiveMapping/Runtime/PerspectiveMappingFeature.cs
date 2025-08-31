using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

public class PerspectiveMappingFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    private Material material;
    private PerspectiveMappingRenderPass perspectiveMappingRenderPass;

    public override void Create()
    {
        shader = Shader.Find("PerspectiveMapping/PerspectiveMappingShader");
        if (shader == null)
        {
            Debug.LogError("[PerspectiveMappingFeature] Shader not found !");
            return;
        }
        material = new Material(shader);
        perspectiveMappingRenderPass = new PerspectiveMappingRenderPass(material);

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

