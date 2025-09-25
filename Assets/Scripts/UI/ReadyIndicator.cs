using UnityEngine;
using UnityEngine.UI;

public class ReadyIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private Vector3 hoverOffset = new Vector3(0, 2f, 0);
    [SerializeField] private float bobAmount = 0.3f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;
    [SerializeField] private float pulseSpeed = 1.5f;
    
    [Header("Icon Types")]
    [SerializeField] private Sprite harvestIcon; // For crops
    [SerializeField] private Sprite collectIcon; // For animals
    [SerializeField] private Sprite defaultIcon;
    
    private GameObject currentIndicator;
    private Vector3 basePosition;
    private float timeOffset;
    private Camera mainCamera;
    private Canvas worldCanvas;
    
    public enum IndicatorType { Harvest, Collect, Default }
    
    void Start()
    {
        mainCamera = Camera.main;
        timeOffset = Random.Range(0f, 2f * Mathf.PI); // Random animation offset
        
        // Find or create world space canvas
        worldCanvas = FindObjectOfType<Canvas>();
        if (worldCanvas == null || worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            CreateWorldCanvas();
        }
        
        // Create default indicator prefab if none assigned
        if (indicatorPrefab == null)
        {
            CreateDefaultIndicatorPrefab();
        }
    }
    
    public void ShowIndicator(IndicatorType type = IndicatorType.Default)
    {
        if (currentIndicator != null) return; // Already showing
        
        // Create indicator
        currentIndicator = Instantiate(indicatorPrefab, worldCanvas.transform);
        
        // Set icon based on type
        Image iconImage = currentIndicator.GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            iconImage.sprite = type switch
            {
                IndicatorType.Harvest => harvestIcon ?? defaultIcon,
                IndicatorType.Collect => collectIcon ?? defaultIcon,
                _ => defaultIcon
            };
        }
        
        // Position above structure
        basePosition = transform.position + hoverOffset;
        currentIndicator.transform.position = basePosition;
        
        // Make sure it faces camera
        if (mainCamera != null)
        {
            currentIndicator.transform.LookAt(mainCamera.transform);
            currentIndicator.transform.Rotate(0, 180, 0); // Face camera properly
        }
    }
    
    public void HideIndicator()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
    }
    
    void Update()
    {
        if (currentIndicator == null) return;
        
        // Animate the indicator
        float time = Time.time + timeOffset;
        
        // Bobbing motion
        Vector3 bobOffset = Vector3.up * (Mathf.Sin(time * bobSpeed) * bobAmount);
        
        // Pulsing scale
        float pulseScale = 1f + (Mathf.Sin(time * pulseSpeed) * pulseAmount);
        
        // Apply animations
        currentIndicator.transform.position = basePosition + bobOffset;
        currentIndicator.transform.localScale = Vector3.one * pulseScale;
        
        // Always face camera
        if (mainCamera != null)
        {
            currentIndicator.transform.LookAt(mainCamera.transform);
            currentIndicator.transform.Rotate(0, 180, 0);
        }
    }
    
    private void CreateWorldCanvas()
    {
        GameObject canvasGO = new GameObject("WorldCanvas");
        worldCanvas = canvasGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = mainCamera;
        
        // Add CanvasScaler for consistent sizing
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        
        // Add GraphicRaycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        
        Debug.Log("Created world space canvas for indicators");
    }
    
    private void CreateDefaultIndicatorPrefab()
    {
        // Create a simple indicator with background and icon
        GameObject indicator = new GameObject("ReadyIndicator");
        
        // Add Canvas component for UI elements
        Canvas canvas = indicator.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        
        // Set canvas size
        RectTransform canvasRect = indicator.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 100);
        
        // Create background circle
        GameObject background = new GameObject("Background");
        background.transform.SetParent(indicator.transform);
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.9f); // Semi-transparent white
        bgImage.sprite = CreateCircleSprite(); // Create a circle sprite
        
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create icon
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(indicator.transform);
        
        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green color
        iconImage.sprite = CreateExclamationSprite(); // Create exclamation mark
        
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.2f);
        iconRect.anchorMax = new Vector2(0.8f, 0.8f);
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;
        
        // Save as prefab reference
        indicatorPrefab = indicator;
        indicator.SetActive(false);
        
        Debug.Log("Created default indicator prefab");
    }
    
    private Sprite CreateCircleSprite()
    {
        // Create a simple circle texture
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color color = distance <= radius ? Color.white : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private Sprite CreateExclamationSprite()
    {
        // Create a simple exclamation mark texture
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        // Clear texture
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }
        
        // Draw exclamation mark (simple rectangle and dot)
        int centerX = size / 2;
        int width = 6;
        
        // Main line
        for (int y = size / 4; y < size * 3 / 4; y++)
        {
            for (int x = centerX - width / 2; x <= centerX + width / 2; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                    texture.SetPixel(x, y, Color.white);
            }
        }
        
        // Dot at bottom
        for (int y = size / 8; y < size / 6; y++)
        {
            for (int x = centerX - width / 2; x <= centerX + width / 2; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                    texture.SetPixel(x, y, Color.white);
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    void OnDestroy()
    {
        HideIndicator();
    }
}