using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OptionsMenuController : MonoBehaviour
{
    public static OptionsMenuController Instance { get; private set; }

    [Header("Tab System")]
    public TabbedSettingsController tabbedController;

    [Header("Audio Settings Tab")]
    public Slider volumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle muteToggle;

    [Header("Display Settings Tab")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle vsyncToggle;
    public Slider brightnessSlider;

    [Header("Game Settings Tab")]
    public TMP_Dropdown slotDropdown;
    public Button saveButton;
    public Toggle autosaveToggle;
    public TMP_Dropdown difficultyDropdown;
    public Toggle tutorialsToggle;

    [Header("Controls Tab")]
    public Slider mouseSensitivitySlider;
    public Toggle invertMouseToggle;
    public Button resetKeybindsButton;

    [Header("Navigation Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI saveFeedbackText;


    private Resolution[] resolutions;

    // Audio settings tracking
    private float initialVolume;
    private float initialMusicVolume;
    private float initialSfxVolume;
    private bool initialMute;

    // Display settings tracking
    private bool initialFullscreen;
    private int initialResolution;
    private int initialQuality;
    private bool initialVSync;
    private float initialBrightness;

    // Game settings tracking
    private int selectedSlot = 0;
    private bool initialAutosave;
    private int initialDifficulty;
    private bool initialTutorials;

    // Control settings tracking
    private float initialMouseSensitivity;
    private bool initialInvertMouse;

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
        InitializeAudioSettings();
        InitializeDisplaySettings();
        InitializeGameSettings();
        InitializeControlSettings();
        
        StoreInitialValues();
        SetupEventListeners();
        SetupNavigationButtons();
        
        UpdateButtonStates();
    }

    private void InitializeAudioSettings()
    {
        // Master volume - FIXED: Load from PlayerPrefs
        if (volumeSlider != null)
        {
            float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedMasterVolume;
            AudioListener.volume = savedMasterVolume; // Apply immediately
        }
        
        // Music volume (if you have separate music control)
        if (musicVolumeSlider != null)
        {
            float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.value = savedMusicVolume;
            SetMusicVolume(savedMusicVolume); // Apply the saved volume immediately
        }
        
        // SFX volume (if you have separate SFX control)
        if (sfxVolumeSlider != null)
        {
            float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.value = savedSFXVolume;
            SetSFXVolume(savedSFXVolume); // Apply the saved volume immediately
        }
        
        // Mute toggle
        if (muteToggle != null)
        {
            muteToggle.isOn = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        }
    }

    private void InitializeDisplaySettings()
    {
        // Fullscreen
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }
        
        // Resolution
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
        }
        
        // Quality
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> options = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(options);
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }
        
        // VSync
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
        }
        
        // Brightness
        if (brightnessSlider != null)
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1f);
            brightnessSlider.value = savedBrightness;
            ApplyBrightness(savedBrightness); // Apply the saved brightness immediately
        }
    }

    private void InitializeGameSettings()
    {
        // Save slots
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
        
        // Save button
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);
            
        // Autosave
        if (autosaveToggle != null)
        {
            autosaveToggle.isOn = PlayerPrefs.GetInt("AutoSave", 1) == 1;
        }
        
        // Difficulty (if implemented)
        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = PlayerPrefs.GetInt("Difficulty", 1); // Default to normal
        }
        
        // Tutorials
        if (tutorialsToggle != null)
        {
            tutorialsToggle.isOn = PlayerPrefs.GetInt("ShowTutorials", 1) == 1;
        }
    }

    private void InitializeControlSettings()
    {
        // Mouse sensitivity
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        }
        
        // Invert mouse
        if (invertMouseToggle != null)
        {
            invertMouseToggle.isOn = PlayerPrefs.GetInt("InvertMouse", 0) == 1;
        }
        
        // Reset keybinds button
        if (resetKeybindsButton != null)
        {
            resetKeybindsButton.onClick.AddListener(ResetKeybinds);
        }
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

    // Audio Settings
    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value); // FIXED: Save master volume to PlayerPrefs
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        
        // Apply to background music in MainMenuController
        MainMenuController mainMenu = FindFirstObjectByType<MainMenuController>();
        if (mainMenu != null && mainMenu.backgroundMusic != null)
        {
            mainMenu.backgroundMusic.volume = value;
        }
        
        // FIXED: Try to find music objects by tag, but handle missing tag gracefully
        try
        {
            GameObject[] musicObjects = GameObject.FindGameObjectsWithTag("Music");
            foreach (GameObject obj in musicObjects)
            {
                AudioSource audioSource = obj.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.volume = value;
                }
            }
        }
        catch (UnityException)
        {
            // Tag doesn't exist - find music by alternative methods
            Debug.LogWarning("[OptionsMenu] 'Music' tag not found. Add it in Project Settings → Tags, or tag your music AudioSources.");
        }
        
        // FALLBACK: Apply to common music source names (BackgroundMusic, BGM, Music, etc.)
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allSources)
        {
            string objName = source.gameObject.name.ToLower();
            // Check if this looks like a music source by name
            if (objName.Contains("music") || objName.Contains("bgm") || objName.Contains("background"))
            {
                source.volume = value;
            }
        }
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        
        // Apply to AudioManager
        if (AudioManager.Instance != null)
        {
            AudioSource audioSource = AudioManager.Instance.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = value;
            }
        }
        
        // FIXED: Try to find SFX objects by tag, but handle missing tag gracefully
        try
        {
            GameObject[] sfxObjects = GameObject.FindGameObjectsWithTag("SFX");
            foreach (GameObject obj in sfxObjects)
            {
                AudioSource audioSource = obj.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.volume = value;
                }
            }
        }
        catch (UnityException)
        {
            // Tag doesn't exist - find SFX by alternative methods
            Debug.LogWarning("[OptionsMenu] 'SFX' tag not found. Add it in Project Settings → Tags, or tag your SFX AudioSources.");
        }
        
        // FALLBACK: Apply to all non-music audio sources (excluding music/bgm sources)
        // This applies SFX volume to all "other" audio sources that aren't clearly music
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allSources)
        {
            string objName = source.gameObject.name.ToLower();
            // Skip sources that are clearly music
            if (objName.Contains("music") || objName.Contains("bgm") || objName.Contains("background"))
                continue;
                
            // Skip if it's in MainMenuController (that's music)
            if (source.GetComponent<MainMenuController>() != null)
                continue;
            
            // Apply SFX volume to everything else (UI sounds, game sounds, etc.)
            source.volume = value;
        }
    }

    public void SetMute(bool value)
    {
        PlayerPrefs.SetInt("AudioMuted", value ? 1 : 0);
        AudioListener.volume = value ? 0f : (volumeSlider != null ? volumeSlider.value : 1f);
    }

    // Display Settings
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

    public void SetBrightness(float value)
    {
        PlayerPrefs.SetFloat("Brightness", value);
        ApplyBrightness(value);
    }
    
    private void ApplyBrightness(float value)
    {
        // Adjust ambient light intensity (affects overall scene brightness)
        RenderSettings.ambientIntensity = value;
        
        // Optionally adjust reflection intensity for more visual impact
        RenderSettings.reflectionIntensity = value;
        
        // If you have a directional light (sun), adjust its intensity as well
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                // Store original intensity if not already stored
                float originalIntensity = PlayerPrefs.GetFloat($"OriginalLightIntensity_{light.GetInstanceID()}", light.intensity);
                if (!PlayerPrefs.HasKey($"OriginalLightIntensity_{light.GetInstanceID()}"))
                {
                    PlayerPrefs.SetFloat($"OriginalLightIntensity_{light.GetInstanceID()}", light.intensity);
                }
                light.intensity = originalIntensity * value;
            }
        }
    }

    // Game Settings
    public void SetAutosave(bool value)
    {
        PlayerPrefs.SetInt("AutoSave", value ? 1 : 0);
    }

    public void SetDifficulty(int value)
    {
        PlayerPrefs.SetInt("Difficulty", value);
    }

    public void SetTutorials(bool value)
    {
        PlayerPrefs.SetInt("ShowTutorials", value ? 1 : 0);
    }

    // Control Settings
    public void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
    }

    public void SetInvertMouse(bool value)
    {
        PlayerPrefs.SetInt("InvertMouse", value ? 1 : 0);
    }

    public void ResetKeybinds()
    {
        // TODO: Reset all keybinds to default
        Debug.Log("Keybinds reset to default");
    }

    // Preset Methods
    public void ApplyDefaultPreset()
    {
        SettingsManager.ApplyPreset(SettingsManager.GetDefaultPreset(), this);
    }

    public void ApplyPerformancePreset()
    {
        SettingsManager.ApplyPreset(SettingsManager.GetPerformancePreset(), this);
    }

    public void ApplyQualityPreset()
    {
        SettingsManager.ApplyPreset(SettingsManager.GetQualityPreset(), this);
    }

    public void ResetAllToDefaults()
    {
        SettingsManager.ResetToDefaults();
        ApplyDefaultPreset();
        ApplyChanges();
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
        bool changed = HasAnySettingChanged();
        
        if (applyButton != null) applyButton.interactable = changed;
        if (cancelButton != null) cancelButton.interactable = changed;
    }

    private bool HasAnySettingChanged()
    {
        // Audio changes
        bool audioChanged = 
            (volumeSlider != null && !Mathf.Approximately(volumeSlider.value, initialVolume)) ||
            (musicVolumeSlider != null && !Mathf.Approximately(musicVolumeSlider.value, initialMusicVolume)) ||
            (sfxVolumeSlider != null && !Mathf.Approximately(sfxVolumeSlider.value, initialSfxVolume)) ||
            (muteToggle != null && muteToggle.isOn != initialMute);

        // Display changes
        bool displayChanged = 
            (fullscreenToggle != null && fullscreenToggle.isOn != initialFullscreen) ||
            (resolutionDropdown != null && resolutionDropdown.value != initialResolution) ||
            (qualityDropdown != null && qualityDropdown.value != initialQuality) ||
            (vsyncToggle != null && vsyncToggle.isOn != initialVSync) ||
            (brightnessSlider != null && !Mathf.Approximately(brightnessSlider.value, initialBrightness));

        // Game changes
        bool gameChanged = 
            (autosaveToggle != null && autosaveToggle.isOn != initialAutosave) ||
            (difficultyDropdown != null && difficultyDropdown.value != initialDifficulty) ||
            (tutorialsToggle != null && tutorialsToggle.isOn != initialTutorials);

        // Control changes
        bool controlsChanged = 
            (mouseSensitivitySlider != null && !Mathf.Approximately(mouseSensitivitySlider.value, initialMouseSensitivity)) ||
            (invertMouseToggle != null && invertMouseToggle.isOn != initialInvertMouse);

        return audioChanged || displayChanged || gameChanged || controlsChanged;
    }

    private void ApplyChanges()
    {
        ApplyAudioSettings();
        ApplyDisplaySettings();
        ApplyGameSettings();
        ApplyControlSettings();
        
        StoreInitialValues(); // Update initial values to current values
        UpdateButtonStates();
        
        PlayerPrefs.Save(); // Save all preferences
        
        if (saveFeedbackText != null)
        {
            saveFeedbackText.text = "Settings Applied Successfully!";
            saveFeedbackText.color = Color.green;
            StartCoroutine(ClearFeedbackText(3f));
        }
    }

    private void ApplyAudioSettings()
    {
        if (volumeSlider != null) SetVolume(volumeSlider.value);
        if (musicVolumeSlider != null) SetMusicVolume(musicVolumeSlider.value);
        if (sfxVolumeSlider != null) SetSFXVolume(sfxVolumeSlider.value);
        if (muteToggle != null) SetMute(muteToggle.isOn);
    }

    private void ApplyDisplaySettings()
    {
        if (fullscreenToggle != null) SetFullscreen(fullscreenToggle.isOn);
        if (resolutionDropdown != null) SetResolution(resolutionDropdown.value);
        if (qualityDropdown != null) SetQuality(qualityDropdown.value);
        if (vsyncToggle != null) SetVSync(vsyncToggle.isOn);
        if (brightnessSlider != null) SetBrightness(brightnessSlider.value);
    }

    private void ApplyGameSettings()
    {
        if (autosaveToggle != null) SetAutosave(autosaveToggle.isOn);
        if (difficultyDropdown != null) SetDifficulty(difficultyDropdown.value);
        if (tutorialsToggle != null) SetTutorials(tutorialsToggle.isOn);
    }

    private void ApplyControlSettings()
    {
        if (mouseSensitivitySlider != null) SetMouseSensitivity(mouseSensitivitySlider.value);
        if (invertMouseToggle != null) SetInvertMouse(invertMouseToggle.isOn);
    }

    private void CancelChanges()
    {
        RevertAudioSettings();
        RevertDisplaySettings();
        RevertGameSettings();
        RevertControlSettings();
        
        UpdateButtonStates();
        
        if (saveFeedbackText != null)
        {
            saveFeedbackText.text = "Changes Cancelled";
            saveFeedbackText.color = Color.yellow;
            StartCoroutine(ClearFeedbackText(2f));
        }
    }

    private void RevertAudioSettings()
    {
        if (volumeSlider != null) volumeSlider.value = initialVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = initialMusicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = initialSfxVolume;
        if (muteToggle != null) muteToggle.isOn = initialMute;
    }

    private void RevertDisplaySettings()
    {
        if (fullscreenToggle != null) fullscreenToggle.isOn = initialFullscreen;
        if (resolutionDropdown != null) resolutionDropdown.value = initialResolution;
        if (qualityDropdown != null) qualityDropdown.value = initialQuality;
        if (vsyncToggle != null) vsyncToggle.isOn = initialVSync;
        if (brightnessSlider != null) brightnessSlider.value = initialBrightness;
    }

    private void RevertGameSettings()
    {
        if (autosaveToggle != null) autosaveToggle.isOn = initialAutosave;
        if (difficultyDropdown != null) difficultyDropdown.value = initialDifficulty;
        if (tutorialsToggle != null) tutorialsToggle.isOn = initialTutorials;
    }

    private void RevertControlSettings()
    {
        if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = initialMouseSensitivity;
        if (invertMouseToggle != null) invertMouseToggle.isOn = initialInvertMouse;
    }

    private void UpdateButtonStates()
    {
        if (applyButton != null) applyButton.interactable = false;
        if (cancelButton != null) cancelButton.interactable = false;
    }

    private System.Collections.IEnumerator ClearFeedbackText(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (saveFeedbackText != null)
        {
            saveFeedbackText.text = "";
        }
    }
    public void OnBackToMenuButtonClicked()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenuScene")
        {
            // Reset GameLoopManager state before going to menu to prevent game state persistence issues
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.ResetForNewGame();
            }
            
            // Also reset other persistent managers that might cause issues
            if (ShopUIManager.Instance != null)
            {
                ShopUIManager.Instance.ResetShopState();
            }
            
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

    private void StoreInitialValues()
    {
        // Audio
        initialVolume = AudioListener.volume;
        initialMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        initialSfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        initialMute = PlayerPrefs.GetInt("AudioMuted", 0) == 1;

        // Display
        initialFullscreen = Screen.fullScreen;
        initialResolution = GetCurrentResolutionIndex();
        initialQuality = QualitySettings.GetQualityLevel();
        initialVSync = QualitySettings.vSyncCount > 0;
        initialBrightness = PlayerPrefs.GetFloat("Brightness", 1f);

        // Game
        initialAutosave = PlayerPrefs.GetInt("AutoSave", 1) == 1;
        initialDifficulty = PlayerPrefs.GetInt("Difficulty", 1);
        initialTutorials = PlayerPrefs.GetInt("ShowTutorials", 1) == 1;

        // Controls
        initialMouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        initialInvertMouse = PlayerPrefs.GetInt("InvertMouse", 0) == 1;
    }

    private void SetupEventListeners()
    {
        // Audio listeners - add both the action and change tracking
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxVolumeSlider.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (muteToggle != null)
        {
            muteToggle.onValueChanged.AddListener(SetMute);
            muteToggle.onValueChanged.AddListener(_ => OnOptionChanged());
        }

        // Display listeners - add both the action and change tracking
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
            resolutionDropdown.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.AddListener(SetQuality);
            qualityDropdown.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.AddListener(SetVSync);
            vsyncToggle.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
            brightnessSlider.onValueChanged.AddListener(_ => OnOptionChanged());
        }

        // Game listeners - add both the action and change tracking
        if (autosaveToggle != null)
        {
            autosaveToggle.onValueChanged.AddListener(SetAutosave);
            autosaveToggle.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.AddListener(SetDifficulty);
            difficultyDropdown.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (tutorialsToggle != null)
        {
            tutorialsToggle.onValueChanged.AddListener(SetTutorials);
            tutorialsToggle.onValueChanged.AddListener(_ => OnOptionChanged());
        }

        // Control listeners - add both the action and change tracking
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
            mouseSensitivitySlider.onValueChanged.AddListener(_ => OnOptionChanged());
        }
        if (invertMouseToggle != null)
        {
            invertMouseToggle.onValueChanged.AddListener(SetInvertMouse);
            invertMouseToggle.onValueChanged.AddListener(_ => OnOptionChanged());
        }
    }

    private void SetupNavigationButtons()
    {
        if (applyButton != null) applyButton.onClick.AddListener(ApplyChanges);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelChanges);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClicked);

        if (backToMenuButton != null)
        {
            bool inMainMenu = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenuScene";
            backToMenuButton.interactable = !inMainMenu;
        }
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