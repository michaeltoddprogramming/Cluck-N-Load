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
    [SerializeField] private float bobAmount = 0.1f; // Reduced from 0.3f
    [SerializeField] private float bobSpeed = 1f; // Reduced from 2f
    [SerializeField] private float pulseAmount = 0.05f; // Reduced from 0.2f
    [SerializeField] private float pulseSpeed = 1f; // Reduced from 1.5f
    
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
        ShowIndicator(type, null);
    }
    
    public void ShowIndicator(IndicatorType type, int? amount)
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
        
        // Store the relative offset from the structure center
        float structureHeight = GetStructureHeight();
        bool isCropStructure = GetComponent<CropStructure>() != null;
        float extraHeight = isCropStructure ? 2.0f : 0.5f; // Much higher for crops
        
        // Store the offset that will be applied to the structure's position
        hoverOffset = new Vector3(0, structureHeight + extraHeight, 0);
        
        // Set initial position
        UpdateIndicatorPosition();
        
        // Make sure it faces camera
        if (mainCamera != null)
        {
            currentIndicator.transform.LookAt(mainCamera.transform);
            currentIndicator.transform.Rotate(0, 180, 0); // Face camera properly
        }
        
        Debug.Log($"[ReadyIndicator] Showing {type} indicator for {gameObject.name}");
    }
    
    void OnDestroy()
    {
        // Clean up indicator when structure is destroyed
        HideIndicator();
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
        
        // Update indicator position to follow the structure
        UpdateIndicatorPosition();
        
        // Animate the indicator
        float time = Time.time + timeOffset;
        
        // Bobbing motion
        Vector3 bobOffset = Vector3.up * (Mathf.Sin(time * bobSpeed) * bobAmount);
        
        // Pulsing scale
        float pulseScale = 1f + (Mathf.Sin(time * pulseSpeed) * pulseAmount);
        
        // Apply animations relative to base position
        currentIndicator.transform.position = basePosition + bobOffset;
        currentIndicator.transform.localScale = Vector3.one * pulseScale;
        
        // Always face camera
        if (mainCamera != null)
        {
            currentIndicator.transform.LookAt(mainCamera.transform);
            currentIndicator.transform.Rotate(0, 180, 0);
        }
    }
    
    private void UpdateIndicatorPosition()
    {
        if (currentIndicator == null) return;
        
        // Calculate the base position based on current structure position
        basePosition = transform.position + hoverOffset;
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
    
    public static void ShowFloatingNumber(Vector3 worldPosition, int amount, Color textColor)
    {
        ShowFloatingNumberWithBonus(worldPosition, amount, 0, textColor);
    }
    
    public static void ShowFloatingNumberWithBonus(Vector3 worldPosition, int baseAmount, int bonusAmount, Color textColor)
    {
        Debug.Log($"[ReadyIndicator] ShowFloatingNumberWithBonus: baseAmount={baseAmount}, bonusAmount={bonusAmount}");
        
        // Show main amount
        CreateFloatingText(worldPosition, baseAmount.ToString(), textColor, 0f);
        
        // Show bonus amount if there is one
        if (bonusAmount > 0)
        {
            Debug.Log($"[ReadyIndicator] Creating synergy bonus text: +{bonusAmount} SYNERGY");
            CreateFloatingText(worldPosition, $"+{bonusAmount} SYNERGY", Color.cyan, 1.5f); // Increased from 0.7f to 1.5f for much better spacing
        }
        else
        {
            Debug.Log($"[ReadyIndicator] No bonus to show (bonusAmount={bonusAmount})");
        }
    }
    
    public static void ShowFloatingNumberWithMultipleBonuses(Vector3 worldPosition, int baseAmount, int productionBonus, int synergyBonus, int seasonalBonus, Color textColor)
    {
        Debug.Log($"[ReadyIndicator] ShowFloatingNumberWithMultipleBonuses: baseAmount={baseAmount}, productionBonus={productionBonus}, synergyBonus={synergyBonus}, seasonalBonus={seasonalBonus}");
        
        // Show main amount
        CreateFloatingText(worldPosition, baseAmount.ToString(), textColor, 0f);
        
        float verticalOffset = 2f; // Increased initial spacing from 0.7f to 1.5f
        
        // Show production bonus if there is one
        if (productionBonus > 0)
        {
            Debug.Log($"[ReadyIndicator] Creating production bonus text: +{productionBonus} BONUS");
            CreateFloatingText(worldPosition, $"+{productionBonus} BONUS", Color.yellow, verticalOffset);
            verticalOffset += 2f; // Increased spacing between bonuses from 0.6f to 1.2f
        }
        
        // Show synergy bonus if there is one
        if (synergyBonus > 0)
        {
            Debug.Log($"[ReadyIndicator] Creating synergy bonus text: +{synergyBonus} SYNERGY");
            CreateFloatingText(worldPosition, $"+{synergyBonus} SYNERGY", Color.cyan, verticalOffset);
            verticalOffset += 2f; // Increased spacing between bonuses from 0.6f to 1.2f
        }
        
        // Show seasonal bonus if there is one
        if (seasonalBonus > 0)
        {
            Debug.Log($"[ReadyIndicator] Creating seasonal bonus text: +{seasonalBonus} SEASONAL");
            CreateFloatingText(worldPosition, $"+{seasonalBonus} SEASONAL", Color.green, verticalOffset);
        }
        
        if (productionBonus == 0 && synergyBonus == 0 && seasonalBonus == 0)
        {
            Debug.Log($"[ReadyIndicator] No bonuses to show");
        }
    }
    
    public static void CreateFloatingText(Vector3 worldPosition, string text, Color textColor, float verticalOffset)
    {
        // Find or create world space canvas
        Canvas canvas = worldCanvas;
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            {
                // Create temporary canvas for this floating text
                GameObject canvasObj = new GameObject("FloatingTextCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.scaleFactor = 0.01f;
                canvas.sortingOrder = 100;
            }
        }
        
        // Create floating text object
        GameObject floatingTextObj = new GameObject("FloatingNumber");
        floatingTextObj.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform
        RectTransform textRect = floatingTextObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(150, 50); // Wider for bonus text
        
        // Position in world space (slightly above the structure, with offset for bonus)
        Vector3 basePosition = worldPosition + Vector3.up * 3f;
        Vector3 offsetPosition = basePosition + Vector3.down * verticalOffset; // Changed back to Vector3.down so bonuses appear below base amount
        floatingTextObj.transform.position = offsetPosition;
        floatingTextObj.transform.LookAt(Camera.main.transform);
        floatingTextObj.transform.Rotate(0, 180, 0);
        
        // Add Text component
        var textComponent = floatingTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        
        // Format text based on whether it's bonus or not
        if (text.Contains("BONUS") || text.Contains("SYNERGY") || text.Contains("SEASONAL"))
        {
            textComponent.text = text;
            textComponent.fontSize = 18; // Smaller for bonus
            textComponent.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
        }
        else
        {
            textComponent.text = "+" + text;
            textComponent.fontSize = 24; // Normal size for main amount
            textComponent.fontStyle = TMPro.FontStyles.Bold;
        }
        
        textComponent.color = textColor;
        textComponent.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Add background for better readability
        GameObject bgObj = new GameObject("FloatingNumberBG");
        bgObj.transform.SetParent(floatingTextObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(text.Contains("BONUS") || text.Contains("SYNERGY") || text.Contains("SEASONAL") ? 140 : 80, 40);
        bgRect.anchoredPosition = Vector2.zero;
        
        var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);
        
        // Animate the floating text with slight delay for bonus
        float delay = (text.Contains("BONUS") || text.Contains("SYNERGY") || text.Contains("SEASONAL")) ? 0.1f + (verticalOffset * 0.3f) : 0f;
        StartFloatingAnimationWithDelay(floatingTextObj, delay);
    }
    
    private static void StartFloatingAnimationWithDelay(GameObject floatingText, float delay)
    {
        if (delay > 0f)
        {
            // For bonus text, start animation after a small delay
            FloatingTextAnimator animator = floatingText.AddComponent<FloatingTextAnimator>();
            Vector3 startPos = floatingText.transform.position;
            Vector3 endPos = startPos + Vector3.up * 2f;
            animator.InitializeWithDelay(startPos, endPos, 2f, delay);
        }
        else
        {
            // For main text, start immediately
            StartFloatingAnimation(floatingText);
        }
    }
    
    private static void StartFloatingAnimation(GameObject floatingText)
    {
        // Simple animation: move up and fade out over 2 seconds
        Vector3 startPos = floatingText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        
        // Create a component to handle the animation
        FloatingTextAnimator animator = floatingText.AddComponent<FloatingTextAnimator>();
        animator.Initialize(startPos, endPos, 2f);
    }
    
    private GameObject CreateDefaultIndicator(Color color, string label)
    {
        GameObject indicator = new GameObject($"Default{label}Indicator");
        
        // Add RectTransform
        RectTransform rect = indicator.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 60);
        
        // Create background element
        GameObject background = new GameObject("Background");
        background.transform.SetParent(indicator.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(70, 70); // Slightly larger than main icon
        bgRect.anchoredPosition = Vector2.zero;
        
        // Add background image with semi-transparent dark color
        Image bgImg = background.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f); // Dark semi-transparent background
        
        // Create main icon element
        GameObject icon = new GameObject("Icon");
        icon.transform.SetParent(indicator.transform, false);
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(60, 60);
        iconRect.anchoredPosition = Vector2.zero;
        
        // Add Image for main icon
        Image iconImg = icon.AddComponent<Image>();
        iconImg.color = color;
        
        // Simple circle shape (you can replace with actual sprites)
        
        return indicator;
    }
}