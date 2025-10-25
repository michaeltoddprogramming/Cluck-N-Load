using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
public class NightManager : MonoBehaviour
{
    public static NightManager Instance { get; private set; }
    private int currentSeason = 1;
    public event System.Action<int> OnDayChanged;

    [Header("Start NIght button")]
    [SerializeField] private Button startNightButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI dayCountText;

    [Header("Lighting stuff")]
    [SerializeField] private Light sceneLight;
    [SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
    [SerializeField] private Gradient morningToDayGradient;
    [SerializeField] private Gradient DayToAfternoonGradient;
    [SerializeField] private Gradient AfternoonToNightGradient;
    [SerializeField] private Gradient nightToMorningGradient;

    [Header("Skyboxes")]
    [SerializeField] private Texture2D skyboxMorning;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxAfternoon;
    [SerializeField] private Texture2D skyboxNight;

    [Header("Time Indicator")]
    [SerializeField] private Image timeOfDayIcon;

    private AudioSource source1;
    private AudioSource source2;

    private bool isDay = true;
    public bool IsDay => isDay; // For CropStructureUI

    [Header("Shop Stuff")]
    [SerializeField] private Button shopButton;
    private Color dayShop = Color.white;
    private Color nightShop = Color.grey * 0.9f;
    public Image shopIcon;
    [SerializeField] public ShopUIManager shopManager;

    [Header("Delete Icon")]
    [SerializeField] private BuildController buildController;

    [Header("Time Management")]
    [SerializeField] private TextMeshProUGUI seasonNotification;
    [SerializeField] private TextMeshProUGUI productionNotification;
    [SerializeField] private float speedUp = 1f;
    [SerializeField] private float speedOfFast = 5f;

    [Tooltip("How many in-game minutes per real life second (0.02f -> 1 in-game minute = 0.02 seconds (1 day ≈ 36 minutes))")]
    [SerializeField] private bool isPaused = false;
    public bool IsPaused => isPaused;

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

    // private int daysThisYear = 0; // Unused field
    private int lastSurvivalRewardDay = -1;

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

    [Header("Season Icons")]
    [SerializeField] private Image seasonIcon;
    [SerializeField] private Sprite summer;
    [SerializeField] private Sprite winter;
    [SerializeField] private Sprite spring;
    [SerializeField] private Sprite fall;

    [Header("Fog stuff")]
    [SerializeField] private float morningFog = 0.005f;
    [SerializeField] private float dayFog = 0.003f;
    [SerializeField] private float nightFog = 0.009f;

    [Header("Light intensity")]
    [SerializeField] private float nightIntensity = 0.03f;
    [SerializeField] private float dayIntensity = 2f;

    [Header("Pause Game Manager")]
    [SerializeField] private PauseManager pauseManager;

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

        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            // CombatManager not found - combat features will be disabled
        }
        enemyIndicator = FindFirstObjectByType<EnemyIndicator>();
        timeIndicator = FindFirstObjectByType<TimeIndicator>();
        timeSpeedEffect = FindFirstObjectByType<TimeSpeedEffect>();
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
        
        // float currentTimeRate = inGameMinVSSec;
        if (tempSecond >= 0.08)
        {
            Minutes += 1;
            tempSecond = 0;
        }
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
        shopManager.disableShop();

        if (buildController != null)
        {
            buildController.DisableBuildMode();
        }

        buildController.HideDeleteIcon();
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Hide();

        cropGrowthOnAll(1);

        shopButton.interactable = false;
        shopIcon.color = nightShop;
        isDay = false;
        buttonText.text = "End Night";

        if (WeatherManager.Instance != null)
        {
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

        Coroutine skyboxCor = StartCoroutine(Skybox(skyboxDay, skyboxNight, 5f));
        skyboxCoroutines.Add(skyboxCor);

        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        sceneLight.intensity = nightIntensity;
        RenderSettings.fogDensity = nightFog;

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
        foreach (AnimalStructure structure in animalStructures)
        {
            if (structure != null)
            {
                structure.OnNewDay();
            }
        }
        foreach (BarracksStructure barracks in barracksStructures)
        {
            if (barracks != null)
            {
                barracks.OnDayNightChanged(false);
            }
        }
        if (WeatherManager.Instance != null)
        {
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

        cropGrowthOnAll(2);
        isDay = true;
        buttonText.text = "Start Night";
        if (shopManager != null)
        {
            shopManager.UpdateShopButtonStateForTimeControls();
        }

        Coroutine skyboxCor = StartCoroutine(Skybox(skyboxNight, skyboxDay, flag == 0 ? 0f : 5f));
        skyboxCoroutines.Add(skyboxCor);

        if (source1 != null) source1.Stop();
        if (source2 != null) source2.Play();

        sceneLight.intensity = dayIntensity;
        RenderSettings.fogDensity = dayFog;
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

        if (value == 0)
        {
        }
        else if (value == 5)
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

            // Combat report display temporarily disabled

            // Award survival bonus for completing the night
            if (days > 0 && lastSurvivalRewardDay < days)
            {
                lastSurvivalRewardDay = days;
                AwardSurvivalBonus();
            }

            // Check for newly unlocked structures in the morning
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.CheckForNewlyUnlockedStructuresMorning();
            }
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

    int dayInCycle = value % 20;

    // Seasons change **only at 5 AM**
    if (hours == 5)
    {
        switch (dayInCycle)
        {
            case 0:
                setSeason(1); // Spring
                break;
            case 5:
                setSeason(2); // Summer
                break;
            case 10:
                setSeason(3); // Fall
                break;
            case 15:
                setSeason(4); // Winter
                break;
        }
    }

    // Year increment at multiples of 20 days (optional)
    if (dayInCycle == 0 && value != 0 && hours == 5) 
    {
        if (yearAudioSource != null)
            yearAudioSource.Play();

        years++;
        YearsChanged = true;
    }

    UpdateDayCountUI();
}






    public void UpdateEnemyIndicatorForSeason(int season)
    {
        if (CheatManager.Instance != null && CheatManager.Instance.IsUnlockEnemyAnimalsActive())
        {
            if (enemyIndicator != null) enemyIndicator.MakeAllEnemiesVisible();
            return;
        }
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
        // else: Enemy indicator not available
    }

    private void UpdateDayCountUI()
    {
        if (dayCountText != null) {
            dayCountText.text = $"Day {Days}";

        }
            
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
        currentSeason = season;
        string text;
        switch (season)
        {
            case 1:
                if (YearsChanged)
                {
                    text = $"Year {Years} done!!\nSpring!!!!";
                    seasonIcon.sprite = spring;
                }
                else
                {
                    text = "Spring!!";
                    seasonIcon.sprite = spring;
                }
                break;
            case 2:
                YearsChanged = false;
                text = "Summer!!";
                seasonIcon.sprite = summer;
                break;
            case 3:
                text = "Fall!!";
                seasonIcon.sprite = fall;
                break;
            case 4:
                text = "Winter!!";
                seasonIcon.sprite = winter;
                break;
            default:
                text = "";
                break;
        }

        SetSeasonWeather();

        chooseAnimalProductForSeason();

        foreach (FarmHouseStructure farmHouse in farmHouseStructures)
        {
            if (farmHouse != null)
            {
                farmHouse.OnNewSeason(season);
            }
        }

        if (!isFirstDay)
        {
            combatManager.increaseAfterSeason();
            combatManager.scaleTimeBySeason();
        }

        combatManager.SetSeason(season);

        UpdateEnemyIndicatorForSeason(season);

        if (!isFirstDay)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
            {
                if (NotificationManager.Instance != null)
                {
                    string unlocksText = null;
                    if (GameLoopManager.Instance != null)
                    {
                        string[] newStructs = GameLoopManager.Instance.GetAndMarkNewlyUnlockedStructures(Days);
                        if (newStructs != null && newStructs.Length > 0)
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            sb.AppendLine("New Structures Unlocked:");
                            foreach (var sname in newStructs)
                            {
                                sb.AppendLine($"• {sname}");
                            }
                            unlocksText = sb.ToString();
                        }
                    }

                    NotificationManager.ShowSeasonalBlocking(season, unlocksText);
                }
            }
        }


    }

    private void SetSeasonWeather()
    {
        if (WeatherManager.Instance != null)
        {
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
        if (seasonNotificationCoroutine != null)
        {
            StopCoroutine(seasonNotificationCoroutine);
            seasonNotification.gameObject.SetActive(false);
        }

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

    private void StartProductionNotification(string message, float duration)
    {
        if (productionNotificationCoroutine != null)
        {
            StopCoroutine(productionNotificationCoroutine);
            productionNotification.gameObject.SetActive(false);
        }

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

        // Hide previous season's bonus indicators
        AnimalProductionIndicator.HideAllBonuses();

        //bonus falls on the same product
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
            
            if (NotificationManager.Instance != null)
            {
                NotificationManager.ShowAchievement("Double Production!", $"{fullAnimalName} earning 100% more! (Lasts entire season)");
            }

            AnimalProductionIndicator.ShowOneProductionBonus(animal);
        }
        //bonus falls different products
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
                
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.ShowSuccess("Production Boost!", $"{fullAnimalName1} & {fullAnimalName2} +50% output! (Lasts entire season)", 2.5f);
                }
                StartProductionNotification(message, 5);
                
                AnimalProductionIndicator.ShowTwoProductionBonuses(animal1, animal2);

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

                // Update the visual indicator even during tutorial
                AnimalProductionIndicator.ShowTwoProductionBonuses(animal1, animal2);

                // Block production notifications during tutorial
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    return;
                }


                string message = $"Animal production increased for <b>{fullAnimalName1}</b> by <b>{(increasePercent * 100) / 3}%</b> and <b>{fullAnimalName2}</b> by <b>{(increasePercent * 100) / 3}%</b>!";

                // StartProductionNotification(message, 5);
                
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.ShowSuccess("Production Boost!", $"{fullAnimalName1} & {fullAnimalName2} +50% output! (Lasts entire season)", 2.5f);
                }
                
                StartProductionNotification(message, 5);

                // if (doubleProductionSource != null)
                // {
                //     doubleProductionSource.Play();
                // }
            }
        }
    }

    public void ShowSimplifiedTutorialSeasonBonus()
    {
        string tutorialMessage = "Each season brings special bonuses to your farm animals! You can see which animals produce more by checking the price panel.";

        if (NotificationManager.Instance != null)
        {
            NotificationManager.ShowNotification("Seasonal Bonuses", tutorialMessage, "Info", 4f);
        }

        ProductionBoosts productionBoostsManager = FindFirstObjectByType<ProductionBoosts>();
        if (productionBoostsManager != null)
        {
            chooseAnimalProductForSeason();
        }
    }

    public void ShowPeteSeasonNotification(int season)
    {
        if (TutorialManager.Instance == null) return;

        string seasonName = GetSeasonName(season);
        string peteMessage = GetPeteSeasonMessage(season);

        var seasonStep = new TutorialStep
        {
            stepId = $"pete_season_{season}",
            title = $"Pete's {seasonName} Update",
            instructionText = peteMessage,
            triggerToWaitFor = TutorialTrigger.None
        };

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
                message = "Animals energetic • Perfect farming weather • 2 random animals get +50% production";
                theme = "Success";
                break;
            case 2: // Summer  
                title = "Summer Heat!";
                message = "Faster crop growth • Wolves extra aggressive • 2 random animals get +50% production";
                theme = "Warning";
                break;
            case 3: // Fall
                title = "Fall Harvest Season!";
                message = "Animals eat more, produce more • Expand livestock • 2 random animals get +50% production";
                theme = "Achievement";
                break;
            case 4: // Winter
                title = "Winter is Here!";
                message = "Slower crops • Snow incoming • Tougher enemies • 2 random animals get +50% production";
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

    private bool IsInTutorialMode()
    {
        return TutorialManager.Instance != null &&
               TutorialManager.Instance.IsTutorialActive() &&
               TutorialManager.Instance.enabled;
    }

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

        setSeason(season);

        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.TriggerSeasonChanged(season);
        }
    }

    public int GetDays() => days;

    public void SetLastSurvivalRewardDay(int day)
    {
        lastSurvivalRewardDay = day;
    }
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

    private void AwardSurvivalBonus()
    {
        const int SURVIVAL_BONUS = 100;
        
        if (NotificationManager.Instance != null)
        {
            string[] funnyMessages = {
                "You didn't die! Here's some pocket money.",
                "Congratulations! You survived another night of farm chaos!",
                "Farm life didn't kill you today. Bonus points!",
                "You made it! The chickens didn't revolt... yet.",
                "Survival bonus: Because farming is basically war.",
                "You lived! Now go spend this on more questionable life choices.",
                "Another day, another dollar... wait, 100 dollars!",
                "Farmers gonna farm, but you survived to tell the tale!",
                "You beat the night! The wolves are disappointed.",
                "Survival of the fittest farmer! You win this round.",
                "You didn't get eaten by your own livestock. Impressive!",
                "Farm life: Where 'surviving the night' is an achievement.",
                "You made it through! The silo didn't collapse on you.",
                "Congratulations! Your farm didn't burn down overnight.",
                "You survived! Now go hug a chicken or something."
            };
            
            string randomMessage = funnyMessages[Random.Range(0, funnyMessages.Length)];
            NotificationManager.ShowAchievement($"Day {days} Survived!", randomMessage, 4f);
        }
        
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(SURVIVAL_BONUS, transform.position);
        }
    }
}