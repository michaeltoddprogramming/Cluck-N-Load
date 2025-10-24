using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class BlurRendererFeature : ScriptableRendererFeature
{
    BlurRenderPass blurRenderPass;

    public override void Create()
    {
        blurRenderPass = new BlurRenderPass();
        name = "Blur";
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (blurRenderPass.Setup(renderer))
        {
            renderer.EnqueuePass(blurRenderPass);
        }
    }
}
