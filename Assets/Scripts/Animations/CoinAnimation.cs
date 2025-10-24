// Add this script to a GameObject in your hierarchy (like UIManager)
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

    private int amountOfMoney = 0;
    
    void Awake()
    {
        Instance = this;
        Debug.Log("CoinAnimation initialized");
        
        // Automatic reference finding if not assigned
        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();
            
        if (coinTargetTransform == null)
        {
            // Try to find the gold UI element by name or tag
            var goldTextObj = GameObject.Find("GoldText") ?? GameObject.Find("MoneyText");
            if (goldTextObj)
                coinTargetTransform = goldTextObj.transform;
        }
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Create a basic coin prefab if missing
        if (coinPrefab == null)
            CreateBasicCoinPrefab();
            
        Debug.Log($"Coin animation setup: Prefab={coinPrefab!=null}, Target={coinTargetTransform!=null}, Canvas={mainCanvas!=null}");
        
        // Store original UI scale to prevent accumulation
        if (coinTargetTransform != null)
            originalUIScale = coinTargetTransform.localScale;
    }
    
    public void PlayCoinAnimation(Vector3 worldPos, int amount)
    {
        amountOfMoney = amount;

        Debug.Log($"PlayCoinAnimation called: pos={worldPos}, amount={amount}");
        
        if (coinPrefab == null || mainCanvas == null || coinTargetTransform == null)
        {
            Debug.LogError($"Missing references: Prefab={coinPrefab!=null}, Target={coinTargetTransform!=null}, Canvas={mainCanvas!=null}");
            return;
        }
        
        // Convert world to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        
        // Play sound
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
        
        // Create coin
        GameObject coin = Instantiate(coinPrefab, startPos, Quaternion.identity, mainCanvas.transform);
        RectTransform rt = coin.GetComponent<RectTransform>();
        
        // Make sure it's properly positioned
        rt.anchoredPosition = startPos;
        
        // Make sure coin is visible and sized properly
        coin.SetActive(true);
        rt.localScale = Vector3.one * 0.5f;
        
        // Target position (UI element)
        Vector3 targetPos = coinTargetTransform.position;
        
        // Animate using direct manipulation since LeanTween might be missing
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
        
        // Ensure it reaches final position
        rt.position = targetPos;
        
        // Scale down and destroy
        float scaleTime = 0.2f;
        float scaleStart = Time.time;
        while (Time.time < scaleStart + scaleTime)
        {
            float t = (Time.time - scaleStart) / scaleTime;
            rt.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.zero, t);
            yield return null;
        }
        
        Destroy(coin);
        
        // Make UI element pulse (use static scale to prevent accumulation)
        StartCoroutine(PulseUI());

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.addMoneyAfterCoinAnimation(amountOfMoney);
        }
    }
    
    private System.Collections.IEnumerator PulseUI()
    {
        if (isPulsing || coinTargetTransform == null) yield break;
        
        isPulsing = true;
        Transform uiElement = coinTargetTransform;
        
        float pulseTime = 0.15f;
        float pulseStart = Time.time;
        
        // Pulse up
        while (Time.time < pulseStart + pulseTime)
        {
            float t = (Time.time - pulseStart) / pulseTime;
            uiElement.localScale = Vector3.Lerp(originalUIScale, originalUIScale * 1.15f, t);
            yield return null;
        }
        
        // Pulse down
        pulseStart = Time.time;
        while (Time.time < pulseStart + pulseTime)
        {
            float t = (Time.time - pulseStart) / pulseTime;
            uiElement.localScale = Vector3.Lerp(originalUIScale * 1.15f, originalUIScale, t);
            yield return null;
        }
        
        // Ensure it returns to exactly original scale
        uiElement.localScale = originalUIScale;
        isPulsing = false;
    }
    
    // Creates a basic coin if prefab is missing
    private void CreateBasicCoinPrefab()
    {
        GameObject tempCoin = new GameObject("CoinTemp");
        
        // Add image component
        Image img = tempCoin.AddComponent<Image>();
        
        // Try to find a coin sprite
        Sprite coinSprite = Resources.Load<Sprite>("Coin") ?? 
                            Resources.Load<Sprite>("UI/Coin") ?? 
                            Resources.Load<Sprite>("Icons/Coin");
        
        // If no sprite found, create a yellow circle
        if (coinSprite == null)
        {
            img.color = Color.yellow;
            // Make circular mask
            tempCoin.AddComponent<Mask>().showMaskGraphic = true;
        }
        else
        {
            img.sprite = coinSprite;
        }
        
        // Set size
        RectTransform rt = tempCoin.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40, 40);
        
        // Save as prefab in memory
        coinPrefab = tempCoin;
        tempCoin.SetActive(false);
        DontDestroyOnLoad(tempCoin);
        
        Debug.Log("Created basic coin prefab");
    }
}