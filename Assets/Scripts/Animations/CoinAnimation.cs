using UnityEngine;
using UnityEngine.UI;

public class CoinAnimation : MonoBehaviour
{
    public static CoinAnimation Instance { get; private set; }

    [Header("Required References")]
    [SerializeField] private GameObject coinPrefab; // MUST BE ASSIGNED
    [SerializeField] private Transform coinTargetTransform; // The money UI element
    [SerializeField] private Canvas mainCanvas; // Canvas where coins will be shown
    
    [Header("Animation Settings")]
    [SerializeField] private int coinsToSpawn = 3;
    [SerializeField] private float animDuration = 1.0f;
    [SerializeField] private AudioClip coinSound;
    
    private AudioSource audioSource;
    private Vector3 originalUIScale = Vector3.one; // Store original scale to prevent accumulation
    private bool isPulsing = false; // Track if UI is currently pulsing
    
    private Camera mainCamera;
    private Sprite cachedCoinSprite;
    
    void Awake()
    {
        Instance = this;
        
        mainCamera = Camera.main;
        
        cachedCoinSprite = Resources.Load<Sprite>("Coin") ?? 
                          Resources.Load<Sprite>("UI/Coin") ?? 
                          Resources.Load<Sprite>("Icons/Coin");
        
        if (mainCanvas == null)
            mainCanvas = FindFirstObjectByType<Canvas>();
            
        if (coinTargetTransform == null)
        {
            var goldTextObj = GameObject.Find("GoldText") ?? GameObject.Find("MoneyText");
            if (goldTextObj)
                coinTargetTransform = goldTextObj.transform;
        }
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        if (coinPrefab == null)
            CreateBasicCoinPrefab();
            
        if (coinTargetTransform != null)
            originalUIScale = coinTargetTransform.localScale;
    }
    
    public void PlayCoinAnimation(Vector3 worldPos, int amount)
    {
        if (coinPrefab == null || mainCanvas == null || coinTargetTransform == null || mainCamera == null)
        {
            return;
        }
        
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        if (coinSound && audioSource)
            audioSource.PlayOneShot(coinSound);
        
        // Spawn coins
        int numCoins = Mathf.Min(coinsToSpawn, Mathf.Max(1, amount / 20));
        for (int i = 0; i < numCoins; i++)
        {
            StartCoroutine(AnimateCoin(screenPos, i * 0.1f));
        }
    }
    
    private System.Collections.IEnumerator AnimateCoin(Vector3 startPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        GameObject coin = Instantiate(coinPrefab, startPos, Quaternion.identity, mainCanvas.transform);
        RectTransform rt = coin.GetComponent<RectTransform>();
        
        rt.anchoredPosition = startPos;
        
        coin.SetActive(true);
        rt.localScale = Vector3.one * 0.5f;
        
        Vector3 targetPos = coinTargetTransform.position;
        
        float startTime = Time.time;
        Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
        midPoint.y += Random.Range(50f, 150f);
        
        while (Time.time < startTime + animDuration)
        {
            float normalizedTime = (Time.time - startTime) / animDuration;
            
            // Arc path
            if (normalizedTime < 0.5f)
            {
                rt.position = Vector3.Lerp(startPos, midPoint, normalizedTime * 2);
            }
            else
            {
                rt.position = Vector3.Lerp(midPoint, targetPos, (normalizedTime - 0.5f) * 2);
            }
            
            yield return null;
        }
        
        rt.position = targetPos;
        
        float scaleTime = 0.2f;
        float scaleStart = Time.time;
        while (Time.time < scaleStart + scaleTime)
        {
            float t = (Time.time - scaleStart) / scaleTime;
            rt.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.zero, t);
            yield return null;
        }
        
        Destroy(coin);
        
        StartCoroutine(PulseUI());
    }
    
    private System.Collections.IEnumerator PulseUI()
    {
        if (isPulsing || coinTargetTransform == null) yield break;
        
        isPulsing = true;
        Transform uiElement = coinTargetTransform;
        
        float pulseTime = 0.15f;
        float pulseStart = Time.time;
        
        while (Time.time < pulseStart + pulseTime)
        {
            float t = (Time.time - pulseStart) / pulseTime;
            uiElement.localScale = Vector3.Lerp(originalUIScale, originalUIScale * 1.15f, t);
            yield return null;
        }
        
        pulseStart = Time.time;
        while (Time.time < pulseStart + pulseTime)
        {
            float t = (Time.time - pulseStart) / pulseTime;
            uiElement.localScale = Vector3.Lerp(originalUIScale * 1.15f, originalUIScale, t);
            yield return null;
        }
        
        uiElement.localScale = originalUIScale;
        isPulsing = false;
    }
    
   private void CreateBasicCoinPrefab()
    {
        GameObject tempCoin = new GameObject("CoinTemp");
        
        Image img = tempCoin.AddComponent<Image>();
        
        Sprite coinSprite = cachedCoinSprite;
        
        if (coinSprite == null)
        {
            img.color = Color.yellow;
            tempCoin.AddComponent<Mask>().showMaskGraphic = true;
        }
        else
        {
            img.sprite = coinSprite;
        }
        
        RectTransform rt = tempCoin.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40, 40);
        
        coinPrefab = tempCoin;
        tempCoin.SetActive(false);
        DontDestroyOnLoad(tempCoin);
    }
}