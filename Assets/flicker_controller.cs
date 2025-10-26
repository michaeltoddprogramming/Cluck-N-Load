using UnityEngine;

public class flicker_controller : MonoBehaviour
{
    [Header("Flicker Settings")]
    [Tooltip("Amount of intensity variation (0 = no flicker, 1 = full flicker)")]
    [Range(0f, 1f)]
    public float flickerIntensity = 0.1f;
    
    [Tooltip("Amount of range variation (0 = no flicker, 1 = full flicker)")]
    [Range(0f, 1f)]
    public float flickerRange = 0.05f;
    
    [Tooltip("Speed of the flicker effect")]
    [Range(0.1f, 10f)]
    public float flickerSpeed = 3f;

    private Light pointLight;
    private float baseIntensity;
    private float baseRange;
    private float timeOffset;

    void Start()
    {
        // Find the Light component in children
        pointLight = GetComponentInChildren<Light>();
        
        if (pointLight == null)
        {
            Debug.LogWarning($"[FlickerController] No Light component found in children of {gameObject.name}");
            enabled = false;
            return;
        }

        // Store the base values
        baseIntensity = pointLight.intensity;
        baseRange = pointLight.range;
        
        // Random time offset so lights don't all flicker in sync
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (pointLight == null) return;

        // Use Perlin noise for smooth, natural-looking flicker
        float time = (Time.time + timeOffset) * flickerSpeed;
        
        // Generate smooth random values using Perlin noise
        float intensityNoise = Mathf.PerlinNoise(time, 0f);
        float rangeNoise = Mathf.PerlinNoise(time + 50f, 0f); // Offset for different pattern
        
        // Map from [0,1] to [-1,1] for variation around base value
        float intensityVariation = (intensityNoise - 0.5f) * 2f * flickerIntensity;
        float rangeVariation = (rangeNoise - 0.5f) * 2f * flickerRange;
        
        // Apply flicker to intensity and range
        pointLight.intensity = baseIntensity * (1f + intensityVariation);
        pointLight.range = baseRange * (1f + rangeVariation);
    }
}
