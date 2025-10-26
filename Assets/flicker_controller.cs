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
    
    [Tooltip("Amount of emission variation (0 = no flicker, 1 = full flicker)")]
    [Range(0f, 1f)]
    public float flickerEmission = 0.1f;
    
    [Tooltip("Speed of the flicker effect")]
    [Range(0.1f, 10f)]
    public float flickerSpeed = 3f;
    
    [Header("Emission Control")]
    [Tooltip("Overall emission intensity multiplier (affects base brightness)")]
    [Range(0f, 5f)]
    public float emissionIntensityMultiplier = 1f;

    private Light pointLight;
    private float baseIntensity;
    private float baseRange;
    private float timeOffset;
    
    private Renderer objectRenderer;
    private Material materialInstance;
    private Color baseEmissionColor;
    private float baseEmissionIntensity;
    private bool hasEmission = false;

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
        
        // Get the renderer and material from this GameObject
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            // Create material instance to avoid modifying the shared material
            materialInstance = objectRenderer.material;
            
            // Check if material has emission
            if (materialInstance.HasProperty("_EmissionColor"))
            {
                baseEmissionColor = materialInstance.GetColor("_EmissionColor");
                
                // Extract emission intensity from HDR color
                float maxColorComponent = Mathf.Max(Mathf.Max(baseEmissionColor.r, baseEmissionColor.g), baseEmissionColor.b);
                baseEmissionIntensity = maxColorComponent;
                
                hasEmission = true;
            }
            else
            {
                Debug.LogWarning($"[FlickerController] Material on {gameObject.name} doesn't have _EmissionColor property");
            }
        }
        
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
        float emissionVariation = intensityVariation * (flickerEmission / flickerIntensity); // Sync with intensity flicker
        
        // Apply flicker to intensity and range
        pointLight.intensity = baseIntensity * (1f + intensityVariation);
        pointLight.range = baseRange * (1f + rangeVariation);
        
        // Apply flicker to emission if available (synced with light intensity)
        if (hasEmission && materialInstance != null)
        {
            // Apply overall intensity multiplier AND flicker variation
            float newEmissionIntensity = baseEmissionIntensity * emissionIntensityMultiplier * (1f + emissionVariation);
            
            // Reconstruct the emission color with new intensity
            Color newEmissionColor = baseEmissionColor.linear * (newEmissionIntensity / baseEmissionIntensity);
            materialInstance.SetColor("_EmissionColor", newEmissionColor);
        }
    }
    
    void OnDestroy()
    {
        // Clean up material instance to prevent memory leaks
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}
