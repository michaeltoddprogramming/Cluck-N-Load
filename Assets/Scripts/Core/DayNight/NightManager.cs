using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
// Force recompile - COMPILATION FORCED!

public class NightManager : MonoBehaviour
{
    public static NightManager Instance { get; private set; }
    private int currentSeason = 1;
    public event System.Action<int> OnDayChanged;

    // Start night button
    [Header("Start NIght button")]
    [SerializeField] private Button startNightButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI dayCountText;

    // Light
    [Header("Lighting stuff")]
    [SerializeField] private Light sceneLight;
    [SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
    [SerializeField] private Gradient morningToDayGradient;
    [SerializeField] private Gradient DayToAfternoonGradient;
    [SerializeField] private Gradient AfternoonToNightGradient;
    [SerializeField] private Gradient nightToMorningGradient;

    [Header("Performance Settings")]
    [SerializeField] private bool enableLightingOptimizations = true;
    [SerializeField] private float lightingUpdateInterval = 0.2f; // Reduce frequency for potato devices

    // Skyboxes
    [Header("Skyboxes")]
    [SerializeField] private Texture2D skyboxMorning;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxAfternoon;
    [SerializeField] private Texture2D skyboxNight;

    // Time indicator icons
    [Header("Time Indicator")]
    [SerializeField] private Image timeOfDayIcon;
    // [SerializeField] private Sprite dayIcon;
    // [SerializeField] private Sprite nightIcon;

    // Songs
    private AudioSource source1;
    private AudioSource source2;

    // Is day bool
    private bool isDay = true;
    public bool IsDay => isDay; // For CropStructureUI


    // Shop stuff
    [Header("Shop Stuff")]
    [SerializeField] private Button shopButton;
    private Color dayShop = Color.white;
    private Color nightShop = Color.grey * 0.9f;
    public Image shopIcon;
    [SerializeField] public ShopUIManager shopManager;

    // Item delete icon
    [Header("Delete Icon")]
    [SerializeField] private BuildController buildController;

    // Time management
    [Header("Time Management")]
    [SerializeField] private TextMeshProUGUI seasonNotification;
    [SerializeField] private TextMeshProUGUI productionNotification;
    [SerializeField] private float speedUp = 1f;
    [SerializeField] private float speedOfFast = 5f;

    [Tooltip("How many in-game minutes per real life second (0.02f -> 1 in-game minute = 0.02 seconds (1 day ≈ 36 minutes))")]
    [SerializeField] private float inGameMinVSSec = 0.0000000000000001f;
    [SerializeField] private bool isFast = false;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private bool isPaused = false;
    public bool IsPaused => isPaused;

    // Time
    [Header("Time")]
    [Header("Year Change Sounds")]
    private AudioSource yearAudioSource;
    private AudioSource clockTickingSource;
    private AudioSource roosterMorningSource;
    private AudioSource doubleProductionSource;
    [SerializeField] private int minutes;
    public int Minutes
    {
        get => minutes;
        set { minutes = value; OnMinutesChange(value); }
    }

    [SerializeField] private int hours;
    public int Hours
    {
        get => hours;
        set { hours = value; OnHoursChange(value); }
    }

    [SerializeField] private int days;
    public int Days
    {
        get => days;
        set
        {
            days = value;
            OnDayChange(value);
            OnDayChanged?.Invoke(days);
        }
    }

    [SerializeField] private int years;
    public int Years
    {
        get => years;
        set => years = value;
    }
    public bool YearsChanged
    {
        get => yearsChanged;
        set => yearsChanged = value;
    }
    private float tempSecond;
    private bool yearsChanged = false;

    // Season icons
    [Header("Season Icons")]
    [SerializeField] private Image seasonIcon;
    [SerializeField] private Sprite summer;
    [SerializeField] private Sprite winter;
    [SerializeField] private Sprite spring;
    [SerializeField] private Sprite fall;

    // Fog density
    [Header("Fog stuff")]
    [SerializeField] private float morningFog = 0.005f;
    [SerializeField] private float dayFog = 0.003f;
    [SerializeField] private float nightFog = 0.009f;

    // Light intensity
    [Header("Light intensity")]
    [SerializeField] private float nightIntensity = 0.03f;
    [SerializeField] private float dayIntensity = 2f;

    // Pause game manager
    [Header("Pause Game Manager")]
    [SerializeField] private PauseManager pauseManager;

    // Structures
    private List<AnimalStructure> animalStructures = new List<AnimalStructure>();
    private List<BarracksStructure> barracksStructures = new List<BarracksStructure>();
    private List<FarmHouseStructure> farmHouseStructures = new List<FarmHouseStructure>();

    private Coroutine seasonNotificationCoroutine = null;
    private Coroutine productionNotificationCoroutine = null;
    // private Coroutine wolfSpawnCoroutine = null;
    private List<Coroutine> skyboxCoroutines = new List<Coroutine>();
    private List<Coroutine> lightingCoroutines = new List<Coroutine>();

    private CombatManager combatManager;
    private bool isFirstDay = true;
    private EnemyIndicator enemyIndicator;
    private TimeIndicator timeIndicator;
    private TimeSpeedEffect timeSpeedEffect;


    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null)
        {
            Debug.LogError("CombatManager not found in scene!");
        }

        enemyIndicator = FindObjectOfType<EnemyIndicator>();
        timeIndicator = FindObjectOfType<TimeIndicator>();
        timeSpeedEffect = FindObjectOfType<TimeSpeedEffect>();

    }

    private void Start()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            source1 = sources[0];
            source2 = sources[1];
            yearAudioSource = sources[2];
            clockTickingSource = sources[3];
            roosterMorningSource = sources[4];
            doubleProductionSource = sources[5];
        }

        //preload the audio samples
        if (source1 != null)
        {
            source1.Play();
            source1.Stop();
        }
        if (source2 != null)
        {
            source2.Play();
            source2.Stop();
        }

        Hours = 5;
        Minutes = 0;

        if (Years == 0) Years = 1;
        seasonNotification.gameObject.SetActive(false);
        productionNotification.gameObject.SetActive(false);

        if (currentSeason == 0) setSeason(1);
        else setSeason(currentSeason);

        // NOTE: setSeason already calls combatManager.SetSeason, so no need for duplicate call
        // combatManager.SetSeason(currentSeason);
        // chooseAnimalProductForSeason();
    }

    private void Update()
    {
        if (isPaused)
        {
            return;
        }

        tempSecond += Time.deltaTime * speedUp;
        // tempSecond += Time.deltaTime * inGameMinVSSec * speedUp;
        // Debug.Log("here is the time: " + tempSecond);
        timeText.text = $"{Hours:D2}:{Minutes:D2}";

        // float currentTimeRate = inGameMinVSSec;
        if (tempSecond >= 0.08)
        {
            Minutes += 1;
            tempSecond = 0;
        }

        //rotate daynight icon
        rotateDayNightIcon();
    }

    public void pauseTime()
    {
        AudioManager.Instance.PauseGameAudio();
        timeIndicator.exchangeTimeIcon("pause");
        timeSpeedEffect.StopSpeedEffect();
        isPaused = true;
        Time.timeScale = 0f;

        if (shopManager != null)
        {
            shopManager.UpdateShopButtonStateForTimeControls();
        }

        GameEventManager.Instance?.OnGamePaused?.Invoke();
    }

    public void playTime()
    {
        if (isPaused == true)
        {
            AudioManager.Instance.ResumeGameAudio();
        }

        timeIndicator.exchangeTimeIcon("play");
        timeSpeedEffect.StopSpeedEffect();
        isPaused = false;
        isFast = false;
        speedUp = 1f;
        Time.timeScale = 1f;

        if (shopManager != null)
        {
            shopManager.UpdateShopButtonStateForTimeControls();
        }

        GameEventManager.Instance?.OnGameResumed?.Invoke();

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.Trigger(TutorialTrigger.TimeControlsUsed);
    }

    public void fastForwardTime()
    {
        if (isPaused == true)
        {
            AudioManager.Instance.ResumeGameAudio();
        }
        
        timeIndicator.exchangeTimeIcon("fast");
        timeSpeedEffect.StartSpeedEffect();
        // if (isPaused)
        // {
        //     Time.timeScale = 1f;
        // }

        isFast = true;
        isPaused = false;
        Time.timeScale = speedOfFast;

        if (shopManager != null)
        {
            shopManager.UpdateShopButtonStateForTimeControls();
        }

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.Trigger(TutorialTrigger.TimeControlsUsed);
    }
    private void cropGrowthOnAll(int stage)
    {
        CropStructure[] allCrops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        foreach (CropStructure crop in allCrops)
        {
            if (crop.IsGrowing && !crop.CropReady)
            {
                crop.UpdateVisuals(stage);
            }
            else if (crop.CropReady)
            {
            }
            else
            {
            }
        }
    }

    private void StartNight(int flag)
    {
        isFirstDay = false;

        combatManager.StartCombat();

        // Disable shop for night
        shopManager.disableShop();

        // IMPORTANT: Force disable build mode to place any moving structures
        if (buildController != null)
        {
            Debug.Log("StartNight: Calling DisableBuildMode to force-place any moving structures");
            buildController.DisableBuildMode();
        }

        buildController.HideDeleteIcon();
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Hide();

        // Advance crops to stage 1
        cropGrowthOnAll(1);

        shopButton.interactable = false;
        shopIcon.color = nightShop;
        isDay = false;
        buttonText.text = "End Night";

        // Set weather for night: randomly rain, only snow in winter
        if (WeatherManager.Instance != null)
        {
            // Don't allow rain during tutorial
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                WeatherManager.Instance.ClearWeather();
            }
            else
            {
                int season = currentSeason;
                float rainChance = 0.15f; // Reduced from 0.3f to 0.15f
                float snowChance = (season == 4) ? 0.5f : 0f; // Only snow in winter
                float roll = Random.value;
                if (season == 4 && roll < snowChance)
                    WeatherManager.Instance.SpawnSnow();
                else if (roll < rainChance)
                    WeatherManager.Instance.SpawnRain();
                else
                    WeatherManager.Instance.ClearWeather();
            }
        }

        // Start wolf spawning when night begins
        // if (unitSpawner != null)
        // {
        //     // Stop only wolf spawning coroutine if active
        //     if (wolfSpawnCoroutine != null)
        //     {
        //         StopCoroutine(wolfSpawnCoroutine);
        //     }

        //     wolfSpawnCoroutine = StartCoroutine(SpawnWolvesOverTime());
        // }
        // else
        // {
        //     Debug.LogError("UnitSpawner reference missing in NightManager!");
        // }

        // Manage skybox coroutines
        Coroutine skyboxCor = StartCoroutine(Skybox(skyboxDay, skyboxNight, 5f));
        skyboxCoroutines.Add(skyboxCor);

        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        sceneLight.intensity = nightIntensity;
        RenderSettings.fogDensity = nightFog;
        // timeOfDayIcon.sprite = nightIcon;

        // Notify barracks of night
        foreach (BarracksStructure barracks in barracksStructures)
        {
            if (barracks != null)
            {
                barracks.OnDayNightChanged(true);
            }
        }
    }

    private void StartDay(int flag)
    {
        playTime();
        isDay = true;
        buttonText.text = "End Day";

        // Use shop manager to properly handle tutorial state
        if (!isPaused)
        {
            shopManager.enableShop();
        }
        else
        {
            shopManager.UpdateShopButtonStateForTimeControls();
        }

        if (isFirstDay)
        {
            combatManager.StopCombat();
        }
        else
        {
            combatManager.StopCombat();
            combatManager.scaleTimeNightly();
            combatManager.increaseAfterNight();
        }
        // Destroy all remaining wolves when day starts
        // foreach (Wolf wolf in activeWolves.ToList())
        // {
        //     if (wolf != null)
        //     {
        //         wolf.OnDayNightChanged(false); // This should trigger the wolf to destroy itself
        //     }
        // }
        // activeWolves.Clear();

        // Notify animal structures
        foreach (AnimalStructure structure in animalStructures)
        {
            if (structure != null)
            {
                structure.OnNewDay();
            }
        }

        // Notify barracks of day
        foreach (BarracksStructure barracks in barracksStructures)
        {
            if (barracks != null)
            {
                barracks.OnDayNightChanged(false);
            }
        }

        // Set weather for day: randomly rain, only snow in winter
        if (WeatherManager.Instance != null)
        {
            // Don't allow rain during tutorial
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                WeatherManager.Instance.ClearWeather();
            }
            else
            {
                int season = currentSeason; // You may need to track season in NightManager
                float rainChance = 0.15f; // Reduced from 0.3f to 0.15f
                float snowChance = (season == 4) ? 0.5f : 0f; // Only snow in winter
                float roll = Random.value;
                if (season == 4 && roll < snowChance)
                    WeatherManager.Instance.SpawnSnow();
                else if (roll < rainChance)
                    WeatherManager.Instance.SpawnRain();
                else
                    WeatherManager.Instance.ClearWeather();
            }
        }

        // Advance growing crops to stage 2, preserve ready-to-harvest crops
        cropGrowthOnAll(2);

        isDay = true;
        buttonText.text = "Start Night";

        // Use shop manager to properly handle tutorial state
        if (shopManager != null)
        {
            shopManager.UpdateShopButtonStateForTimeControls();
        }

        // Manage skybox coroutines
        Coroutine skyboxCor = StartCoroutine(Skybox(skyboxNight, skyboxDay, flag == 0 ? 0f : 5f));
        skyboxCoroutines.Add(skyboxCor);

        if (source1 != null) source1.Stop();
        if (source2 != null) source2.Play();

        sceneLight.intensity = dayIntensity;
        RenderSettings.fogDensity = dayFog;
        // timeOfDayIcon.sprite = dayIcon;
    }

    private IEnumerator LightingChanges(Gradient lightGradient, float time)
    {
        for (float k = 0; k < time; k += Time.deltaTime)
        {
            sceneLight.color = lightGradient.Evaluate(k / time);
            RenderSettings.fogColor = sceneLight.color;
            yield return null;
        }
    }

    private void OnMinutesChange(int value)
    {
        sceneLight.transform.Rotate(Vector3.up, (1f / 1440f) * 360f, Space.World);
        if (value >= 60)
        {
            Hours++;
            minutes = 0;
        }
    }

    private void OnHoursChange(int value)
    {
        if (value >= 24)
        {
            Days++;
            Hours = 0;
        }

        if (value == 5)
        {
            if (days == 5 || days == 10 || days == 15 || days == 20)
            {
                OnDayChange(days);
            }
            StartDay(2);
            if (roosterMorningSource != null)
            {
                roosterMorningSource.Play();
            }
            RenderSettings.fogDensity = morningFog;

            Coroutine skyboxCor = StartCoroutine(Skybox(skyboxNight, skyboxMorning, 2f));
            skyboxCoroutines.Add(skyboxCor);

            Coroutine lightCor = StartCoroutine(LightingChanges(nightToMorningGradient, 2f));
            lightingCoroutines.Add(lightCor);

            sceneLight.colorTemperature = 2000f;
        }
        else if (value == 7)
        {
            RenderSettings.fogDensity = dayFog;

            Coroutine skyboxCor = StartCoroutine(Skybox(skyboxMorning, skyboxDay, 2f));
            skyboxCoroutines.Add(skyboxCor);

            Coroutine lightCor = StartCoroutine(LightingChanges(morningToDayGradient, 2f));
            lightingCoroutines.Add(lightCor);

            sceneLight.colorTemperature = 6000f;
        }
        else if (value == 18)
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                Coroutine tutorialSkyboxCor = StartCoroutine(Skybox(skyboxDay, skyboxAfternoon, 2f));
                skyboxCoroutines.Add(tutorialSkyboxCor);

                Coroutine tutorialLightCor = StartCoroutine(LightingChanges(DayToAfternoonGradient, 2f));
                lightingCoroutines.Add(tutorialLightCor);

                sceneLight.colorTemperature = 2000f;
                return;
            }

            if (clockTickingSource != null)
            {
                clockTickingSource.Play();
            }

            // StartNotification("Night starting soon!!", 5f);
            
            // NEW: Add modern warning notification for night approaching
            if (NotificationManager.Instance != null)
            {
                NotificationManager.ShowWarning("Night Approaching!", "Prepare defenses • Enemies spawn soon");
            }

            Coroutine skyboxCor = StartCoroutine(Skybox(skyboxDay, skyboxAfternoon, 2f));
            skyboxCoroutines.Add(skyboxCor);

            Coroutine lightCor = StartCoroutine(LightingChanges(DayToAfternoonGradient, 2f));
            lightingCoroutines.Add(lightCor);

            sceneLight.colorTemperature = 2000f;
        }
        else if (value == 20)
        {
            if (clockTickingSource.isPlaying)
            {
                clockTickingSource.Stop();
            }

            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                hours = 17;
                return;
            }

            StartNight(2);

            Coroutine skyboxCor = StartCoroutine(Skybox(skyboxAfternoon, skyboxNight, 2f));
            skyboxCoroutines.Add(skyboxCor);

            Coroutine lightCor = StartCoroutine(LightingChanges(AfternoonToNightGradient, 2f));
            lightingCoroutines.Add(lightCor);

            sceneLight.colorTemperature = 9000f;
        }
    }

    private void OnDayChange(int value)
    {
        Debug.Log($"OnDayChange called: value={value}, hours={hours}");

        if (value == 0)
        {
            Debug.Log("Day 0 - setting Spring season");
            setSeason(1);

        }
        else if (value == 5 && hours == 5)
        {
            Debug.Log("Day 5, hour 5 - setting Summer season");
            setSeason(2);
        }
        else if (value == 10 && hours == 5)
        {
            Debug.Log("Day 10, hour 5 - setting Fall season");
            setSeason(3);
        }
        else if (value == 15 && hours == 5)
        {
            Debug.Log("Day 15, hour 5 - setting Winter season");
            setSeason(4);
        }
        else if (value == 20 && hours == 5)
        {
            // print("CRITICAL: Year transition triggered at Day 20, Hour 5");
            // UnityEngine.Debug.LogError($"YEAR TRANSITION: Day 20, hour 5 detected! About to increment year from {years} to {years + 1}");
            // Debug.Log("Day 20, hour 5 - triggering year transition");
            // Debug.Log($"Before year transition: years={years}, days={days}, hours={hours}");
            // StartNotification("Night starting soon!!", 5f);

            if (yearAudioSource != null)
            {
                yearAudioSource.Play();
            }

            years++;
            days = 0;
            hours = 7;
            minutes = 0;
            YearsChanged = true;

            Debug.Log($"After year transition: years={years}, yearsChanged={YearsChanged} - setting Spring season");
            setSeason(1);

            StartDay(0); // force reset to day state
            // setSeason(1); // reset season if needed
        }
        else if (value == 21)
        {
            Debug.Log("Day 21 - triggering year transition (fallback)");
            years++;
            days = 0;
            hours = 7;
            minutes = 0;

            StartDay(0); // force reset to day state
            setSeason(1); // reset season if needed
        }

        UpdateDayCountUI();
    }

    public void UpdateEnemyIndicatorForSeason(int season)
    {
        // Debug logging to track what's happening
        Debug.Log($"UpdateEnemyIndicatorForSeason called: season={season}, years={years}");
        Debug.Log($"Enemy indicator instance: {enemyIndicator}");
        
        // Check if unlock enemy animals cheat is active
        if (CheatManager.Instance != null && CheatManager.Instance.IsUnlockEnemyAnimalsActive())
        {
            Debug.Log("Cheat active - showing all enemies");
            if (enemyIndicator != null) enemyIndicator.MakeAllEnemiesVisible();
            return;
        }
        
        // After completing the first year (year 2+), all enemy types should be available
        // if (years >= 2)
        // {
            // print($"CRITICAL: Post-first-year condition met (years={years}) - showing all enemies");
            // Debug.LogError($"CRITICAL: Post-first-year condition met (years={years}) - showing all enemies");
            // if (enemyIndicator != null) 
            // {
                // enemyIndicator.MakeAllEnemiesVisible();
                // print("Called MakeAllEnemiesVisible()");
                // Debug.LogError("Called MakeAllEnemiesVisible()");
            // }
            // else
            // {
                // Debug.LogError("Enemy indicator is null!");
            // }
            // return;
        // }
        
        // First year only: Normal seasonal behavior
        Debug.Log($"First year seasonal behavior - showing only season {season} enemy");
        if (enemyIndicator != null)
        {
            switch (season)
            {
                case 1:
                    enemyIndicator.MakeWolfVisible();
                    break;
                case 2:
                    enemyIndicator.MakeRacoonVisible();
                    break;
                case 3:
                    enemyIndicator.MakeBoarVisible();
                    break;
                case 4:
                    enemyIndicator.MakeBearVisible();
                    break;
            }
        }
        else
        {
            Debug.LogError("Enemy indicator is null!");
        }
    }

    private void UpdateDayCountUI()
    {
        if (dayCountText != null)
            dayCountText.text = $"Day {Days}";
    }

    private IEnumerator Skybox(Texture2D a, Texture2D b, float time)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        RenderSettings.skybox.SetFloat("_Blend", 0);

        for (float k = 0; k < time; k += Time.deltaTime)
        {
            RenderSettings.skybox.SetFloat("_Blend", k / time);
            yield return null;
        }

        RenderSettings.skybox.SetTexture("_Texture1", b);
    }

    private void setSeason(int season)
    {
        // COMPILATION DEFINITELY FORCED NOW!
        print($"CRITICAL DEBUG: setSeason called with season={season}, years={years}");
        // UnityEngine.Debug.LogError($"ENEMY INDICATOR DEBUG: setSeason called with season={season}, years={years}");
        Debug.Log("===== setSeason method called =====");
        Debug.Log($"setSeason called: season={season}, years={years}, yearsChanged={YearsChanged}");
        Debug.Log($"EnemyIndicator instance check: {enemyIndicator}");
        Debug.Log($"About to call UpdateEnemyIndicatorForSeason with season={season}");
        currentSeason = season;
        string text;
        switch (season)
        {
            case 1:
                if (YearsChanged)
                {
                    text = $"Year {Years} done!!\nSpring!!!!";
                    seasonIcon.sprite = spring;
                    if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialActive())
                        TutorialManager.Instance.Trigger(TutorialTrigger.SpringSeason);
                }
                else
                {
                    text = "Spring!!";
                    seasonIcon.sprite = spring;
                    if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialActive())
                        TutorialManager.Instance.Trigger(TutorialTrigger.SpringSeason);
                }
                break;
            case 2:
                YearsChanged = false;
                text = "Summer!!";
                seasonIcon.sprite = summer;
                if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialActive())
                    TutorialManager.Instance.Trigger(TutorialTrigger.SummerSeason);
                break;
            case 3:
                text = "Fall!!";
                seasonIcon.sprite = fall;
                if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialActive())
                    TutorialManager.Instance.Trigger(TutorialTrigger.FallSeason);
                break;
            case 4:
                text = "Winter!!";
                seasonIcon.sprite = winter;
                if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialActive())
                    TutorialManager.Instance.Trigger(TutorialTrigger.WinterSeason);
                break;
            default:
                text = "";
                break;
        }

        // Set weather for season change
        SetSeasonWeather();

        // Always call chooseAnimalProductForSeason (it will handle tutorial appropriately now)
        chooseAnimalProductForSeason();

        // Heal all farm houses to full HP at the start of a new season
        foreach (FarmHouseStructure farmHouse in farmHouseStructures)
        {
            if (farmHouse != null)
            {
                farmHouse.OnNewSeason(season);
            }
        }

        // Debug log removed: Setting season to ...
        if (!isFirstDay)
        {
            combatManager.increaseAfterSeason();
            combatManager.scaleTimeBySeason();
        }

        combatManager.SetSeason(season);

        // Update enemy indicator for the new season
        Debug.Log($"About to call UpdateEnemyIndicatorForSeason with season={season}, enemyIndicator={enemyIndicator}");
        UpdateEnemyIndicatorForSeason(season);

        // Use Pete for season notifications instead of basic text
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialActive())
        {
            ShowPeteSeasonNotification(season);
        }
        
        // NEW: Add modern notification system for season changes
        ShowSeasonChangeNotification(season);
    }

    private void SetSeasonWeather()
    {
        if (WeatherManager.Instance != null)
        {
            // Don't allow rain during tutorial
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                WeatherManager.Instance.ClearWeather();
            }
            else if (currentSeason == 4)
            {
                WeatherManager.Instance.SpawnSnow();
            }
            else
            {
                float rainChance = 0.15f; // Reduced from 0.3f to 0.15f
                float roll = Random.value;
                if (roll < rainChance)
                    WeatherManager.Instance.SpawnRain();
                else
                    WeatherManager.Instance.ClearWeather();
            }
        }
    }
    private void StartNotification(string message, float duration)
    {
        // Stop any existing notification first
        if (seasonNotificationCoroutine != null)
        {
            StopCoroutine(seasonNotificationCoroutine);
            seasonNotification.gameObject.SetActive(false);
        }

        // Start new notification
        seasonNotificationCoroutine = StartCoroutine(showText(message, duration));
    }

    private IEnumerator showText(string message, float time)
    {
        seasonNotification.text = message;
        seasonNotification.gameObject.SetActive(true);
        yield return new WaitForSeconds(time);
        seasonNotification.gameObject.SetActive(false);
        seasonNotificationCoroutine = null;
    }

    // New method to properly manage production notification coroutines
    private void StartProductionNotification(string message, float duration)
    {
        // Stop any existing notification first
        if (productionNotificationCoroutine != null)
        {
            StopCoroutine(productionNotificationCoroutine);
            productionNotification.gameObject.SetActive(false);
        }

        // Start new notification
        productionNotificationCoroutine = StartCoroutine(showProductionText(message, duration));
    }

    public void FastForwardEnableShop()
    {
        if (isDay) shopManager.enableShop();
    }

    public void PlayEnableShop()
    {
        if (isDay) shopManager.enableShop();
    }

    public void RegisterAnimalStructure(AnimalStructure structure)
    {
        if (structure == null || !structure) return;
        animalStructures.RemoveAll(s => s == null || !s);
        if (!animalStructures.Contains(structure))
        {
            animalStructures.Add(structure);
        }
    }

    public void UnregisterAnimalStructure(AnimalStructure structure)
    {
        if (structure == null || !structure) return;
        animalStructures.RemoveAll(s => s == null || !s);
        animalStructures.Remove(structure);
    }

    public void RegisterBarracksStructure(BarracksStructure barracks)
    {
        if (barracks != null && !barracksStructures.Contains(barracks))
        {
            barracksStructures.Add(barracks);
        }
    }

    public void UnregisterBarracksStructure(BarracksStructure barracks)
    {
        if (barracks != null && barracksStructures.Remove(barracks))
        {
        }
    }

    public void RegisterFarmHouseStructure(FarmHouseStructure farmHouse)
    {
        if (farmHouse != null && !farmHouseStructures.Contains(farmHouse))
        {
            farmHouseStructures.Add(farmHouse);
        }
    }

    public void UnregisterFarmHouseStructure(FarmHouseStructure farmHouse)
    {
        if (farmHouse != null)
        {
            farmHouseStructures.Remove(farmHouse);
        }
    }

    public void chooseAnimalProductForSeason()
    {
        // Declare once at the beginning
        ProductionBoosts productionBoosts = FindFirstObjectByType<ProductionBoosts>();

        // Allow bonuses during tutorial - removed tutorial check

        float sameProduct = Random.Range(0f, 1f);
        int product1 = Random.Range(1, 6);
        int product2 = Random.Range(1, 6);
        float increasePercent = 1.5f; // 50% increase
        float sameProductIncreasePercent = 2f; // 100% increase

        float[] boostedProducts = new float[5] { 1f, 1f, 1f, 1f, 1f }; // Initialize with default multipliers instead of zeros

        // Reset all animal production to base before applying bonuses
        foreach (AnimalStructure animalStructure in animalStructures)
        {
            animalStructure.resetAnimalProductionAmount();
        }

        if (sameProduct <= 0.05f)
        {
            string animal = determineAnimalProduct(product1);
            string fullAnimalName = getFullAnimalName(animal);

            if (animal == "E")
            {
                Debug.LogError("Invalid animal product determined."); // Keep this error log
                return;
            }

            foreach (AnimalStructure animalStructure in animalStructures)
            {
                animalStructure.updateAnimalProductionAmount(animal, sameProductIncreasePercent);
            }

            if (animal == "Ch")
            {
                boostedProducts[0] = sameProductIncreasePercent;
            }
            else if (animal == "C")
            {
                boostedProducts[1] = sameProductIncreasePercent;
            }
            else if (animal == "S")
            {
                boostedProducts[2] = sameProductIncreasePercent;
            }
            else if (animal == "G")
            {
                boostedProducts[3] = sameProductIncreasePercent;
            }
            else if (animal == "P")
            {
                boostedProducts[4] = sameProductIncreasePercent;
            }

            productionBoosts.SetBoosted(boostedProducts);

            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                return;
            }

            string message = $"Animal production increased for <b>{fullAnimalName}</b> by <b>{(sameProductIncreasePercent * 100) / 2}</b>%!\nLUCKY!!! You got a <b>double</b> production bonus!";

            if (doubleProductionSource != null)
            {
                doubleProductionSource.Play();
            }
            // StartProductionNotification(message, 5);
            
            // NEW: Add modern notification for production boost
            if (NotificationManager.Instance != null)
            {
                NotificationManager.ShowAchievement("Double Production!", $"{fullAnimalName} earning 100% more!");
            }

        }
        else
        {
            if (product1 == product2)
            {
                int[] newRange = new int[4];
                switch (product2)
                {
                    case 1: newRange = new int[] { 2, 3, 4, 5 }; break;
                    case 2: newRange = new int[] { 1, 3, 4, 5 }; break;
                    case 3: newRange = new int[] { 1, 2, 4, 5 }; break;
                    case 4: newRange = new int[] { 1, 2, 3, 5 }; break;
                    case 5: newRange = new int[] { 1, 2, 3, 4 }; break;
                    default: Debug.LogError("Invalid product number."); return;
                }

                product2 = newRange[Random.Range(0, newRange.Length)];
                string animal1 = determineAnimalProduct(product1);
                string fullAnimalName1 = getFullAnimalName(animal1);

                string animal2 = determineAnimalProduct(product2);
                string fullAnimalName2 = getFullAnimalName(animal2);

                foreach (AnimalStructure animalStructure in animalStructures)
                {
                    animalStructure.updateAnimalProductionAmount(animal1, increasePercent);
                    animalStructure.updateAnimalProductionAmount(animal2, increasePercent);
                }

                if (animal1 == "Ch")
                {
                    boostedProducts[0] = increasePercent;
                }
                else if (animal1 == "C")
                {
                    boostedProducts[1] = increasePercent;
                }
                else if (animal1 == "S")
                {
                    boostedProducts[2] = increasePercent;
                }
                else if (animal1 == "G")
                {
                    boostedProducts[3] = increasePercent;
                }
                else if (animal1 == "P")
                {
                    boostedProducts[4] = increasePercent;
                }

                if (animal2 == "Ch")
                {
                    boostedProducts[0] = increasePercent;
                }
                else if (animal2 == "C")
                {
                    boostedProducts[1] = increasePercent;
                }
                else if (animal2 == "S")
                {
                    boostedProducts[2] = increasePercent;
                }
                else if (animal2 == "G")
                {
                    boostedProducts[3] = increasePercent;
                }
                else if (animal2 == "P")
                {
                    boostedProducts[4] = increasePercent;
                }

                productionBoosts.SetBoosted(boostedProducts);



                string message = $"Animal production increased for <b>{fullAnimalName1}</b> by <b>{(increasePercent * 100) / 3}%</b> and <b>{fullAnimalName2}</b> by <b>{(increasePercent * 100) / 3}%</b>!";

                // StartProductionNotification(message, 5);
                
                // NEW: Add modern notification for dual production boost
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.ShowSuccess("Production Boost!", $"{fullAnimalName1} & {fullAnimalName2} +50% output!");
                }

                // if (doubleProductionSource != null)
                // {
                //     doubleProductionSource.Play();
                // }
            }
            else
            {
                string animal1 = determineAnimalProduct(product1);
                string fullAnimalName1 = getFullAnimalName(animal1);

                string animal2 = determineAnimalProduct(product2);
                string fullAnimalName2 = getFullAnimalName(animal2);

                foreach (AnimalStructure animalStructure in animalStructures)
                {
                    animalStructure.updateAnimalProductionAmount(animal1, increasePercent);
                    animalStructure.updateAnimalProductionAmount(animal2, increasePercent);
                }

                if (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialActive())
                {
                    bool anyBoost = false;
                    for (int i = 0; i < boostedProducts.Length; i++)
                    {
                        if (boostedProducts[i] > 1.0f)
                        {
                            anyBoost = true;
                            break;
                        }
                    }

                    if (anyBoost)
                    {
                        TutorialManager.Instance.Trigger(TutorialTrigger.AnimalProductionBoosted);
                    }
                }

                if (animal1 == "Ch")
                {
                    boostedProducts[0] = increasePercent;
                }
                else if (animal1 == "C")
                {
                    boostedProducts[1] = increasePercent;
                }
                else if (animal1 == "S")
                {
                    boostedProducts[2] = increasePercent;
                }
                else if (animal1 == "G")
                {
                    boostedProducts[3] = increasePercent;
                }
                else if (animal1 == "P")
                {
                    boostedProducts[4] = increasePercent;
                }

                if (animal2 == "Ch")
                {
                    boostedProducts[0] = increasePercent;
                }
                else if (animal2 == "C")
                {
                    boostedProducts[1] = increasePercent;
                }
                else if (animal2 == "S")
                {
                    boostedProducts[2] = increasePercent;
                }
                else if (animal2 == "G")
                {
                    boostedProducts[3] = increasePercent;
                }
                else if (animal2 == "P")
                {
                    boostedProducts[4] = increasePercent;
                }

                productionBoosts.SetBoosted(boostedProducts);


                // Block production notifications during tutorial
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    return;
                }


                string message = $"Animal production increased for <b>{fullAnimalName1}</b> by <b>{(increasePercent * 100) / 3}%</b> and <b>{fullAnimalName2}</b> by <b>{(increasePercent * 100) / 3}%</b>!";

                // StartProductionNotification(message, 5);
                
                // NEW: Add modern notification for dual production boost
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.ShowSuccess("Production Boost!", $"{fullAnimalName1} & {fullAnimalName2} +50% output!");
                }

                // if (doubleProductionSource != null)
                // {
                //     doubleProductionSource.Play();
                // }
            }
        }
    }

    // New helper method to show simplified tutorial explanation
    public void ShowSimplifiedTutorialSeasonBonus()
    {

        // During tutorial, we'll just show a message explaining the concept
        string tutorialMessage = "Each season brings <b>special bonuses</b> to your farm animals!\n\nYou can see which animals produce more by checking the price panel.";

        // Show a notification that doesn't overwhelm with details
        StartProductionNotification(tutorialMessage, 7f);

        // Apply normal seasonal bonuses during tutorial (allow players to see them working)
        ProductionBoosts productionBoostsManager = FindFirstObjectByType<ProductionBoosts>();
        if (productionBoostsManager != null)
        {
            // Let the normal seasonal bonus system work during tutorial
            // This will call chooseAnimalProductForSeason() which will apply random bonuses
            chooseAnimalProductForSeason();
        }
    }

    public void ShowPeteSeasonNotification(int season)
    {
        if (TutorialManager.Instance == null) return;

        string seasonName = GetSeasonName(season);
        string peteMessage = GetPeteSeasonMessage(season);

        // Create a Pete notification using the tutorial system
        var seasonStep = new TutorialStep
        {
            stepId = $"pete_season_{season}",
            title = $"Pete's {seasonName} Update",
            instructionText = peteMessage,
            triggerToWaitFor = TutorialTrigger.None
        };

        // Show Pete's season notification as a discovery popup
        TutorialManager.Instance.ShowPeteSeasonNotification(seasonStep);
    }

    private string GetSeasonName(int season)
    {
        return season switch
        {
            1 => "Spring",
            2 => "Summer",
            3 => "Fall",
            4 => "Winter",
            _ => "Unknown Season"
        };
    }

    private string GetPeteSeasonMessage(int season)
    {
        return season switch
        {
            1 => YearsChanged ?
                $"Howdy! Year {Years} is in the books! Welcome to Spring!\n\nEverything's blooming - perfect time for chickens and basic farming. Spring brings renewed energy to your animals!" :
                "Spring's here! Time for fresh starts and happy chickens. Your animals are feeling energetic - great season for egg production!",
            2 => "Summer heat is upon us! Your crops grow faster, but watch out - wolves get extra cranky in this weather. Stock up on defenses!",
            3 => "Fall harvest season! Animals eat heartier and produce more. Perfect time to expand your livestock before winter hits!",
            4 => "Winter's arrived! Crops grow slower but your animals huddle together for warmth. Expect snow and tougher enemies!",
            _ => "Something's wrong with the seasons, partner..."
        };
    }

    // NEW: Modern notification system for season changes
    private void ShowSeasonChangeNotification(int season)
    {
        if (NotificationManager.Instance == null) return;

        string title = "";
        string message = "";
        string theme = "Info";

        switch (season)
        {
            case 1: // Spring
                title = YearsChanged ? $"Year {Years} - Spring!" : "Spring Has Arrived!";
                message = "Animals energetic • Perfect farming weather";
                theme = "Success";
                break;
            case 2: // Summer  
                title = "Summer Heat!";
                message = "Faster crop growth • Wolves extra aggressive";
                theme = "Warning";
                break;
            case 3: // Fall
                title = "Fall Harvest Season!";
                message = "Animals eat more, produce more • Expand livestock";
                theme = "Achievement";
                break;
            case 4: // Winter
                title = "Winter is Here!";
                message = "Slower crops • Snow incoming • Tougher enemies";
                theme = "Info";
                break;
        }

        if (!string.IsNullOrEmpty(title))
        {
            NotificationManager.ShowNotification(title, message, theme, 4f);
        }
    }

    private string determineAnimalProduct(int product)
    {
        switch (product)
        {
            case 1:
                return "Ch";
            case 2:
                return "C";
            case 3:
                return "S";
            case 4:
                return "G";
            case 5:
                return "P";
            default:
                return "E";
        }
    }

    private string getFullAnimalName(string animal)
    {
        switch (animal)
        {
            case "Ch":
                return "Chicken";
            case "C":
                return "Cow";
            case "S":
                return "Sheep";
            case "G":
                return "Goat";
            case "P":
                return "Pig";
            default:
                return "Eish";
        }
    }

    private IEnumerator showProductionText(string message, float time)
    {
        productionNotification.text = message;
        productionNotification.gameObject.SetActive(true);
        yield return new WaitForSeconds(time);
        productionNotification.gameObject.SetActive(false);
        productionNotificationCoroutine = null;
    }

    /// <summary>
    /// Check if we're currently in tutorial mode and should use extended day timing
    /// </summary>
    private bool IsInTutorialMode()
    {
        return TutorialManager.Instance != null &&
               TutorialManager.Instance.IsTutorialActive() &&
               TutorialManager.Instance.enabled;
    }


    // private void rotateDayNightIcon()
    // {
    //     // Day: 7:00 to 18:00 (11 hours, 660 minutes)
    //     // Night: 18:00 to next 7:00 (13 hours, 780 minutes)
    //     float totalMinutes = Hours * 60 + Minutes;
    //     float rotation = 0f;

    //     if (Hours >= 5 && Hours < 20)
    //     {
    //         // Daytime: 7:00 (0 min) to 18:00 (660 min)
    //         float dayMinutes = totalMinutes - (5 * 60);
    //         rotation = Mathf.Clamp01(dayMinutes / 780f) * 180f;
    //     }
    //     else
    //     {
    //         // Nighttime: 18:00 (1080 min) to next 7:00 (420 min, but next day)
    //         float nightMinutes;
    //         if (Hours >= 20)
    //         {
    //             // 20:00 to 24:00
    //             nightMinutes = totalMinutes - (20 * 60);
    //         }
    //         else
    //         {
    //             // 0:00 to 7:00
    //             nightMinutes = (totalMinutes + (6 * 60)); // (0:00 is 0, 7:00 is 420)
    //         }
    //         rotation = 180f + Mathf.Clamp01(nightMinutes / 660f) * 180f;
    //     }

    //     timeOfDayIcon.rectTransform.localRotation = Quaternion.Euler(0, 0, -rotation);
    // }

    private void rotateDayNightIcon()
    {
        // Day: 5:00 to 20:00 (15 hours, 900 minutes)
        // Night: 20:00 to next 5:00 (9 hours, 540 minutes)
        float totalMinutes = Hours * 60 + Minutes;
        float rotation = 0f;

        if (Hours >= 5 && Hours < 20)
        {
            // Daytime: 5:00 (0 min) to 20:00 (900 min)
            float dayMinutes = totalMinutes - (5 * 60);
            rotation = Mathf.Clamp01(dayMinutes / 900f) * 180f;
        }
        else
        {
            // Nighttime: 20:00 (1200 min) to next 5:00 (300 min)
            float nightMinutes;
            if (Hours >= 20)
            {
                // 20:00 to 24:00
                nightMinutes = totalMinutes - (20 * 60);
            }
            else
            {
                // 0:00 to 5:00
                nightMinutes = totalMinutes + (4 * 60); // 0:00 -> 0, 5:00 -> 300
            }
            rotation = 180f + Mathf.Clamp01(nightMinutes / 540f) * 180f;
        }

        timeOfDayIcon.rectTransform.localRotation = Quaternion.Euler(0, 0, -rotation);
    }


    public void SetSeason(int season)
    {
        if (season < 1 || season > 4) return;

        Debug.Log($"Public SetSeason called: season={season} - calling private setSeason method");
        
        // Call the private setSeason method to ensure all season logic is executed
        setSeason(season);

        // Trigger season change events
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.TriggerSeasonChanged(season);
        }

        Debug.Log($"Season changed to {season}");
    }

    public int GetDays() => days;
    public bool GetIsDay() => isDay;
    public int GetCurrentSeason()
    {
        return currentSeason;
    }

    public string GetSeason()
    {
        switch (currentSeason)
        {
            case 1: return "Spring";
            case 2: return "Summer";
            case 3: return "Fall";
            case 4: return "Winter";
            default: return "Spring";
        }
    }

    public void CheatSetDays(int newDays)
    {
        days = newDays;
        OnDayChange(newDays);
        OnDayChanged?.Invoke(days);
        UpdateDayCountUI();
    }

    public void CheatForceNight()
    {
        Hours = 18;
        StartNight(2);
    }

    public void CheatForceDay()
    {
        Hours = 7;
        StartDay(2);
    }

    private void OnDestroy()
    {
        // FIXED COMPILATION FORCED - Clean up any remaining coroutines on destroy
        if (seasonNotificationCoroutine != null)
        {
            StopCoroutine(seasonNotificationCoroutine);
            seasonNotificationCoroutine = null;
        }
        if (productionNotificationCoroutine != null)
        {
            StopCoroutine(productionNotificationCoroutine);
            productionNotificationCoroutine = null;
        }
        
        // Safely stop skybox coroutines
        if (skyboxCoroutines != null)
        {
            foreach (Coroutine cor in skyboxCoroutines)
            {
                if (cor != null)
                {
                    StopCoroutine(cor);
                }
            }
            skyboxCoroutines.Clear();
        }
        
        // Safely stop lighting coroutines
        foreach (Coroutine cor in lightingCoroutines)
        {
            if (cor != null)
            {
                StopCoroutine(cor);
            }
        }
    }

    public bool getIsPaused()
    {
        return isPaused;
    }
}