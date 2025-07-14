using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to automatically configure tutorial system in Unity Editor.
/// Attach this to your TutorialSystem GameObject and click "Auto Setup" in inspector.
/// </summary>
[System.Serializable]
public class TutorialSetupConfiguration : MonoBehaviour
{
    [Header("Setup Options")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool createMissingPrefabs = true;
    [SerializeField] private bool setupUITags = true;
    
    [Header("Old Man Character Assets")]
    [SerializeField] private Sprite oldManPortrait;
    [SerializeField] private AudioClip[] voiceClips;
    
    [Header("UI Colors")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color dialogueBackgroundColor = new Color(0, 0, 0, 0.8f);
    
    private TutorialSystem tutorialSystem;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            AutoSetupTutorialSystem();
        }
    }
    
    [ContextMenu("Auto Setup Tutorial System")]
    public void AutoSetupTutorialSystem()
    {
        Debug.Log("Setting up tutorial system...");
        
        tutorialSystem = GetComponent<TutorialSystem>();
        if (tutorialSystem == null)
        {
            Debug.LogError("TutorialSystem component not found!");
            return;
        }
        
        // Step 1: Setup UI Tags
        if (setupUITags)
        {
            SetupUITags();
        }
        
        // Step 2: Create or find prefabs
        if (createMissingPrefabs)
        {
            CreateTutorialPrefabs();
        }
        
        // Step 3: Setup materials
        CreateTutorialMaterials();
        
        Debug.Log("Tutorial system setup complete!");
    }
    
    private void SetupUITags()
    {
        Debug.Log("Setting up UI tags for tutorial highlighting...");
        
        // Find and tag important UI elements
        var shopButton = GameObject.Find("ShopButton");
        if (shopButton != null)
        {
            shopButton.tag = "TutorialShopButton";
        }
        
        var inventoryPanel = GameObject.Find("InventoryPanel");
        if (inventoryPanel != null)
        {
            inventoryPanel.tag = "TutorialInventory";
        }
        
        var moneyDisplay = GameObject.Find("MoneyText");
        if (moneyDisplay != null)
        {
            moneyDisplay.tag = "TutorialMoney";
        }
        
        // Add more UI element tags as needed
        Debug.Log("UI tags setup complete.");
    }
    
    private void CreateTutorialPrefabs()
    {
        Debug.Log("Creating tutorial UI prefabs...");
        
        // Create Tutorial UI Prefab
        CreateTutorialDialogueUI();
        
        // Create World Pointer Prefab
        CreateWorldPointerPrefab();
        
        // Create UI Arrow Prefab
        CreateUIArrowPrefab();
    }
    
    private void CreateTutorialDialogueUI()
    {
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create tutorial UI
        GameObject tutorialUI = new GameObject("TutorialUI");
        tutorialUI.transform.SetParent(canvas.transform, false);
        
        // Add RectTransform
        RectTransform rect = tutorialUI.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0.3f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Add background
        Image background = tutorialUI.AddComponent<Image>();
        background.color = dialogueBackgroundColor;
        
        // Add CanvasGroup for fading
        tutorialUI.AddComponent<CanvasGroup>();
        
        // Add TutorialUIPrefab script
        var uiScript = tutorialUI.AddComponent<TutorialUIPrefab>();
        
        // Create character portrait
        CreateCharacterPortrait(tutorialUI);
        
        // Create dialogue text
        CreateDialogueText(tutorialUI);
        
        // Create buttons
        CreateTutorialButtons(tutorialUI);
        
        // Create progress bar
        CreateProgressBar(tutorialUI);
        
        // Configure the script
        uiScript.ConfigureForTutorial();
        
        Debug.Log("Tutorial dialogue UI created.");
    }
    
    private void CreateCharacterPortrait(GameObject parent)
    {
        GameObject portrait = new GameObject("CharacterPortrait");
        portrait.transform.SetParent(parent.transform, false);
        
        RectTransform rect = portrait.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(100, 100);
        rect.anchoredPosition = new Vector2(50, 0);
        
        Image image = portrait.AddComponent<Image>();
        if (oldManPortrait != null)
        {
            image.sprite = oldManPortrait;
        }
        image.preserveAspect = true;
    }
    
    private void CreateDialogueText(GameObject parent)
    {
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.3f);
        rect.anchorMax = new Vector2(0.85f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Welcome to the tutorial!";
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
    }
    
    private void CreateTutorialButtons(GameObject parent)
    {
        // Next Button
        GameObject nextBtn = CreateButton(parent, "NextButton", "Next", new Vector2(0.7f, 0.1f), new Vector2(80, 30));
        
        // Skip Button
        GameObject skipBtn = CreateButton(parent, "SkipButton", "Skip", new Vector2(0.85f, 0.1f), new Vector2(80, 30));
        
        // Skip All Button
        GameObject skipAllBtn = CreateButton(parent, "SkipAllButton", "Skip All", new Vector2(0.85f, 0.9f), new Vector2(80, 30));
    }
    
    private GameObject CreateButton(GameObject parent, string name, string text, Vector2 anchorPos, Vector2 size)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorPos;
        rect.anchorMax = anchorPos;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = Color.gray;
        
        Button button = buttonObj.AddComponent<Button>();
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        return buttonObj;
    }
    
    private void CreateProgressBar(GameObject parent)
    {
        GameObject progressObj = new GameObject("ProgressSlider");
        progressObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = progressObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.05f);
        rect.anchorMax = new Vector2(0.6f, 0.15f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Slider slider = progressObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(progressObj.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = Color.black;
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(progressObj.transform, false);
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        
        slider.fillRect = fill.GetComponent<RectTransform>();
        
        // Progress text
        GameObject progressText = new GameObject("ProgressText");
        progressText.transform.SetParent(parent.transform, false);
        
        RectTransform textRect = progressText.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.65f, 0.05f);
        textRect.anchorMax = new Vector2(0.85f, 0.15f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI progressTMP = progressText.AddComponent<TextMeshProUGUI>();
        progressTMP.text = "0/20";
        progressTMP.fontSize = 12;
        progressTMP.color = Color.white;
        progressTMP.alignment = TextAlignmentOptions.Center;
    }
    
    private void CreateWorldPointerPrefab()
    {
        GameObject pointer = new GameObject("WorldPointer");
        pointer.AddComponent<WorldPointerPrefab>();
        
        // Add visual components
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(pointer.transform, false);
        
        // Add pointer image (you can replace with a better sprite)
        Image image = visual.AddComponent<Image>();
        image.color = highlightColor;
        
        pointer.SetActive(false);
        Debug.Log("World pointer prefab created.");
    }
    
    private void CreateUIArrowPrefab()
    {
        GameObject arrow = new GameObject("UIArrow");
        arrow.AddComponent<UIArrowPrefab>();
        
        // Add arrow image
        Image image = arrow.AddComponent<Image>();
        image.color = highlightColor;
        
        arrow.SetActive(false);
        Debug.Log("UI arrow prefab created.");
    }
    
    private void CreateTutorialMaterials()
    {
        // Create highlight material
        Material highlightMat = new Material(Shader.Find("Standard"));
        highlightMat.color = highlightColor;
        highlightMat.SetFloat("_Metallic", 0f);
        highlightMat.SetFloat("_Glossiness", 0.5f);
        
        Debug.Log("Tutorial materials created.");
    }
}
