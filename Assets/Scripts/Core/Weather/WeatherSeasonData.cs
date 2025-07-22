using UnityEngine;

[CreateAssetMenu(fileName = "WeatherSeasonData", menuName = "Weather/Season Data")]
public class WeatherSeasonData : ScriptableObject
{
    [Header("Weather Intensities")]
    [Tooltip("Rain particle emission rate (0 = no rain)")]
    [Range(0f, 300f)] public float rainIntensity = 0f;
    
    [Tooltip("Snow particle emission rate (0 = no snow)")]
    [Range(0f, 200f)] public float snowIntensity = 0f;
    
    [Tooltip("Wind zone strength")]
    [Range(0f, 10f)] public float windStrength = 1f;
    
    [Header("Special Effects")]
    [Tooltip("Enable weather wind particle effects (dust, leaves, etc.)")]
    public bool enableWeatherWind = false;
    
    [Header("Weather Probabilities")]
    [Tooltip("Chance of rain events during this season")]
    [Range(0f, 1f)] public float rainChance = 0.3f;
    
    [Tooltip("Chance of snow events during this season")]
    [Range(0f, 1f)] public float snowChance = 0.1f;
    
    [Tooltip("Chance of windy weather during this season")]
    [Range(0f, 1f)] public float windChance = 0.5f;
    
    [Header("Season Description")]
    [TextArea(2, 4)]
    public string seasonDescription = "Default season weather";
}
