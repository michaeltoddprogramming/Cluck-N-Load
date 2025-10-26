using UnityEngine;

public class Blink_controller : MonoBehaviour
{
    [Header("Blink Timing")]
    [Tooltip("Minimum time between blinks (seconds)")]
    [Range(1f, 20f)]
    public float minBlinkInterval = 3f;
    
    [Tooltip("Maximum time between blinks (seconds)")]
    [Range(1f, 20f)]
    public float maxBlinkInterval = 8f;
    
    [Tooltip("How long the eyes stay closed (seconds)")]
    [Range(0.05f, 1f)]
    public float closedDuration = 0.1f;
    
    [Tooltip("Speed of closing/opening animation (seconds)")]
    [Range(0.01f, 0.2f)]
    public float blinkSpeed = 0.05f;
    
    [Header("Blink Intensity")]
    [Tooltip("Normal emission intensity multiplier (eyes open)")]
    [Range(0f, 5f)]
    public float normalIntensity = 1f;
    
    [Tooltip("Closed emission intensity multiplier (eyes closed)")]
    [Range(0f, 1f)]
    public float closedIntensity = 0f;
    
    private Renderer objectRenderer;
    private Material materialInstance;
    private Color baseEmissionColor;
    private float baseEmissionIntensity;
    private bool hasEmission = false;
    
    private float nextBlinkTime;
    private bool isBlinking = false;
    private float blinkStartTime;
    private enum BlinkPhase { Closing, Closed, Opening }
    private BlinkPhase currentPhase = BlinkPhase.Closing;

    void Start()
    {
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
                
                // Set initial blink time
                ScheduleNextBlink();
            }
            else
            {
                Debug.LogWarning($"[BlinkController] Material on {gameObject.name} doesn't have _EmissionColor property");
                enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"[BlinkController] No Renderer found on {gameObject.name}");
            enabled = false;
        }
    }

    void Update()
    {
        if (!hasEmission || materialInstance == null) return;

        // Check if it's time to blink
        if (!isBlinking && Time.time >= nextBlinkTime)
        {
            StartBlink();
        }

        // Update blink animation
        if (isBlinking)
        {
            UpdateBlink();
        }
    }

    private void ScheduleNextBlink()
    {
        // Random interval between min and max
        float interval = Random.Range(minBlinkInterval, maxBlinkInterval);
        nextBlinkTime = Time.time + interval;
    }

    private void StartBlink()
    {
        isBlinking = true;
        blinkStartTime = Time.time;
        currentPhase = BlinkPhase.Closing;
    }

    private void UpdateBlink()
    {
        float elapsed = Time.time - blinkStartTime;
        float targetIntensity = normalIntensity;
        
        switch (currentPhase)
        {
            case BlinkPhase.Closing:
                // Quick close animation
                float closeProgress = elapsed / blinkSpeed;
                if (closeProgress >= 1f)
                {
                    // Transition to closed phase
                    currentPhase = BlinkPhase.Closed;
                    blinkStartTime = Time.time; // Reset timer for closed duration
                    targetIntensity = closedIntensity;
                }
                else
                {
                    // Lerp from normal to closed
                    targetIntensity = Mathf.Lerp(normalIntensity, closedIntensity, closeProgress);
                }
                break;
                
            case BlinkPhase.Closed:
                // Stay closed for the specified duration
                targetIntensity = closedIntensity;
                if (elapsed >= closedDuration)
                {
                    // Transition to opening phase
                    currentPhase = BlinkPhase.Opening;
                    blinkStartTime = Time.time; // Reset timer for opening animation
                }
                break;
                
            case BlinkPhase.Opening:
                // Quick open animation
                float openProgress = elapsed / blinkSpeed;
                if (openProgress >= 1f)
                {
                    // Blink complete
                    targetIntensity = normalIntensity;
                    isBlinking = false;
                    ScheduleNextBlink();
                }
                else
                {
                    // Lerp from closed to normal
                    targetIntensity = Mathf.Lerp(closedIntensity, normalIntensity, openProgress);
                }
                break;
        }
        
        // Apply the emission intensity
        float newEmissionIntensity = baseEmissionIntensity * targetIntensity;
        Color newEmissionColor = baseEmissionColor.linear * (newEmissionIntensity / baseEmissionIntensity);
        materialInstance.SetColor("_EmissionColor", newEmissionColor);
    }
    
    void OnDestroy()
    {
        // Clean up material instance to prevent memory leaks
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
    
    // Optional: Method to trigger a manual blink (can be called from other scripts)
    public void TriggerBlink()
    {
        if (!isBlinking)
        {
            StartBlink();
        }
    }
}
