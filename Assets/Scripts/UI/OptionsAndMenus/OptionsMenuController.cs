using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OptionsMenuController : MonoBehaviour
{
    public static OptionsMenuController Instance { get; private set; }

    [Header("Audio")]
    public Slider volumeSlider;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle vsyncToggle;
    public TMP_Dropdown slotDropdown;
    public Button saveButton;

    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private TextMeshProUGUI saveFeedbackText;


    private Resolution[] resolutions;

    private float initialVolume;
    private bool initialFullscreen;
    private int initialResolution;
    private int initialQuality;
    private bool initialVSync;

    private int selectedSlot = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameObject.activeSelf)
                HideMenu();
            else
                ShowMenu();
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);  
    }

    private void Start()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            int currentResIndex = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(new TMP_Dropdown.OptionData(option));
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
            }
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> options = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(options);
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vsyncToggle.onValueChanged.AddListener(SetVSync);
        }

        initialVolume = AudioListener.volume;
        initialFullscreen = Screen.fullScreen;
        initialResolution = GetCurrentResolutionIndex();
        initialQuality = QualitySettings.GetQualityLevel();
        initialVSync = QualitySettings.vSyncCount > 0;


        volumeSlider.onValueChanged.AddListener(_ => OnOptionChanged());
        fullscreenToggle.onValueChanged.AddListener(_ => OnOptionChanged());
        resolutionDropdown.onValueChanged.AddListener(_ => OnOptionChanged());
        qualityDropdown.onValueChanged.AddListener(_ => OnOptionChanged());
        vsyncToggle.onValueChanged.AddListener(_ => OnOptionChanged());


        if (applyButton != null) applyButton.onClick.AddListener(ApplyChanges);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelChanges);


        if (backToMenuButton != null)
        {
            bool inMainMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene";
            backToMenuButton.interactable = !inMainMenu;
        }

        UpdateButtonStates();

        if (slotDropdown != null)
        {
            slotDropdown.ClearOptions();
            List<string> slotOptions = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                var saveData = GameSaveHelper.LoadFromSlot(i);
                if (saveData != null)
                    slotOptions.Add($"Slot {i + 1}: Day {saveData.day}, Money {saveData.money}");
                else
                    slotOptions.Add($"Empty Slot {i + 1}");
            }
            slotDropdown.AddOptions(slotOptions);
            slotDropdown.onValueChanged.AddListener(OnSlotChanged);
        }

        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);
    }

    private void OnEnable()
    {
        if (backToMenuButton != null)
        {
            bool inMainMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene";
            backToMenuButton.interactable = !inMainMenu;
        }
    }

    private int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                return i;
        }
        return 0;
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    public void SetVSync(bool value)
    {
        QualitySettings.vSyncCount = value ? 1 : 0;
    }

    public void ShowMenu()
    {
        gameObject.SetActive(true);
    }

    public void HideMenu()
    {
        gameObject.SetActive(false);
    }

    private void OnOptionChanged()
    {
        bool changed =
            !Mathf.Approximately(volumeSlider.value, initialVolume) ||
            fullscreenToggle.isOn != initialFullscreen ||
            resolutionDropdown.value != initialResolution ||
            qualityDropdown.value != initialQuality ||
            vsyncToggle.isOn != initialVSync;

        applyButton.interactable = changed;
        cancelButton.interactable = changed;
    }

    private void ApplyChanges()
    {
        initialVolume = volumeSlider.value;
        initialFullscreen = fullscreenToggle.isOn;
        initialResolution = resolutionDropdown.value;
        initialQuality = qualityDropdown.value;
        initialVSync = vsyncToggle.isOn;

        SetVolume(initialVolume);
        SetFullscreen(initialFullscreen);
        SetResolution(initialResolution);
        SetQuality(initialQuality);
        SetVSync(initialVSync);

        UpdateButtonStates();
    }

    private void CancelChanges()
    {
        volumeSlider.value = initialVolume;
        fullscreenToggle.isOn = initialFullscreen;
        resolutionDropdown.value = initialResolution;
        qualityDropdown.value = initialQuality;
        vsyncToggle.isOn = initialVSync;

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        applyButton.interactable = false;
        cancelButton.interactable = false;
    }
    public void OnBackToMenuButtonClicked()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenuScene")
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadSceneWithLoading("MainMenuScene");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
            }
        }
        HideMenu();
        PauseManager pauseManager = FindFirstObjectByType<PauseManager>();
        if (pauseManager != null)
            pauseManager.playGame();
    }

        public void OnCloseButtonClicked()
    {
        HideMenu();
        PauseManager pauseManager = FindFirstObjectByType<PauseManager>();
        if (pauseManager != null)
            pauseManager.playGame();
    }

    private void OnSlotChanged(int index)
    {
        selectedSlot = index;
    }

    private GameSaveData GatherCurrentGameState()
    {
        GameSaveData data = new GameSaveData();
        data.money = MoneyManager.Instance != null ? MoneyManager.Instance.GetCurrentMoney() : 0;

        if (InventoryManager.Instance != null)
        {
            data.sunflowerAmount = InventoryManager.Instance.GetItemCount("Sunflower");
            data.wheatAmount = InventoryManager.Instance.GetItemCount("Wheat");
            data.carrotsAmount = InventoryManager.Instance.GetItemCount("Carrots");
        }
        else
        {
            data.sunflowerAmount = 0;
            data.wheatAmount = 0;
            data.carrotsAmount = 0;
        }

        if (NightManager.Instance != null)
        {
            data.day = NightManager.Instance.Days;
            data.season = NightManager.Instance.GetCurrentSeason();
        }

        data.structures = new List<StructureSaveData>();
        foreach (var structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (structure.gameObject.name == "BuildGhost") continue;

            var save = new StructureSaveData
            {
                type = structure.GetStructureName(),
                position = structure.transform.position,
                health = structure.GetCurrentHealth(),
                rotation = structure.transform.rotation
            };

            if (structure is AnimalStructure animal)
            {
                save.animalCount = animal.AnimalCount;
                save.maxAnimalCount = animal.MaxAnimalCount;
                save.animalType = animal.GetAnimalType.ToString();
                save.isProducing = animal.IsProducing;
                save.productReady = animal.ProductReady;
                save.productionProgress = animal.ProductionProgress;
            }
            else if (structure is CropStructure crop)
            {
                save.cropType = crop.CurrentCropType.ToString();
                save.isGrowing = crop.IsGrowing;
                save.cropReady = crop.CropReady;
            }
            else if (structure is BarracksStructure barracks)
            {
                save.armyAnimalCount = barracks.ArmyAnimalCount;
            }

            data.structures.Add(save);
        }
        return data;
    }

    public void OnSaveButtonClicked()
    {
        GameSaveData saveData = GatherCurrentGameState();
        GameSaveHelper.SaveToSlot(selectedSlot, saveData);
        Debug.Log($"Game saved to slot {selectedSlot} with money: {saveData.money}");

        if (saveFeedbackText != null)
        {
            saveFeedbackText.text = $"Saved to Slot {selectedSlot + 1}: Day {saveData.day}, Money {saveData.money}";
            saveFeedbackText.color = Color.green;
        }

        UpdateSlotDropdown();
    }

    private void UpdateSlotDropdown()
    {
        if (slotDropdown == null) return;
        slotDropdown.ClearOptions();
        List<string> slotOptions = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            var saveData = GameSaveHelper.LoadFromSlot(i);
            if (saveData != null)
                slotOptions.Add($"Slot {i + 1}: Day {saveData.day}, Money {saveData.money}");
            else
                slotOptions.Add($"Empty Slot {i + 1}");
        }
        slotDropdown.AddOptions(slotOptions);
        slotDropdown.value = selectedSlot;
        slotDropdown.RefreshShownValue();
    }
}