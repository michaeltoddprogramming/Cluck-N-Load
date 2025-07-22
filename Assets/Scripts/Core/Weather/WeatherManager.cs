using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Weather Objects")]
    [SerializeField] private ParticleSystem rainSystem;
    [SerializeField] private ParticleSystem snowSystem;
    [SerializeField] private WindZone windZone;
    [SerializeField] private ParticleSystem weatherWindSystem;
    
    [Header("Seasonal Weather Settings")]
    [SerializeField] private WeatherSeasonData springWeather;
    [SerializeField] private WeatherSeasonData summerWeather;
    [SerializeField] private WeatherSeasonData fallWeather;
    [SerializeField] private WeatherSeasonData winterWeather;
    
    [Header("Weather Intensity")]
    [SerializeField] private float baseRainIntensity = 100f;
    [SerializeField] private float baseSnowIntensity = 50f;
    [SerializeField] private float baseWindStrength = 1f;
    
    [Header("Weather Transition")]
    [SerializeField] private float weatherTransitionTime = 3f;
    [SerializeField] private bool enableRandomWeatherEvents = true;
    [SerializeField] private float weatherEventChance = 0.3f; // 30% chance per day
    
    [Header("Performance & Organization")]
    [SerializeField] private bool enableOptimizations = true;
    [SerializeField] private bool organizeWeatherObjects = true;
    [SerializeField] private Transform weatherParent; // Optional parent for organization
    
    [Header("Debug & Visual Improvements")]
    [SerializeField] private bool enableWeatherDebugLogs = true;
    [SerializeField] private bool improveParticleVisuals = true;
    [SerializeField] private float debugUpdateInterval = 2f; // How often to log weather status
    [SerializeField] private bool enableTransitionLogs = false; // Reduce spam from transitions
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource rainAudioSource;
    
    private int currentSeason = 1; // 1=Spring, 2=Summer, 3=Fall, 4=Winter
    private bool isNight = false;
    private Coroutine currentWeatherTransition;
    private float lastDebugTime;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Auto-find weather objects if not assigned
        FindWeatherObjects();
        
        // Organize weather objects in hierarchy
        OrganizeWeatherObjects();
        
        // Initialize weather for current season
        SetSeasonalWeather(1, false); // Start with Spring
        
        // Improve particle visuals if enabled
        if (improveParticleVisuals)
        {
            ImproveWeatherVisuals();
        }
        
        // Removed automatic creation of weather data. Set all values manually in the Unity editor.
        
        // Subscribe to NightManager events if available
        StartCoroutine(WaitForNightManagerAndSubscribe());
    }
    
    private void FindWeatherObjects()
    {
        if (rainSystem == null)
        {
            GameObject rainObj = GameObject.Find("Rain");
            if (rainObj != null)
                rainSystem = rainObj.GetComponent<ParticleSystem>();
        }
        
        if (snowSystem == null)
        {
            GameObject snowObj = GameObject.Find("Snow");
            if (snowObj != null)
                snowSystem = snowObj.GetComponent<ParticleSystem>();
        }
        
        if (windZone == null)
        {
            GameObject windObj = GameObject.Find("Wind");
            if (windObj != null)
                windZone = windObj.GetComponent<WindZone>();
        }
        
        if (weatherWindSystem == null)
        {
            GameObject weatherWindObj = GameObject.Find("WeatherWind");
            if (weatherWindObj != null)
                weatherWindSystem = weatherWindObj.GetComponent<ParticleSystem>();
        }
    }
    
    private IEnumerator WaitForNightManagerAndSubscribe()
    {
        // Wait for NightManager to be ready
        while (NightManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("WeatherManager: Connected to NightManager");
    }
    
    private void OrganizeWeatherObjects()
    {
        if (!organizeWeatherObjects) return;
        
        // Create or find weather parent object
        if (weatherParent == null)
        {
            GameObject weatherParentObj = GameObject.Find("Weather Systems");
            if (weatherParentObj == null)
            {
                weatherParentObj = new GameObject("Weather Systems");
                weatherParentObj.transform.position = Vector3.zero;
            }
            weatherParent = weatherParentObj.transform;
        }
        
        // Organize weather objects under parent
        if (rainSystem != null && rainSystem.transform.parent != weatherParent)
        {
            rainSystem.transform.SetParent(weatherParent);
            rainSystem.name = "Rain System";
        }
        
        if (snowSystem != null && snowSystem.transform.parent != weatherParent)
        {
            snowSystem.transform.SetParent(weatherParent);
            snowSystem.name = "Snow System";
        }
        
        if (windZone != null && windZone.transform.parent != weatherParent)
        {
            windZone.transform.SetParent(weatherParent);
            windZone.name = "Wind Zone";
        }
        
        if (weatherWindSystem != null && weatherWindSystem.transform.parent != weatherParent)
        {
            weatherWindSystem.transform.SetParent(weatherParent);
            weatherWindSystem.name = "Weather Wind System";
        }
        
        Debug.Log("WeatherManager: Organized weather objects under 'Weather Systems' parent");
    }
    
    private void ImproveWeatherVisuals()
    {
        // Simple color changes only
        if (rainSystem != null)
        {
            var main = rainSystem.main;
            main.startColor = Color.blue; // Blue rain
        }
        
        if (snowSystem != null)
        {
            var main = snowSystem.main;
            main.startColor = Color.white; // White snow
        }
        
        if (weatherWindSystem != null)
        {
            var main = weatherWindSystem.main;
            main.startColor = new Color(0.7f, 0.6f, 0.4f, 0.6f); // Brown wind
        }
        
        Debug.Log("Simple weather colors applied: Rain=Blue, Snow=White, Wind=Brown");
    }
    
    // Removed automatic creation of weather data. Set all values manually in the Unity editor.
    
    private void LogCurrentWeatherStatus()
    {
        string seasonName = GetSeasonName(currentSeason);
        string timeOfDay = isNight ? "Night" : "Day";
        
        string rainStatus = GetParticleSystemStatus(rainSystem, "Rain");
        string snowStatus = GetParticleSystemStatus(snowSystem, "Snow");
        string windStatus = windZone != null ? $"Wind: {windZone.windMain:F1} strength" : "Wind: Not found";
        string weatherWindStatus = GetParticleSystemStatus(weatherWindSystem, "Weather Wind");
        
        Debug.Log($"=== WEATHER STATUS ===\n" +
                  $"Season: {seasonName} | Time: {timeOfDay}\n" +
                  $"{rainStatus}\n" +
                  $"{snowStatus}\n" +
                  $"{windStatus}\n" +
                  $"{weatherWindStatus}\n" +
                  $"==================");
    }
    
    private string GetParticleSystemStatus(ParticleSystem system, string name)
    {
        if (system == null)
            return $"{name}: Not found";
            
        bool isPlaying = system.isPlaying;
        float emissionRate = system.emission.rateOverTime.constant;
        int particleCount = system.particleCount;
        
        string status = isPlaying ? "ACTIVE" : "INACTIVE";
        return $"{name}: {status} | Emission: {emissionRate:F0}/sec | Particles: {particleCount}";
    }
    
    private void Update()
    {
        // Debug weather status periodically
        if (enableWeatherDebugLogs && Time.time - lastDebugTime >= debugUpdateInterval)
        {
            LogCurrentWeatherStatus();
            lastDebugTime = Time.time;
        }
    }
    
    public void OnSeasonChanged(int season)
    {
        Debug.Log($"🌍 WeatherManager: Season changed to {GetSeasonName(season)}");
        currentSeason = season;
        SetSeasonalWeather(season, true);
        
        // Log immediate weather status after season change
        if (enableWeatherDebugLogs)
        {
            LogCurrentWeatherStatus();
        }
        
        // Random weather event chance when season changes
        if (enableRandomWeatherEvents && Random.Range(0f, 1f) < weatherEventChance)
        {
            StartCoroutine(TriggerRandomWeatherEvent());
        }
    }
    
    public void OnDayNightChanged(bool isNightTime)
    {
        isNight = isNightTime;
        string timeChange = isNightTime ? "🌙 Night" : "☀️ Day";
        Debug.Log($"WeatherManager: Time changed to {timeChange}");
        
        AdjustWeatherForTimeOfDay();
        
        // Log weather status after time change
        if (enableWeatherDebugLogs)
        {
            LogCurrentWeatherStatus();
        }
    }
    
    private void SetSeasonalWeather(int season, bool animate)
    {
        WeatherSeasonData seasonData = GetSeasonData(season);
        if (seasonData == null) return;
        
        // CLAMP VALUES BEFORE STARTING TRANSITION to prevent infinite loops
        WeatherSeasonData clampedData = ScriptableObject.CreateInstance<WeatherSeasonData>();
        clampedData.rainIntensity = Mathf.Clamp(seasonData.rainIntensity, 0f, 300f);
        clampedData.snowIntensity = Mathf.Clamp(seasonData.snowIntensity, 0f, 200f);
        clampedData.windStrength = Mathf.Clamp(seasonData.windStrength, 0f, 8f); // Cap at 8 BEFORE transition
        clampedData.enableWeatherWind = seasonData.enableWeatherWind;
        clampedData.rainChance = seasonData.rainChance;
        clampedData.snowChance = seasonData.snowChance;
        clampedData.windChance = seasonData.windChance;
        
        // Override snow for warm seasons
        if (season == 1 || season == 2) // Spring or Summer
        {
            clampedData.snowIntensity = 0f;
            if (enableWeatherDebugLogs)
                Debug.Log($"🌸 {GetSeasonName(season)} weather - Snow disabled for warm season");
        }
        
        if (currentWeatherTransition != null)
        {
            StopCoroutine(currentWeatherTransition);
        }
        
        if (animate)
        {
            currentWeatherTransition = StartCoroutine(TransitionToWeather(clampedData));
        }
        else
        {
            ApplyWeatherInstantly(clampedData);
        }
    }
    
    private WeatherSeasonData GetSeasonData(int season)
    {
        return season switch
        {
            1 => springWeather,
            2 => summerWeather,
            3 => fallWeather,
            4 => winterWeather,
            _ => springWeather
        };
    }
    
    private IEnumerator TransitionToWeather(WeatherSeasonData targetWeather)
    {
        float elapsed = 0f;
        
        // Get current states
        float startRainRate = rainSystem != null && rainSystem.isPlaying ? rainSystem.emission.rateOverTime.constant : 0f;
        float startSnowRate = snowSystem != null && snowSystem.isPlaying ? snowSystem.emission.rateOverTime.constant : 0f;
        float startWindMain = windZone != null ? windZone.windMain : 0f;
        
        while (elapsed < weatherTransitionTime)
        {
            float progress = elapsed / weatherTransitionTime;
            
            // Lerp weather values
            float currentRainRate = Mathf.Lerp(startRainRate, targetWeather.rainIntensity, progress);
            float currentSnowRate = Mathf.Lerp(startSnowRate, targetWeather.snowIntensity, progress);
            float currentWindStrength = Mathf.Lerp(startWindMain, targetWeather.windStrength, progress);
            
            ApplyWeatherValues(currentRainRate, currentSnowRate, currentWindStrength, targetWeather);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final values are set
        ApplyWeatherValues(targetWeather.rainIntensity, targetWeather.snowIntensity, targetWeather.windStrength, targetWeather);
        currentWeatherTransition = null;
    }
    
    private void ApplyWeatherInstantly(WeatherSeasonData weather)
    {
        ApplyWeatherValues(weather.rainIntensity, weather.snowIntensity, weather.windStrength, weather);
    }
    
    private void ApplyWeatherValues(float rainRate, float snowRate, float windStrength, WeatherSeasonData weather)
    {
        bool anyWeatherActive = false;
        string seasonName = GetSeasonName(currentSeason);
        
        // Final safety caps (should rarely trigger now)
        bool windCapped = false;
        if (windStrength > 8f)
        {
            windCapped = true;
            windStrength = 8f;
        }
        
        // Only log capping once per transition, not constantly
        if (windCapped && enableTransitionLogs)
        {
            Debug.Log($"❌ Final wind cap applied: {windStrength:F1} in {seasonName}");
        }
        
        // Rain
        if (rainSystem != null)
        {
            var emission = rainSystem.emission;
            float currentRate = emission.rateOverTime.constant;
            emission.rateOverTime = rainRate;
            
            if (rainRate > 0 && !rainSystem.isPlaying)
            {
                rainSystem.Play();
                if (rainAudioSource != null && !rainAudioSource.isPlaying)
                    rainAudioSource.Play();
            }
            else if (rainRate <= 0 && rainSystem.isPlaying)
            {
                rainSystem.Stop();
                if (rainAudioSource != null && rainAudioSource.isPlaying)
                    rainAudioSource.Stop();
            }
            else if (rainRate > 0)
            {
                anyWeatherActive = true;
            }
        }
        
        // Snow
        if (snowSystem != null)
        {
            var emission = snowSystem.emission;
            float currentRate = emission.rateOverTime.constant;
            emission.rateOverTime = snowRate;
            
            if (snowRate > 0 && !snowSystem.isPlaying)
            {
                snowSystem.Play();
                if (enableWeatherDebugLogs)
                    Debug.Log($"❄️ Snow started - Intensity: {snowRate:F0}/sec in {seasonName}");
                anyWeatherActive = true;
            }
            else if (snowRate <= 0 && snowSystem.isPlaying)
            {
                snowSystem.Stop();
                if (enableWeatherDebugLogs && enableTransitionLogs)
                    Debug.Log($"❄️ Snow stopped in {seasonName}");
            }
            else if (snowRate > 0)
            {
                anyWeatherActive = true;
            }
        }
        
        // Wind
        if (windZone != null)
        {
            float currentWindMain = windZone.windMain;
            windZone.windMain = windStrength;
            windZone.windTurbulence = windStrength * 0.3f;
            
            // Only log significant wind changes, not constant updates
            if (Mathf.Abs(currentWindMain - windStrength) > 0.5f && enableTransitionLogs)
            {
                Debug.Log($"💨 Wind adjusted to {windStrength:F1} in {seasonName}");
                anyWeatherActive = true;
            }
            else if (windStrength > 1f)
            {
                anyWeatherActive = true;
            }
        }
        
        // Weather Wind Particles
        if (weatherWindSystem != null && weather != null && weather.enableWeatherWind)
        {
            var emission = weatherWindSystem.emission;
            emission.rateOverTime = windStrength * 20f;
            
            if (windStrength > 0 && !weatherWindSystem.isPlaying)
            {
                weatherWindSystem.Play();
                if (enableWeatherDebugLogs)
                    Debug.Log($"🌪️ Weather wind effects started - Particles: {windStrength * 20f:F0}/sec");
                anyWeatherActive = true;
            }
            else if (windStrength <= 0 && weatherWindSystem.isPlaying)
            {
                weatherWindSystem.Stop();
                if (enableWeatherDebugLogs)
                    Debug.Log("🌪️ Weather wind effects stopped");
            }
        }
        
        // Only log clear weather occasionally, not constantly
        if (!anyWeatherActive && enableWeatherDebugLogs && enableTransitionLogs)
        {
            Debug.Log($"☀️ Clear weather in {seasonName} - All effects inactive");
        }
    }
    
    private void AdjustWeatherForTimeOfDay()
    {
        if (currentWeatherTransition != null) return; // Don't interfere with transitions
        
        float timeMultiplier = isNight ? 1.2f : 1f; // Slightly more intense weather at night
        
        WeatherSeasonData currentData = GetSeasonData(currentSeason);
        if (currentData == null) return;
        
        ApplyWeatherValues(
            currentData.rainIntensity * timeMultiplier,
            currentData.snowIntensity * timeMultiplier,
            currentData.windStrength * timeMultiplier,
            currentData
        );
    }
    
    private IEnumerator TriggerRandomWeatherEvent()
    {
        Debug.Log("⛈️ WeatherManager: RANDOM WEATHER EVENT TRIGGERED!");
        
        // Store current weather
        WeatherSeasonData normalWeather = GetSeasonData(currentSeason);
        
        // Create intense weather event
        WeatherSeasonData eventWeather = ScriptableObject.CreateInstance<WeatherSeasonData>();
        eventWeather.rainIntensity = normalWeather.rainIntensity * 2f;
        eventWeather.snowIntensity = normalWeather.snowIntensity * 2f;
        eventWeather.windStrength = normalWeather.windStrength * 1.5f;
        eventWeather.enableWeatherWind = true;
        
        Debug.Log($"🌩️ Storm intensity - Rain: {eventWeather.rainIntensity:F0}, Snow: {eventWeather.snowIntensity:F0}, Wind: {eventWeather.windStrength:F1}");
        
        // Apply intense weather
        yield return StartCoroutine(TransitionToWeather(eventWeather));
        
        // Wait for event duration
        float eventDuration = Random.Range(30f, 90f);
        Debug.Log($"⏱️ Weather event will last {eventDuration:F0} seconds");
        yield return new WaitForSeconds(eventDuration);
        
        // Return to normal weather
        Debug.Log("🌤️ Weather event ending, returning to normal conditions...");
        yield return StartCoroutine(TransitionToWeather(normalWeather));
        
        Debug.Log("✅ Weather event ended - Normal weather restored");
    }
    
    private string GetSeasonName(int season)
    {
        return season switch
        {
            1 => "Spring",
            2 => "Summer", 
            3 => "Fall",
            4 => "Winter",
            _ => "Unknown"
        };
    }
    
    // Public methods for manual control
    public void SetRainIntensity(float intensity)
    {
        if (rainSystem != null)
        {
            var emission = rainSystem.emission;
            emission.rateOverTime = intensity;

            if (intensity > 0 && !rainSystem.isPlaying)
            {
                rainSystem.Play();
                if (rainAudioSource != null && !rainAudioSource.isPlaying)
                    rainAudioSource.Play();
            }
            else if (intensity <= 0 && rainSystem.isPlaying)
            {
                rainSystem.Stop();
                if (rainAudioSource != null && rainAudioSource.isPlaying)
                    rainAudioSource.Stop();
            }
        }
    }

    public void SetSnowIntensity(float intensity)
    {
        if (snowSystem != null)
        {
            var emission = snowSystem.emission;
            emission.rateOverTime = intensity;
            
            if (intensity > 0 && !snowSystem.isPlaying)
            {
                snowSystem.Play();
                Debug.Log($"❄️ Snow started - Intensity: {intensity:F0}/sec");
            }
            else if (intensity <= 0 && snowSystem.isPlaying)
            {
                snowSystem.Stop();
                Debug.Log("❄️ Snow stopped");
            }
        }
    }
    
    public void SetWindStrength(float strength)
    {
        if (windZone != null)
        {
            windZone.windMain = strength;
            windZone.windTurbulence = strength * 0.3f;
            
            Debug.Log($"💨 Wind strength set to {strength:F1}");
        }
    }
    
    public void ForceSeasonChange(int season)
    {
        if (season < 1 || season > 4) return;
        
        Debug.Log($"⚡️ WeatherManager: Forcing season change to {GetSeasonName(season)}");
        currentSeason = season;
        
        // Immediately apply season weather without transition
        WeatherSeasonData seasonData = GetSeasonData(season);
        if (seasonData != null)
        {
            ApplyWeatherInstantly(seasonData);
        }
    }
    
    public void TriggerRandomWeatherEventNow()
    {
        if (enableRandomWeatherEvents)
        {
            StartCoroutine(TriggerRandomWeatherEvent());
        }
        else
        {
            Debug.LogWarning("Random weather events are disabled in the settings.");
        }
    }
}
