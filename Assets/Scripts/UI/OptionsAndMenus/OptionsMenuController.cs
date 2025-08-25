using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Required for List<T>

public class OptionsMenuController : MonoBehaviour
{
    public static OptionsMenuController Instance { get; private set; }

    [Header("Audio")]
    public Slider volumeSlider;

    [Header("Display")]
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;
    public Toggle vsyncToggle;

    private Resolution[] resolutions;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
        gameObject.SetActive(false);   // Hide by default
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
            List<string> options = new List<string>();
            int currentResIndex = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);
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

    // Add your options logic here (volume, graphics, etc.)
}