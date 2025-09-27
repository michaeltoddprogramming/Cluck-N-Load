using System.Collections;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [SerializeField] private GameObject rainPrefab;
    [SerializeField] private GameObject snowPrefab;
    [SerializeField] private AudioClip rainSFX;
    [SerializeField] private AudioSource rainAudioSource;

    private GameObject rainInstance;
    private GameObject snowInstance;
    private Coroutine rainFadeCoroutine;

    // In Awake(), set up the AudioSource
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure the assigned AudioSource is set up
            if (rainAudioSource != null)
            {
                rainAudioSource.clip = rainSFX;
                rainAudioSource.loop = true;
                rainAudioSource.volume = 0f;
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Fade helper
    private IEnumerator FadeRainAudio(float targetVolume, float duration)
    {
        float startVolume = rainAudioSource.volume;
        float time = 0f;
        while (time < duration)
        {
            rainAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        rainAudioSource.volume = targetVolume;
        if (targetVolume == 0f) rainAudioSource.Stop();
    }

    // SpawnRain: fade in SFX
    public void SpawnRain()
    {
        ClearWeather();
        if (rainPrefab != null)
        {
            rainInstance = Instantiate(rainPrefab);
        }
        // Ensure rainSFX is loaded before playing
        if (rainAudioSource != null && rainAudioSource.clip == null)
        {
            rainAudioSource.clip = rainSFX;
        }
        if (rainAudioSource != null && rainAudioSource.clip != null)
        {
            if (rainFadeCoroutine != null) StopCoroutine(rainFadeCoroutine);
            rainAudioSource.Play();
            rainFadeCoroutine = StartCoroutine(FadeRainAudio(1f, 2f)); // 2 seconds fade in
        }
    }

    // SpawnSnow: clear weather and instantiate snow prefab
    public void SpawnSnow()
    {
        ClearWeather();
        if (snowPrefab != null)
        {
            snowInstance = Instantiate(snowPrefab);
        }
    }

    // ClearWeather: fade out SFX
    public void ClearWeather()
    {
        if (rainInstance != null)
        {
            Destroy(rainInstance);
            rainInstance = null;
        }
        if (snowInstance != null)
        {
            Destroy(snowInstance);
            snowInstance = null;
        }
        if (rainAudioSource != null && rainAudioSource.isPlaying)
        {
            if (rainFadeCoroutine != null) StopCoroutine(rainFadeCoroutine);
            rainFadeCoroutine = StartCoroutine(FadeRainAudio(0f, 2f)); // 2 seconds fade out
        }
    }
}
