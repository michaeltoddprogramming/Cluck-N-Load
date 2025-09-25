using UnityEngine;

[System.Serializable]
public class SettingsPreset
{
    public string presetName;
    
    [Header("Audio Settings")]
    public float masterVolume = 1f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 1f;
    public bool muted = false;
    
    [Header("Display Settings")]
    public bool fullscreen = true;
    public int qualityLevel = 2; // Medium quality by default
    public bool vsync = true;
    public float brightness = 1f;
    
    [Header("Game Settings")]
    public bool autosave = true;
    public int difficulty = 1; // Normal difficulty
    public bool showTutorials = true;
    
    [Header("Control Settings")]
    public float mouseSensitivity = 1f;
    public bool invertMouse = false;
}

public static class SettingsManager
{
    public static SettingsPreset GetDefaultPreset()
    {
        return new SettingsPreset
        {
            presetName = "Default",
            masterVolume = 1f,
            musicVolume = 0.8f,
            sfxVolume = 1f,
            muted = false,
            fullscreen = true,
            qualityLevel = 2,
            vsync = true,
            brightness = 1f,
            autosave = true,
            difficulty = 1,
            showTutorials = true,
            mouseSensitivity = 1f,
            invertMouse = false
        };
    }
    
    public static SettingsPreset GetPerformancePreset()
    {
        return new SettingsPreset
        {
            presetName = "Performance",
            masterVolume = 1f,
            musicVolume = 0.6f,
            sfxVolume = 0.8f,
            muted = false,
            fullscreen = true,
            qualityLevel = 0, // Low quality for performance
            vsync = false,
            brightness = 1f,
            autosave = true,
            difficulty = 1,
            showTutorials = false,
            mouseSensitivity = 1.2f,
            invertMouse = false
        };
    }
    
    public static SettingsPreset GetQualityPreset()
    {
        return new SettingsPreset
        {
            presetName = "Quality",
            masterVolume = 1f,
            musicVolume = 0.9f,
            sfxVolume = 1f,
            muted = false,
            fullscreen = true,
            qualityLevel = QualitySettings.names.Length - 1, // Highest quality
            vsync = true,
            brightness = 1.1f,
            autosave = true,
            difficulty = 1,
            showTutorials = true,
            mouseSensitivity = 0.8f,
            invertMouse = false
        };
    }
    
    public static void ApplyPreset(SettingsPreset preset, OptionsMenuController optionsController)
    {
        // Audio
        if (optionsController.volumeSlider != null)
            optionsController.volumeSlider.value = preset.masterVolume;
        if (optionsController.musicVolumeSlider != null)
            optionsController.musicVolumeSlider.value = preset.musicVolume;
        if (optionsController.sfxVolumeSlider != null)
            optionsController.sfxVolumeSlider.value = preset.sfxVolume;
        if (optionsController.muteToggle != null)
            optionsController.muteToggle.isOn = preset.muted;
            
        // Display
        if (optionsController.fullscreenToggle != null)
            optionsController.fullscreenToggle.isOn = preset.fullscreen;
        if (optionsController.qualityDropdown != null)
            optionsController.qualityDropdown.value = preset.qualityLevel;
        if (optionsController.vsyncToggle != null)
            optionsController.vsyncToggle.isOn = preset.vsync;
        if (optionsController.brightnessSlider != null)
            optionsController.brightnessSlider.value = preset.brightness;
            
        // Game
        if (optionsController.autosaveToggle != null)
            optionsController.autosaveToggle.isOn = preset.autosave;
        if (optionsController.difficultyDropdown != null)
            optionsController.difficultyDropdown.value = preset.difficulty;
        if (optionsController.tutorialsToggle != null)
            optionsController.tutorialsToggle.isOn = preset.showTutorials;
            
        // Controls
        if (optionsController.mouseSensitivitySlider != null)
            optionsController.mouseSensitivitySlider.value = preset.mouseSensitivity;
        if (optionsController.invertMouseToggle != null)
            optionsController.invertMouseToggle.isOn = preset.invertMouse;
    }
    
    public static void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey("MusicVolume");
        PlayerPrefs.DeleteKey("SFXVolume");
        PlayerPrefs.DeleteKey("AudioMuted");
        PlayerPrefs.DeleteKey("Brightness");
        PlayerPrefs.DeleteKey("AutoSave");
        PlayerPrefs.DeleteKey("Difficulty");
        PlayerPrefs.DeleteKey("ShowTutorials");
        PlayerPrefs.DeleteKey("MouseSensitivity");
        PlayerPrefs.DeleteKey("InvertMouse");
        PlayerPrefs.Save();
    }
}