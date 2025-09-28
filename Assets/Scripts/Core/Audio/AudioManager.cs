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

    [Header("Time Control Sounds")]
    [SerializeField] private AudioClip playSound;
    [SerializeField] private AudioClip pauseSound;
    [SerializeField] private AudioClip fastForwardSound;

    private AudioSource audioSource;

    [Header("Sound muting settings for paused")]
    [SerializeField] private AudioSource[] exceptions;
    private AudioSource[] allAudioSources;

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
            audioSource.PlayOneShot(buildingPlaceSound, volume);
        }
    }

    public void PlayMoneySpendSound()
    {
        if (moneySpendSound != null)
        {
            moneySpendSound.Play();
        }
    }

    public void PlayRemoveSound()
    {
        if (buildingRemoveSound != null)
        {
            audioSource.PlayOneShot(buildingRemoveSound, volume);
        }
    }

    public void PlayInsufficientFundsSound()
    {
        if (insufficientFundsSound != null)
        {
            audioSource.PlayOneShot(insufficientFundsSound, volume);
        }
    }
    public void PlayErrorSound()
    {
        if (errorSound != null)
        {
            errorSound.Play();
        }
    }

    public void PauseGameAudio()
    {
        allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in allAudioSources)
        {
            if (System.Array.IndexOf(exceptions, source) < 0)
            {
                source.mute = true;
            }
        }
    }

    public void ResumeGameAudio()
    {
        foreach (var source in allAudioSources)
        {
            if (System.Array.IndexOf(exceptions, source) < 0)
            {
                source.mute = false;
            }
        }
    }
}