using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NightManager : MonoBehaviour
{
    // Singleton instance
    public static NightManager Instance { get; private set; }

    // Start night button
    [SerializeField] private Button startNightButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    // Light
    [SerializeField] private Light sceneLight;
    [SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
    [SerializeField] private float intensity = 0.3f;
    [SerializeField] private Gradient morningToDayGradient;
    [SerializeField] private Gradient DayToAfternoonGradient;
    [SerializeField] private Gradient AfternoonToNightGradient;
    [SerializeField] private Gradient nightToMorningGradient;

    // Skyboxes
    [SerializeField] private Texture2D skyboxMorning;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxAfternoon;
    [SerializeField] private Texture2D skyboxNight;

    // Animals
    [SerializeField] private WolfMovement wolfMovement;
    [SerializeField] private ChickenMovement chicken;

    // Time indicator icons
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
    [SerializeField] private Button shopButton;
    private Color dayShop = Color.white;
    private Color nightShop = Color.grey * 0.9f;
    public Image shopIcon;
    [SerializeField] private ShopUIManager shopManager;

    // Item delete icon
    [SerializeField] private BuildController buildController;

    // Time management
    [SerializeField] private TextMeshProUGUI seasonNotification;
    private float speedUp = 1f;
    private bool isPaused = false;
    [SerializeField] private bool isFast = false;
    [SerializeField] private TextMeshProUGUI timeText;

    // Time
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
    [SerializeField] private Image seasonIcon;
    [SerializeField] private Sprite summer;
    [SerializeField] private Sprite winter;
    [SerializeField] private Sprite spring;
    [SerializeField] private Sprite fall;

    // Fog density
    [SerializeField] private float morningFog = 0.005f;
    [SerializeField] private float dayFog = 0.003f;
    [SerializeField] private float nightFog = 0.009f;

    // Light intensity
    [SerializeField] private float nightIntensity = 0.03f;
    [SerializeField] private float dayIntensity = 2f;
    [SerializeField] private float dayTemp = 6000f;
    [SerializeField] private float morningTemp = 3000f;
    [SerializeField] private float eveningTemp = 9000f;

    // Pause game manager
    [SerializeField] private PauseManager pauseManager;

    // Structures
    private List<AnimalStructure> animalStructures = new List<AnimalStructure>();
    public CropStructure cropStructure; // Optional, for single crop plot

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
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
        }
    }

    public void Update()
    {
        if (isPaused)
        {
            return;
        }

        tempSecond += Time.deltaTime * speedUp;
        timeText.text = $"{Hours:D2}:{Minutes:D2}";

        if (tempSecond >= 0.0625f) // 1 in-game minute = 0.0625 seconds (1 day ≈ 12 minutes)
        {
            Minutes += 1;
            tempSecond = 0;
        }
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
        speedUp = isFast ? 5f : 1f; // Fast-forward: 1 day ≈ 2.4 minutes
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

        StartCoroutine(Skybox(skyboxDay, skyboxNight, 5f));
        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        sceneLight.intensity = nightIntensity;
        RenderSettings.fogDensity = nightFog;
        timeOfDayIcon.sprite = nightIcon;

        wolfMovement.SpawnAndMoveWolf();
        chicken.SpawnAndMove();
    }

    private void StartDay(int flag)
    {
        // Notify animal structures
        if (animalStructures != null)
        {
            foreach (AnimalStructure structure in animalStructures)
            {
                if (structure != null)
                {
                    structure.OnNewDay();
                }
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

        wolfMovement.despawn();
        chicken.despawn();
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
            StartCoroutine(showText("Night starting soon!!", 5f));
            StartCoroutine(Skybox(skyboxDay, skyboxAfternoon, 2f));
            StartCoroutine(LightingChanges(DayToAfternoonGradient, 2f));
            sceneLight.colorTemperature = 2000f;
        }
        else if (value == 20)
        {
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
                break;
            case 2:
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
        }
    }

    public void UnregisterAnimalStructure(AnimalStructure structure)
    {
        if (structure != null)
        {
            animalStructures.Remove(structure);
        }
    }
}