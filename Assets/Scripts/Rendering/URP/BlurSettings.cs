using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Custom/Blur")]
public class BlurSettings : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Standard deviation (spread) of the blur. Grid size is approx. 6x larger.")]
    public ClampedFloatParameter strength = new ClampedFloatParameter(0.0f, 0.0f, 15.0f);

    public bool IsActive()
    {
        // FIXED: Allow blur to be active even at 0 strength
        // This lets the render pass initialize properly and animate from 0
        return active;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}