using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ReadyIndicator : MonoBehaviour
{
    [Header("Prefab Settings")]
    public static GameObject harvestIndicatorPrefab; // Prefab for crop harvesting
    public static GameObject collectIndicatorPrefab; // Prefab for animal collection
    
    [Header("Visual Settings")]
    [SerializeField] private Vector3 hoverOffset = new Vector3(0, 2f, 0);
    [SerializeField] private float bobAmount = 0.3f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;
    [SerializeField] private float pulseSpeed = 1.5f;
    
    [Header("Click Settings")]
    [SerializeField] private float clickScaleEffect = 1.2f;
    [SerializeField] private float clickEffectDuration = 0.1f;
    
    private GameObject currentIndicator;
    private Vector3 basePosition;
    private float timeOffset;
    private Camera mainCamera;
    private static Canvas worldCanvas;
    private IndicatorType currentType;
    private bool isClickable = true;
    
    public enum IndicatorType { Harvest, Collect, Default }
    
    void Start()
    {
        mainCamera = Camera.main;
        timeOffset = UnityEngine.Random.Range(0f, 2f * Mathf.PI); // Random animation offset
        
        // Find or create world space canvas (only once across all instances)
        if (worldCanvas == null)
        {
            worldCanvas = FindFirstObjectByType<Canvas>();
            if (worldCanvas == null || worldCanvas.renderMode != RenderMode.WorldSpace)
            {
                CreateWorldCanvas();
            }
        }
    }
    
    public void ShowIndicator(IndicatorType type = IndicatorType.Default)
    {
        if (currentIndicator != null) return; // Already showing
        
        currentType = type;
        
        // Choose the appropriate prefab
        GameObject prefabToUse = type switch
        {
            IndicatorType.Harvest => harvestIndicatorPrefab ?? CreateDefaultIndicator(Color.yellow, "Harvest"),
            IndicatorType.Collect => collectIndicatorPrefab ?? CreateDefaultIndicator(Color.green, "Collect"),
            _ => harvestIndicatorPrefab ?? collectIndicatorPrefab ?? CreateDefaultIndicator(Color.white, "Ready") // Default fallback
        };
        
        if (prefabToUse == null)
        {
            Debug.LogWarning($"[ReadyIndicator] No prefab assigned for {type} indicator type!");
            return;
        }
        
        // Create indicator from prefab
        currentIndicator = Instantiate(prefabToUse, worldCanvas.transform);
        
        // Add click functionality
        SetupClickable(currentIndicator, type);
        
        // Position above structure
        // Calculate height of the structure to position indicator above it
        float structureHeight = GetStructureHeight();
        
        // Add extra height for crop structures since they're typically low to the ground
        bool isCropStructure = GetComponent<CropStructure>() != null;
        float extraHeight = isCropStructure ? 2.0f : 0.5f; // Much higher for crops
        
        Vector3 dynamicHoverOffset = new Vector3(0, structureHeight + extraHeight, 0);
        basePosition = transform.position + dynamicHoverOffset;
        currentIndicator.transform.position = basePosition;
        
        // Make sure it faces camera
        if (mainCamera != null)
        {
            currentIndicator.transform.LookAt(mainCamera.transform);
            currentIndicator.transform.Rotate(0, 180, 0); // Face camera properly
        }
        
        Debug.Log($"[ReadyIndicator] Showing {type} indicator for {gameObject.name}");
    }
    
    public void HideIndicator()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
    }
    
    private float GetStructureHeight()
    {
        // Try to get height from collider first
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds.size.y;
        }
        
        // Fallback to renderer bounds
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        
        // Default height if nothing found
        return 1f;
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
        
        // Add GraphicRaycaster for click detection
        canvasGO.AddComponent<GraphicRaycaster>();

        // Scale down the canvas so indicators appear appropriately sized in world space
        canvasGO.transform.localScale = new Vector3(0.020f, 0.020f, 0.020f);
    }
    
    private void SetupClickable(GameObject indicator, IndicatorType type)
    {
        // Ensure the indicator has the necessary components for clicking
        
        // Add Button component to the main indicator or find existing one
        Button button = indicator.GetComponent<Button>();
        if (button == null)
        {
            button = indicator.AddComponent<Button>();
        }
        
        // Ensure there's an Image component for the button to work
        Image buttonImage = indicator.GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = indicator.AddComponent<Image>();
            buttonImage.color = Color.clear; // Invisible but clickable
        }
        
        // Add click listener
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnIndicatorClicked(type));
        
        // Add visual feedback
        var colors = button.colors;
        colors.pressedColor = new Color(1f, 1f, 1f, 0.5f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.3f);
        button.colors = colors;
        
        // Ensure the button can be clicked by making sure it has proper sizing
        RectTransform rectTransform = indicator.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = indicator.AddComponent<RectTransform>();
        }
        rectTransform.sizeDelta = new Vector2(80, 80); // Generous click area
        
        Debug.Log($"[ReadyIndicator] Set up clickable {type} indicator");
    }
    
    private void OnIndicatorClicked(IndicatorType type)
    {
        if (!isClickable) return;
        
        Debug.Log($"[ReadyIndicator] Clicked {type} indicator on {gameObject.name}");
        
        // Play click animation
        StartCoroutine(ClickAnimation());
        
        // Trigger appropriate action and only hide if successful
        bool actionSuccessful = false;
        switch (type)
        {
            case IndicatorType.Harvest:
                actionSuccessful = TriggerHarvest();
                break;
            case IndicatorType.Collect:
                actionSuccessful = TriggerCollect();
                break;
        }
        
        // Only hide indicator if the action was successful
        if (actionSuccessful)
        {
            HideIndicator();
        }
    }
    
    private bool TriggerHarvest()
    {
        // Just delegate to the structure's harvest method - it handles everything
        CropStructure cropStructure = GetComponent<CropStructure>();
        if (cropStructure != null && cropStructure.CropReady)
        {
            string result = cropStructure.Harvest();
            bool success = result == "yes";
            
            // Play harvest audio if successful (matching UI behavior)
            if (success && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHarvestClip();
            }
            
            return success;
        }
        return false;
    }
    
    private bool TriggerCollect()
    {
        // Just delegate to the structure's collect method - it handles everything  
        AnimalStructure animalStructure = GetComponent<AnimalStructure>();
        if (animalStructure != null && animalStructure.ProductReady)
        {
            animalStructure.Collect();
            
            // Play collect audio (matching UI behavior)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCollectClip();
            }
            
            return true; // Collect doesn't return status, assume success if we got here
        }
        return false;
    }
    
    private System.Collections.IEnumerator ClickAnimation()
    {
        if (currentIndicator == null) yield break;
        
        isClickable = false;
        
        // Scale up
        Vector3 originalScale = currentIndicator.transform.localScale;
        Vector3 targetScale = originalScale * clickScaleEffect;
        
        float elapsed = 0f;
        while (elapsed < clickEffectDuration / 2f && currentIndicator != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (clickEffectDuration / 2f);
            currentIndicator.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < clickEffectDuration / 2f && currentIndicator != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (clickEffectDuration / 2f);
            currentIndicator.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        if (currentIndicator != null)
        {
            currentIndicator.transform.localScale = originalScale;
        }
        isClickable = true;
    }
    
    private GameObject CreateDefaultIndicator(Color color, string label)
    {
        GameObject indicator = new GameObject($"Default{label}Indicator");
        
        // Add RectTransform
        RectTransform rect = indicator.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 60);
        
        // Add Image for icon
        Image img = indicator.AddComponent<Image>();
        img.color = color;
        
        // Simple circle shape (you can replace with actual sprites)
        
        return indicator;
    }
}