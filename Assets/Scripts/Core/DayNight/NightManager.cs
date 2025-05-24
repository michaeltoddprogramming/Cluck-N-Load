using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class NightManager : MonoBehaviour
{
    // Singleton instance
    public static NightManager Instance { get; private set; }

    // Start night button
    [Header("Start NIght button")]
    [SerializeField] private Button startNightButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    // Light
    [Header("Lighting stuff")]
    [SerializeField] private Light sceneLight;
    [SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
    [SerializeField] private float intensity = 0.3f;
    [SerializeField] private Gradient morningToDayGradient;
    [SerializeField] private Gradient DayToAfternoonGradient;
    [SerializeField] private Gradient AfternoonToNightGradient;
    [SerializeField] private Gradient nightToMorningGradient;

    // Skyboxes
    [Header("Skyboxes")]
    [SerializeField] private Texture2D skyboxMorning;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxAfternoon;
    [SerializeField] private Texture2D skyboxNight;

    // Time indicator icons
    [Header("Time Indicator")]
    [SerializeField] private Image timeOfDayIcon;
    [SerializeField] private Sprite dayIcon;
    [SerializeField] private Sprite nightIcon;

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
    [SerializeField] private ShopUIManager shopManager;

    // Item delete icon
    [Header("Delete Icon")]
    [SerializeField] private BuildController buildController;

    // Time management
    [Header("Time Management")]
    [SerializeField] private TextMeshProUGUI seasonNotification;
    [SerializeField] private TextMeshProUGUI productionNotification;
    [SerializeField] private float speedUp = 1f;
    [SerializeField] private float speedOfFast = 5f;

    [Tooltip("How many in-game minutes per real life second (0.0625f -> 1 in-game minute = 0.0625 seconds (1 day ≈ 12 minutes))")]
    [SerializeField] private float inGameMinVSSec = 0.0625f;
    private bool isPaused = false;
    [SerializeField] private bool isFast = false;
    [SerializeField] private TextMeshProUGUI timeText;

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
        set { days = value; OnDayChange(value); }
    }

    [SerializeField] private int years;
    public int Years
    {
        get => years;
        set => years = value;
    }
    private float tempSecond;

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
    [SerializeField] private float dayTemp = 6000f;
    [SerializeField] private float morningTemp = 3000f;
    [SerializeField] private float eveningTemp = 9000f;

    // Pause game manager
    [Header("Pause Game Manager")]
    [SerializeField] private PauseManager pauseManager;

    // Structures
    private List<AnimalStructure> animalStructures = new List<AnimalStructure>();
    private List<BarracksStructure> barracksStructures = new List<BarracksStructure>();
    public CropStructure cropStructure; // Optional, for single crop plot

    [Header("Wolf Spawning")]
    [SerializeField] private UnitSpawner unitSpawner;
    [SerializeField] private int baseWolfCount = 3;
    [SerializeField] private int additionalWolvesPerDay = 1;
    [SerializeField] private float spawnInterval = 20f;
    [SerializeField] private int maxWolvesAtOnce = 10;
    
    private List<Wolf> activeWolves = new List<Wolf>();
    private float nextWolfSpawnTime;

    public void RegisterWolf(Wolf wolf)
    {
        if (wolf != null && !activeWolves.Contains(wolf))
        {
            activeWolves.Add(wolf);
            Debug.Log($"Registered wolf: {wolf.name}, Active wolves: {activeWolves.Count}");
        }
    }

    public void UnregisterWolf(Wolf wolf)
    {
        if (wolf != null && activeWolves.Remove(wolf))
        {
            Debug.Log($"Unregistered wolf: {wolf.name}, Active wolves: {activeWolves.Count}");
        }
    }
    
// Add this method to your NightManager class
// Fix the SpawnWolvesOverTime coroutine
private IEnumerator SpawnWolvesOverTime()
{
    // Calculate how many wolves to spawn tonight (increasing difficulty)
    int totalWolvesToSpawn = baseWolfCount + (days * additionalWolvesPerDay);
    int wolvesSpawned = 0;
    
    Debug.Log($"🐺 NIGHT {days}: Planning to spawn {totalWolvesToSpawn} wolves tonight");
    
    // Initial delay before first spawn
    yield return new WaitForSeconds(3f);
    
    // Keep spawning wolves until we reach the limit or day breaks
    while (wolvesSpawned < totalWolvesToSpawn && !isDay)
    {
        // Don't spawn more if we're at max concurrent wolves
        if (activeWolves.Count < maxWolvesAtOnce)
        {
            // Use new specific wolf spawner method
            if (unitSpawner != null)
            {
                unitSpawner.SpawnWolf();
                wolvesSpawned++;
                Debug.Log($"🐺 Wolf {wolvesSpawned}/{totalWolvesToSpawn} spawned! Active wolves: {activeWolves.Count}");
            }
            else
            {
                Debug.LogError("CRITICAL ERROR: UnitSpawner is null!");
                break;
            }
        }
        else
        {
            Debug.Log($"Maximum active wolves reached ({maxWolvesAtOnce}). Waiting for some to die before spawning more.");
        }
        
        // Wait before spawning next wolf
        yield return new WaitForSeconds(spawnInterval);
    }
    
    Debug.Log($"🐺 Wolf spawning complete: {wolvesSpawned} wolves spawned");
}

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
    }

    private void Start()
    {
        Hours = 7;
        Years = 1;
        seasonNotification.gameObject.SetActive(false);
        setSeason(1);

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

        chooseAnimalProductForSeason();
    }

    private void Update()
    {
        if (isPaused)
        {
            return;
        }

        tempSecond += Time.deltaTime * speedUp;
        timeText.text = $"{Hours:D2}:{Minutes:D2}";

        if (tempSecond >= inGameMinVSSec) // 1 in-game minute = 0.0625 seconds (1 day ≈ 12 minutes)
        {
            Minutes += 1;
            tempSecond = 0;
        }
                // Clean up any null references in the wolves list
        activeWolves.RemoveAll(wolf => wolf == null);
    }

    public void pauseTime()
    {
        isPaused = true;
    }

    public void playTime()
    {
        isPaused = false;
        isFast = false;
        speedUp = 1f;
    }

    public void fastForwardTime()
    {
        isFast = !isFast;
        isPaused = false;
        speedUp = isFast ? speedOfFast : 1f;
    }

    private void cropGrowthOnAll(int stage)
    {
        CropStructure[] allCrops = FindObjectsOfType<CropStructure>();
        foreach (CropStructure crop in allCrops)
        {
            if (crop.IsGrowing && !crop.CropReady)
            {
                crop.UpdateVisuals(stage);
                Debug.Log($"Advanced {crop.GetStructureName()} ({crop.CurrentCropType}) to growth stage {stage}");
            }
            else if (crop.CropReady)
            {
                Debug.Log($"Skipped {crop.GetStructureName()} ({crop.CurrentCropType}): Already ready to harvest");
            }
            else
            {
                Debug.Log($"Skipped {crop.GetStructureName()}: No crop planted");
            }
        }
    }

    private void StartNight(int flag)
    {
        shopManager.CloseShop();
        buildController.HideDeleteIcon();

        // Advance crops to stage 1
        cropGrowthOnAll(1);

        shopButton.interactable = false;
        shopIcon.color = nightShop;
        isDay = false;
        buttonText.text = "End Night";

        // Start wolf spawning when night begins
        if (unitSpawner != null)
        {
            StopAllCoroutines(); // Stop any existing spawn coroutines
            StartCoroutine(SpawnWolvesOverTime()); // Use the existing method
            Debug.Log($"Night {days}: Starting wolf spawning");
        }
        else
        {
            Debug.LogError("UnitSpawner reference missing in NightManager!");
        }

        StartCoroutine(Skybox(skyboxDay, skyboxNight, 5f));
        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        sceneLight.intensity = nightIntensity;
        RenderSettings.fogDensity = nightFog;
        timeOfDayIcon.sprite = nightIcon;

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
        // Destroy all remaining wolves when day starts
        foreach (Wolf wolf in activeWolves.ToList())
        {
            if (wolf != null)
            {
                wolf.OnDayNightChanged(false); // This should trigger the wolf to destroy itself
            }
        }
        activeWolves.Clear();

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

        // Advance growing crops to stage 2, preserve ready-to-harvest crops
        cropGrowthOnAll(2);

        shopButton.interactable = true;
        shopIcon.color = dayShop;
        isDay = true;
        buttonText.text = "Start Night";

        StartCoroutine(Skybox(skyboxNight, skyboxDay, flag == 0 ? 0f : 5f));
        if (source1 != null) source1.Stop();
        if (source2 != null) source2.Play();

        sceneLight.intensity = dayIntensity;
        RenderSettings.fogDensity = dayFog;
        timeOfDayIcon.sprite = dayIcon;
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
        sceneLight.transform.Rotate(Vector3.up, (4f / 1440f) * 360f, Space.World);
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
            StartDay(2);
            if (roosterMorningSource != null)
            {
                roosterMorningSource.Play();
            }
            RenderSettings.fogDensity = morningFog;
            StartCoroutine(Skybox(skyboxNight, skyboxMorning, 2f));
            StartCoroutine(LightingChanges(nightToMorningGradient, 2f));
            sceneLight.colorTemperature = 2000f;
        }
        else if (value == 7)
        {
            RenderSettings.fogDensity = dayFog;
            StartCoroutine(Skybox(skyboxMorning, skyboxDay, 2f));
            StartCoroutine(LightingChanges(morningToDayGradient, 2f));
            sceneLight.colorTemperature = 6000f;
        }
        else if (value == 16)
        {
            if (clockTickingSource != null)
            {
                clockTickingSource.Play();
            }

            StartCoroutine(showText("Night starting soon!!", 5f));
            StartCoroutine(Skybox(skyboxDay, skyboxAfternoon, 2f));
            StartCoroutine(LightingChanges(DayToAfternoonGradient, 2f));
            sceneLight.colorTemperature = 2000f;
        }
        else if (value == 20)
        {
            if (clockTickingSource.isPlaying)
            {
                clockTickingSource.Stop();
            }

            StartNight(2);
            StartCoroutine(Skybox(skyboxAfternoon, skyboxNight, 2f));
            StartCoroutine(LightingChanges(AfternoonToNightGradient, 2f));
            sceneLight.colorTemperature = 9000f;
        }
    }

    private void OnDayChange(int value)
    {
        if (value >= 5)
        {
            years++;
            if (yearAudioSource != null)
            {
                yearAudioSource.Play();
            }
            days = 0;
            hours = 7;
            minutes = 0;
            StartDay(0);
            setSeason(1);
            return;
        }

        if (value == 0) setSeason(1);
        else if (value == 1) setSeason(2);
        else if (value == 2) setSeason(3);
        else if (value == 3) setSeason(4);
        else if (value == 4) setSeason(5);
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
        string text;
        switch (season)
        {
            case 1:
                text = "Spring!!";
                seasonIcon.sprite = spring;
                chooseAnimalProductForSeason();
                break;
            case 2:
                text = "Summer!!";
                seasonIcon.sprite = summer;
                chooseAnimalProductForSeason();
                break;
            case 3:
                text = "Fall!!";
                seasonIcon.sprite = fall;
                chooseAnimalProductForSeason();
                break;
            case 4:
                text = "Winter!!";
                seasonIcon.sprite = winter;
                chooseAnimalProductForSeason();
                break;
            case 5:
                text = $"Year {Years} done!!";
                break;
            default:
                text = "";
                break;
        }
        StartCoroutine(showText(text, 5));
    }

    private IEnumerator showText(string message, float time)
    {
        seasonNotification.text = message;
        seasonNotification.gameObject.SetActive(true);
        yield return new WaitForSeconds(time);
        seasonNotification.gameObject.SetActive(false);
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
        if (structure != null && !animalStructures.Contains(structure))
        {
            animalStructures.Add(structure);
            Debug.Log($"Registered animal structure: {structure.GetStructureName()}");
        }
    }

    public void UnregisterAnimalStructure(AnimalStructure structure)
    {
        if (structure != null && animalStructures.Remove(structure))
        {
            Debug.Log($"Unregistered animal structure: {structure.GetStructureName()}");
        }
    }

    public void RegisterBarracksStructure(BarracksStructure barracks)
    {
        if (barracks != null && !barracksStructures.Contains(barracks))
        {
            barracksStructures.Add(barracks);
            Debug.Log($"Registered barracks: {barracks.GetStructureName()}");
        }
    }

    public void UnregisterBarracksStructure(BarracksStructure barracks)
    {
        if (barracks != null && barracksStructures.Remove(barracks))
        {
            Debug.Log($"Unregistered barracks: {barracks.GetStructureName()}");
        }
    }

    public void chooseAnimalProductForSeason()
    {
        float sameProduct = Random.Range(0f, 1f);
        int product1 = Random.Range(1, 6);
        int product2 = Random.Range(1, 6);
        float increasePercent = 1.5f; // 50% increase
        float sameProductIncreasePercent = 2f; // 100% increase

        // Reset all animal production to base before applying bonuses
        foreach (AnimalStructure animalStructure in animalStructures)
        {
            animalStructure.resetAnimalProductionAmount();
        }

        if (sameProduct <= 0.05f)
        // if (sameProduct <= f)
        {
            string animal = determineAnimalProduct(product1);
            string fullAnimalName = getFullAnimalName(animal);
            if (animal == "E")
            {
                Debug.LogError("Invalid animal product determined.");
                return;
            }

            foreach (AnimalStructure animalStructure in animalStructures)
            {
                animalStructure.updateAnimalProductionAmount(animal, sameProductIncreasePercent);
            }

            string message = $"Animal production increased for <b>{fullAnimalName}</b> by <b>{(sameProductIncreasePercent * 100) / 2}</b>%!\nLUCKY!!! You got a <b>double</b> production bonus!";


            StartCoroutine(showProductionText(message, 5));

            // productionNotification.text = $"Animal production increased for {fullAnimalName} by {increasePercent * 100}!\nLUCKY!!! You got a double production bonus!";
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

                string message = $"Animal production increased for <b>{fullAnimalName1}</b> by <b>{(increasePercent * 100) / 3}%</b> and <b>{fullAnimalName2}</b> by <b>{(increasePercent * 100) / 3}%</b>!";

                StartCoroutine(showProductionText(message, 5));

                if (doubleProductionSource != null)
                {
                    doubleProductionSource.Play();
                }

                // productionNotification.text = $"Animal production increased for {fullAnimalName1} by {increasePercent * 100}and {fullAnimalName2} by {increasePercent * 100}!";
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

                string message = $"Animal production increased for <b>{fullAnimalName1}</b> by <b>{(increasePercent * 100) / 3}%</b> and <b>{fullAnimalName2}</b> by <b>{(increasePercent * 100) / 3}%</b>!";

                StartCoroutine(showProductionText(message, 5));

                // productionNotification.text = $"Animal production increased for {fullAnimalName1} by {increasePercent * 100}and {fullAnimalName2} by {increasePercent * 100}!";
            }
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
    }
}