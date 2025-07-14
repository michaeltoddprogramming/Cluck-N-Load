using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialSettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Toggle enableTutorialToggle;
    [SerializeField] private Toggle skipCompletedToggle;
    [SerializeField] private Toggle enableVoiceToggle;
    [SerializeField] private Toggle enableAnimationsToggle;
    [SerializeField] private Slider voiceVolumeSlider;
    [SerializeField] private Slider tutorialSpeedSlider;
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private Button closeButton;
    
    [Header("Display Elements")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI voiceVolumeText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private GameObject completionBadge;
    
    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmResetDialog;
    [SerializeField] private Button confirmResetButton;
    [SerializeField] private Button cancelResetButton;

    private TutorialProgressTracker progressTracker;
    private TutorialManager tutorialManager;
    
    private const string ENABLE_TUTORIAL_KEY = "Tutorial_Enabled";
    private const string SKIP_COMPLETED_KEY = "Tutorial_SkipCompleted";
    private const string ENABLE_VOICE_KEY = "Tutorial_EnableVoice";
    private const string ENABLE_ANIMATIONS_KEY = "Tutorial_EnableAnimations";
    private const string VOICE_VOLUME_KEY = "Tutorial_VoiceVolume";
    private const string TUTORIAL_SPEED_KEY = "Tutorial_Speed";

    private void Start()
    {
        progressTracker = FindFirstObjectByType<TutorialProgressTracker>();
        tutorialManager = TutorialManager.Instance;
        
        SetupUI();
        LoadSettings();
        UpdateProgressDisplay();
    }

    private void SetupUI()
    {
        // Setup toggle listeners
        if (enableTutorialToggle != null)
        {
            enableTutorialToggle.onValueChanged.AddListener(OnEnableTutorialChanged);
        }

        if (skipCompletedToggle != null)
        {
            skipCompletedToggle.onValueChanged.AddListener(OnSkipCompletedChanged);
        }

        if (enableVoiceToggle != null)
        {
            enableVoiceToggle.onValueChanged.AddListener(OnEnableVoiceChanged);
        }

        if (enableAnimationsToggle != null)
        {
            enableAnimationsToggle.onValueChanged.AddListener(OnEnableAnimationsChanged);
        }

        // Setup slider listeners
        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
        }

        if (tutorialSpeedSlider != null)
        {
            tutorialSpeedSlider.onValueChanged.AddListener(OnTutorialSpeedChanged);
        }

        // Setup button listeners
        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.AddListener(ShowResetConfirmation);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettings);
        }

        if (confirmResetButton != null)
        {
            confirmResetButton.onClick.AddListener(ConfirmResetProgress);
        }

        if (cancelResetButton != null)
        {
            cancelResetButton.onClick.AddListener(CancelResetProgress);
        }

        // Hide confirmation dialog initially
        if (confirmResetDialog != null)
        {
            confirmResetDialog.SetActive(false);
        }
    }

    private void LoadSettings()
    {
        // Load toggle settings
        if (enableTutorialToggle != null)
        {
            enableTutorialToggle.isOn = PlayerPrefs.GetInt(ENABLE_TUTORIAL_KEY, 1) == 1;
        }

        if (skipCompletedToggle != null)
        {
            skipCompletedToggle.isOn = PlayerPrefs.GetInt(SKIP_COMPLETED_KEY, 1) == 1;
        }

        if (enableVoiceToggle != null)
        {
            enableVoiceToggle.isOn = PlayerPrefs.GetInt(ENABLE_VOICE_KEY, 1) == 1;
        }

        if (enableAnimationsToggle != null)
        {
            enableAnimationsToggle.isOn = PlayerPrefs.GetInt(ENABLE_ANIMATIONS_KEY, 1) == 1;
        }

        // Load slider settings
        if (voiceVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat(VOICE_VOLUME_KEY, 0.7f);
            voiceVolumeSlider.value = volume;
            UpdateVolumeDisplay(volume);
        }

        if (tutorialSpeedSlider != null)
        {
            float speed = PlayerPrefs.GetFloat(TUTORIAL_SPEED_KEY, 1.0f);
            tutorialSpeedSlider.value = speed;
            UpdateSpeedDisplay(speed);
        }
    }

    private void UpdateProgressDisplay()
    {
        if (progressTracker == null) return;

        // Update progress text
        if (progressText != null)
        {
            if (progressTracker.IsTutorialCompleted())
            {
                float totalTime = progressTracker.GetTotalTutorialTime();
                int minutes = Mathf.FloorToInt(totalTime / 60f);
                int seconds = Mathf.FloorToInt(totalTime % 60f);
                
                progressText.text = $"Tutorial Completed!\nTotal Time: {minutes}m {seconds}s\nSteps: {progressTracker.GetCompletedStepsCount()}";
            }
            else
            {
                int completedSteps = progressTracker.GetCompletedStepsCount();
                progressText.text = $"Progress: {completedSteps} steps completed\nTutorial in progress...";
            }
        }

        // Show/hide completion badge
        if (completionBadge != null)
        {
            completionBadge.SetActive(progressTracker.IsTutorialCompleted());
        }
    }

    private void OnEnableTutorialChanged(bool enabled)
    {
        PlayerPrefs.SetInt(ENABLE_TUTORIAL_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (progressTracker != null)
        {
            progressTracker.SetTutorialEnabled(enabled);
        }
    }

    private void OnSkipCompletedChanged(bool skip)
    {
        PlayerPrefs.SetInt(SKIP_COMPLETED_KEY, skip ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnEnableVoiceChanged(bool enabled)
    {
        PlayerPrefs.SetInt(ENABLE_VOICE_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        // Apply voice setting to tutorial manager if available
        if (tutorialManager != null)
        {
            // tutorialManager.SetVoiceEnabled(enabled);
        }
    }

    private void OnEnableAnimationsChanged(bool enabled)
    {
        PlayerPrefs.SetInt(ENABLE_ANIMATIONS_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        // Apply animation setting to tutorial manager if available
        if (tutorialManager != null)
        {
            // tutorialManager.SetAnimationsEnabled(enabled);
        }
    }

    private void OnVoiceVolumeChanged(float volume)
    {
        PlayerPrefs.SetFloat(VOICE_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        UpdateVolumeDisplay(volume);

        // Apply volume to tutorial manager if available
        if (tutorialManager != null)
        {
            // tutorialManager.SetVoiceVolume(volume);
        }
    }

    private void OnTutorialSpeedChanged(float speed)
    {
        PlayerPrefs.SetFloat(TUTORIAL_SPEED_KEY, speed);
        PlayerPrefs.Save();

        UpdateSpeedDisplay(speed);

        // Apply speed to tutorial manager if available
        if (tutorialManager != null)
        {
            // tutorialManager.SetTutorialSpeed(speed);
        }
    }

    private void UpdateVolumeDisplay(float volume)
    {
        if (voiceVolumeText != null)
        {
            voiceVolumeText.text = $"Voice Volume: {Mathf.RoundToInt(volume * 100)}%";
        }
    }

    private void UpdateSpeedDisplay(float speed)
    {
        if (speedText != null)
        {
            speedText.text = $"Tutorial Speed: {speed:F1}x";
        }
    }

    private void ShowResetConfirmation()
    {
        if (confirmResetDialog != null)
        {
            confirmResetDialog.SetActive(true);
        }
    }

    private void CancelResetProgress()
    {
        if (confirmResetDialog != null)
        {
            confirmResetDialog.SetActive(false);
        }
    }

    private void ConfirmResetProgress()
    {
        if (progressTracker != null)
        {
            progressTracker.ResetProgress();
        }

        if (tutorialManager != null)
        {
            tutorialManager.ResetTutorial();
        }

        // Hide confirmation dialog
        if (confirmResetDialog != null)
        {
            confirmResetDialog.SetActive(false);
        }

        // Update display
        UpdateProgressDisplay();

        Debug.Log("Tutorial progress has been reset.");
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            LoadSettings();
            UpdateProgressDisplay();
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Static getters for other systems to use
    public static bool IsTutorialEnabled()
    {
        return PlayerPrefs.GetInt(ENABLE_TUTORIAL_KEY, 1) == 1;
    }

    public static bool ShouldSkipCompleted()
    {
        return PlayerPrefs.GetInt(SKIP_COMPLETED_KEY, 1) == 1;
    }

    public static bool IsVoiceEnabled()
    {
        return PlayerPrefs.GetInt(ENABLE_VOICE_KEY, 1) == 1;
    }

    public static bool AreAnimationsEnabled()
    {
        return PlayerPrefs.GetInt(ENABLE_ANIMATIONS_KEY, 1) == 1;
    }

    public static float GetVoiceVolume()
    {
        return PlayerPrefs.GetFloat(VOICE_VOLUME_KEY, 0.7f);
    }

    public static float GetTutorialSpeed()
    {
        return PlayerPrefs.GetFloat(TUTORIAL_SPEED_KEY, 1.0f);
    }
}
