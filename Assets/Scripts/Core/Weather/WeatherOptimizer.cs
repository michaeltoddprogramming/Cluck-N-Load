using UnityEngine;

[System.Serializable]
public class WeatherOptimizer
{
    [Header("Performance Settings")]
    [SerializeField] private bool enableWeatherCulling = true;
    [SerializeField] private float cullingDistance = 100f;
    [SerializeField] private bool enableAdaptiveQuality = true;
    
    private Camera playerCamera;
    private ParticleSystem[] weatherSystems;
    
    public void Initialize(Camera camera, ParticleSystem[] systems)
    {
        playerCamera = camera;
        weatherSystems = systems;
        
        if (enableAdaptiveQuality)
        {
            OptimizeForDevice();
        }
    }
    
    public void UpdateWeatherCulling()
    {
        if (!enableWeatherCulling || playerCamera == null || weatherSystems == null)
            return;
            
        foreach (var system in weatherSystems)
        {
            if (system == null) continue;
            
            float distance = Vector3.Distance(playerCamera.transform.position, system.transform.position);
            bool shouldPlay = distance <= cullingDistance;
            
            if (shouldPlay && !system.isPlaying)
            {
                system.Play();
            }
            else if (!shouldPlay && system.isPlaying)
            {
                system.Stop();
            }
        }
    }
    
    private void OptimizeForDevice()
    {
        // Reduce particle count on lower-end devices
        if (SystemInfo.processorCount <= 2 || SystemInfo.systemMemorySize <= 4096)
        {
            foreach (var system in weatherSystems)
            {
                if (system == null) continue;
                
                var main = system.main;
                main.maxParticles = Mathf.Max(10, main.maxParticles / 2);
            }
        }
    }
}
