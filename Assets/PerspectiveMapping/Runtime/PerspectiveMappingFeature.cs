using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class PerspectiveMappingFeature : ScriptableRendererFeature
{
    private const string k_ShaderName = "PerspectiveMapping/PerspectiveMappingShader";

    [SerializeField] private Shader shader;

    [Tooltip("When the mapping is applied. Mapping is a projection correction: it should be the last " +
             "thing applied to the image, after post-processing, so effects such as bloom or vignette " +
             "are warped along with the rest and the grid is drawn over them.")]
    [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private Material material;
    private PerspectiveMappingRenderPass perspectiveMappingRenderPass;

    public override void Create()
    {
        perspectiveMappingRenderPass = new PerspectiveMappingRenderPass(EnsureMaterial());

        perspectiveMappingRenderPass.renderPassEvent = passEvent;

        // After post-processing, URP may render straight to the back buffer, which this pass cannot
        // sample from: force an intermediate color texture.
        perspectiveMappingRenderPass.requiresIntermediateTexture = true;
    }

    // The material is created hidden and unsaved: a plain new Material() is seen as an unused
    // asset and destroyed by Resources.UnloadUnusedAssets (on every scene load), leaving the pass
    // with a destroyed material. If it is gone anyway, it is created again.
    private Material EnsureMaterial()
    {
        if (material != null)
            return material;

        if (shader == null)
            shader = Shader.Find(k_ShaderName);

        if (shader == null)
        {
            Debug.LogError("[PerspectiveMappingFeature] Shader not found : " + k_ShaderName);
            return null;
        }

        material = CoreUtils.CreateEngineMaterial(shader);
        return material;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (perspectiveMappingRenderPass == null)
        {
            return;
        }

        if (renderingData.cameraData.cameraType != CameraType.Game) // Use this to select which camera
        {
            return;
        }

        var currentMaterial = EnsureMaterial();
        if (currentMaterial == null)
        {
            return;
        }

        perspectiveMappingRenderPass.SetMaterial(currentMaterial);
        renderer.EnqueuePass(perspectiveMappingRenderPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
        material = null;
    }
}
