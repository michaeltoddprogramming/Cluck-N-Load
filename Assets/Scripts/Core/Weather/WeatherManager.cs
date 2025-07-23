
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [SerializeField] private GameObject rainPrefab;
    [SerializeField] private GameObject snowPrefab;

    private GameObject rainInstance;
    private GameObject snowInstance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void SpawnRain()
    {
        ClearWeather();
        if (rainPrefab != null)
        {
            rainInstance = Instantiate(rainPrefab);
        }
    }

    public void SpawnSnow()
    {
        ClearWeather();
        if (snowPrefab != null)
        {
            snowInstance = Instantiate(snowPrefab);
        }
    }

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
    }
}
