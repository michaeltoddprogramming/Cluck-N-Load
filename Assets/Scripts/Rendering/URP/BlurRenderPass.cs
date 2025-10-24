using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRenderPass : ScriptableRenderPass
{
    private Material material;
    private BlurSettings blurSettings;
    private RTHandle cameraColorHandle;

    public bool Setup(ScriptableRenderer renderer)
    {
        blurSettings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        if (blurSettings == null)
        {
            Debug.Log("BlurRenderPass: No BlurSettings found in Volume stack.");
            return false;
        }

        if (!blurSettings.IsActive())
        {
            Debug.Log($"BlurRenderPass: BlurSettings present but not active (strength={blurSettings.strength.value}).");
            return false;
        }

        Shader shader = Shader.Find("PostProcessing/Blur");
        if (shader == null)
        {
            Debug.LogError("BlurRenderPass: Shader 'PostProcessing/Blur' not found!");
            return false;
        }

        material = CoreUtils.CreateEngineMaterial(shader);
        Debug.Log($"BlurRenderPass: Setup succeeded. Strength={blurSettings.strength.value}");
        return material != null;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // Get the camera color target handle
        cameraColorHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
    }

    [System.Obsolete]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (blurSettings == null || !blurSettings.IsActive() || material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get("Blur Post Process");

        // Get camera descriptor
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;

        // Create temporary RT
        int tempBlurRT = Shader.PropertyToID("_TempBlurRT");
        cmd.GetTemporaryRT(tempBlurRT, descriptor);
        RenderTargetIdentifier tempRT = new RenderTargetIdentifier(tempBlurRT);

        // Calculate blur parameters
        float strength = blurSettings.strength.value;
        int gridSize = Mathf.CeilToInt(strength * 6.0f);
        if (gridSize % 2 == 0) gridSize++;
        gridSize = Mathf.Max(3, gridSize);

        material.SetInt("_GridSize", gridSize);
        material.SetFloat("_Spread", strength);

        Debug.Log($"BlurRenderPass: Execute. Strength={strength}, GridSize={gridSize}");

        // Apply blur: horizontal pass then vertical pass
        cmd.Blit(cameraColorHandle, tempRT, material, 0);
        cmd.Blit(tempRT, cameraColorHandle, material, 1);

        // Clean up
        cmd.ReleaseTemporaryRT(tempBlurRT);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}