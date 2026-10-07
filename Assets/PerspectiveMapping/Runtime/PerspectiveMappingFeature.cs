using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

public class PerspectiveMappingFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;

    [Tooltip("When the mapping is applied. Mapping is a projection correction: it should be the last " +
             "thing applied to the image, after post-processing, so effects such as bloom or vignette " +
             "are warped along with the rest and the grid is drawn over them.")]
    [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;

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

        perspectiveMappingRenderPass.renderPassEvent = passEvent;

        // After post-processing, URP may render straight to the back buffer, which this pass cannot
        // sample from: force an intermediate color texture.
        perspectiveMappingRenderPass.requiresIntermediateTexture = true;
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

