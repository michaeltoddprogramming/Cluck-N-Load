using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Building Sounds")]
    [SerializeField] private AudioClip buildingPlaceSound;
    [SerializeField] private AudioClip buildingRemoveSound;
    [SerializeField] private AudioClip insufficientFundsSound;
    [SerializeField] private AudioSource moneySpendSound;
    [SerializeField] private AudioSource errorSound;
    [SerializeField] private float volume = 1.0f;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buildingSelectSound;

    [Header("Time Control Sounds")]
    [SerializeField] private AudioClip playSound;
    [SerializeField] private AudioClip pauseSound;
    [SerializeField] private AudioClip fastForwardSound;

    private AudioSource audioSource;

    [Header("One-shot SFX")]
    [SerializeField] private AudioClip harvestClip;
    [SerializeField] private AudioClip collectClip;

    [Header("Sound muting settings for paused")]
    [SerializeField] private AudioSource[] exceptions;
    private AudioSource[] allAudioSources;

    [Header("Repair SFX")]
    [SerializeField] private AudioSource repairSFX;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Add AudioSource if not present
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Apply saved SFX volume
            float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            if (audioSource != null)
            {
                audioSource.volume = savedSFXVolume * volume;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayPlaceSound()
    {
        if (buildingPlaceSound != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(buildingPlaceSound, volume * sfxVolume);
        }
    }

    public void PlayMoneySpendSound()
    {
        if (moneySpendSound != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            moneySpendSound.volume = sfxVolume;
            moneySpendSound.Play();
        }
    }

    public void PlayRemoveSound()
    {
        if (buildingRemoveSound != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(buildingRemoveSound, volume * sfxVolume);
        }
    }

    public void PlayInsufficientFundsSound()
    {
        if (insufficientFundsSound != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(insufficientFundsSound, volume * sfxVolume);
        }
    }
    public void PlaySelectSound()
    {
        if (buildingSelectSound != null && audioSource != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(buildingSelectSound, volume * sfxVolume);
        }
    }

    public void PlayErrorSound()
    {
        if (errorSound != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            errorSound.volume = sfxVolume;
            errorSound.Play();
        }
    }

    public void PauseGameAudio()
    {
        allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var source in allAudioSources)
        {
            if (System.Array.IndexOf(exceptions, source) < 0 && source != null)
            {
                source.mute = true;
            }
        }
    }

    public void ResumeGameAudio()
    {
        foreach (var source in allAudioSources)
        {
            if (System.Array.IndexOf(exceptions, source) < 0 && source != null)
            {
                source.mute = false;
            }
        }
    }

    public void PlayRepairSound()
    {
        if (repairSFX != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            repairSFX.volume = sfxVolume;
            repairSFX.Play();
        }
    }

    // Play a one-shot clip at given volume
    public void PlayOneShotClip(AudioClip clip, float vol = 1f)
    {
        if (clip != null && audioSource != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(clip, vol * sfxVolume);
        }
    }

    // Convenience wrappers so callers don't need to know which clip is assigned
    public void PlayHarvestClip()
    {
        if (harvestClip != null && audioSource != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(harvestClip, volume * sfxVolume);
            return;
        }
        // Fallback to place sound if no harvest clip assigned
        PlayPlaceSound();
    }

    public void PlayCollectClip()
    {
        if (collectClip != null && audioSource != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(collectClip, volume * sfxVolume);
            return;
        }
        // No explicit collect clip: use select sound if available
        PlayMoneySpendSound();
    }
}