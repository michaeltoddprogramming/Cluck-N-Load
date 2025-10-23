using UnityEngine;

public class Lamp_Controller : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NightManager nightManager;
    
    [Header("Lighting Settings")]
    [SerializeField] private float dayEmissionIntensity = 0f;
    [SerializeField] private float nightEmissionIntensity = 4.1f;
    [SerializeField] private float dayLightIntensity = 0f;
    [SerializeField] private float nightLightIntensity = 30f;
    
    private Material lampMaterial;
    private Light pointLight;
    private bool wasDay = true;
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
    
    void Start()
    {
        // Find NightManager if not assigned
        if (nightManager == null)
        {
            nightManager = FindFirstObjectByType<NightManager>();
            if (nightManager == null)
            {
                Debug.LogError("Lamp_Controller: NightManager not found in scene!");
                enabled = false;
                return;
            }
        }
        
        // Cache material reference (assumes this GameObject has a Renderer)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            lampMaterial = renderer.material; // Creates instance automatically
        }
        else
        {
            Debug.LogError("Lamp_Controller: No Renderer component found!");
        }
        
        // Cache child point light reference
        pointLight = GetComponentInChildren<Light>();
        if (pointLight == null)
        {
            Debug.LogError("Lamp_Controller: No child Light component found!");
        }
        
        // Set initial state based on current time
        wasDay = nightManager.IsDay;
        UpdateLampState(wasDay);
    }
    
    void Update()
    {
        // Only update when day/night state changes (optimized)
        bool isDay = nightManager.IsDay;
        if (isDay != wasDay)
        {
            wasDay = isDay;
            UpdateLampState(isDay);
        }
    }
    
    private void UpdateLampState(bool isDay)
    {
        float emissionIntensity = isDay ? dayEmissionIntensity : nightEmissionIntensity;
        float lightIntensity = isDay ? dayLightIntensity : nightLightIntensity;
        
        // Update material emission
        if (lampMaterial != null)
        {
            Color emissionColor = Color.white * emissionIntensity;
            lampMaterial.SetColor(EmissionColorProperty, emissionColor);
        }
        
        // Update point light intensity
        if (pointLight != null)
        {
            pointLight.intensity = lightIntensity;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up material instance to prevent memory leak
        if (lampMaterial != null)
        {
            Destroy(lampMaterial);
        }
    }
}
