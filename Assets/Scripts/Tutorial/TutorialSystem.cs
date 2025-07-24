using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Main integration script for the tutorial system.
/// Place this on a GameObject in your main scene to set up the entire tutorial system.
/// </summary>
public class TutorialSystem : MonoBehaviour
{
    [Header("Tutorial Components")]
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private TutorialConditionTracker conditionTracker;
    
    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialCanvasPrefab;
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateComponents = true;
    [SerializeField] private bool startTutorialOnGameStart = true;
    [SerializeField] private float startDelay = 1f;
    
    [Header("Events")]
    public UnityEvent OnTutorialStarted;
    public UnityEvent OnTutorialCompleted;
    public UnityEvent OnTutorialSkipped;

    public static TutorialSystem Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-create components if needed
        if (autoCreateComponents)
        {
            SetupTutorialComponents();
        }
    }

    private void Start()
    {
        // Initialize tutorial system
        InitializeTutorialSystem();

        // Start tutorial if enabled
        if (startTutorialOnGameStart && ShouldStartTutorial())
        {
            Invoke(nameof(StartTutorial), startDelay);
        }
    }

    private void SetupTutorialComponents()
    {
        // Setup Tutorial Manager
        if (tutorialManager == null)
        {
            tutorialManager = FindFirstObjectByType<TutorialManager>();
            if (tutorialManager == null)
            {
                tutorialManager = gameObject.AddComponent<TutorialManager>();
            }
        }

        // Setup Condition Tracker
        if (conditionTracker == null)
        {
            conditionTracker = FindFirstObjectByType<TutorialConditionTracker>();
            if (conditionTracker == null)
            {
                conditionTracker = gameObject.AddComponent<TutorialConditionTracker>();
            }
        }

        Debug.Log("Tutorial components setup complete!");
    }

    private void InitializeTutorialSystem()
    {
        // First, create the tutorial UI if it doesn't exist
        CreateTutorialUIIfNeeded();
        
        // Configure tutorial manager
        if (tutorialManager != null)
        {
            // Connect auto-created UI to tutorial manager
            ConnectUIToTutorialManager();
        }

        // Subscribe to events
        SubscribeToEvents();
    }

    private void ConnectUIToTutorialManager()
    {
        // Try multiple ways to find the tutorial UI
        GameObject tutorialUI = null;
        
        // First, try to find TutorialDialoguePanel (your prefab structure)
        tutorialUI = GameObject.Find("TutorialDialoguePanel");
        
        if (tutorialUI == null)
        {
            // Try finding TutorialUI (auto-generated)
            tutorialUI = GameObject.Find("TutorialUI");
        }
        
        if (tutorialUI == null)
        {
            // Try finding it under TutorialCanvas
            GameObject tutorialCanvas = GameObject.Find("TutorialCanvas");
            if (tutorialCanvas == null)
            {
                tutorialCanvas = GameObject.Find("TutorialCanvas(Clone)");
            }
            
            if (tutorialCanvas != null)
            {
                // Look for TutorialDialoguePanel under the canvas
                Transform dialoguePanel = tutorialCanvas.transform.Find("TutorialDialoguePanel");
                if (dialoguePanel != null)
                {
                    tutorialUI = dialoguePanel.gameObject;
                }
            }
        }
        
        if (tutorialUI == null)
        {
            Debug.LogWarning("Could not find tutorial UI! Checked TutorialDialoguePanel, TutorialUI, and TutorialCanvas structures.");
            return;
        }

        Debug.Log($"Found tutorial UI: {tutorialUI.name}");
        
        // Make sure it's visible
        tutorialUI.SetActive(true);
        
        // Fix the alpha issue
        var canvasGroup = tutorialUI.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log("Fixed CanvasGroup alpha to 1");
        }

        // Get UI components and connect them to TutorialManager
        var uiScript = tutorialUI.GetComponent<TutorialUIPrefab>();
        if (uiScript != null && tutorialManager != null)
        {
            // Use reflection to connect UI elements
            var tutorialManagerType = typeof(TutorialManager);
            
            // Set tutorial panel
            var tutorialPanelField = tutorialManagerType.GetField("tutorialPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tutorialPanelField != null)
            {
                tutorialPanelField.SetValue(tutorialManager, tutorialUI);
                Debug.Log("Connected tutorialPanel");
            }

            // Set dialogue text
            var tutorialDescriptionField = tutorialManagerType.GetField("tutorialDescription", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tutorialDescriptionField != null && uiScript.dialogueText != null)
            {
                tutorialDescriptionField.SetValue(tutorialManager, uiScript.dialogueText);
                Debug.Log("Connected dialogueText");
            }

            // Set title text (character name)
            var tutorialTitleField = tutorialManagerType.GetField("tutorialTitle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tutorialTitleField != null && uiScript.characterNameText != null)
            {
                tutorialTitleField.SetValue(tutorialManager, uiScript.characterNameText);
                Debug.Log("Connected characterNameText");
            }

            // Set next button
            var nextButtonField = tutorialManagerType.GetField("nextButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nextButtonField != null && uiScript.nextButton != null)
            {
                nextButtonField.SetValue(tutorialManager, uiScript.nextButton);
                Debug.Log("Connected nextButton");
            }

            // Set skip button
            var skipButtonField = tutorialManagerType.GetField("skipButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (skipButtonField != null && uiScript.skipAllButton != null)
            {
                skipButtonField.SetValue(tutorialManager, uiScript.skipAllButton);
                Debug.Log("Connected skipButton");
            }

            // Set character portrait
            var characterPortraitField = tutorialManagerType.GetField("characterPortrait", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (characterPortraitField != null && uiScript.characterPortrait != null)
            {
                characterPortraitField.SetValue(tutorialManager, uiScript.characterPortrait);
                Debug.Log("Connected characterPortrait");
            }

            Debug.Log("UI components connected to TutorialManager!");
            
            // Force the UI to be visible after connection
            tutorialManager.ForceShowTutorialUI();
        }
        else
        {
            Debug.LogWarning("Could not find TutorialUIPrefab component on tutorial UI, or TutorialManager is null!");
        }
    }

    private void SubscribeToEvents()
    {
        if (tutorialManager != null)
        {
            tutorialManager.OnTutorialCompleted += HandleTutorialCompleted;
        }
    }

    private bool ShouldStartTutorial()
    {
        // Check if tutorial should be skipped because it's already completed
        if (HasCompletedTutorial())
        {
            return false;
        }

        return true;
    }

    private bool HasCompletedTutorial()
    {
        // Simple check - could be expanded to use PlayerPrefs or save file
        return false;
    }

    public void StartTutorial()
    {
        if (tutorialManager != null)
        {
            Debug.Log("Starting tutorial system...");
            OnTutorialStarted?.Invoke();
            
            // The tutorial manager will handle the rest
            tutorialManager.OnConditionMet(TutorialCondition.GameStarted);
        }
        else
        {
            Debug.LogError("TutorialManager not found! Cannot start tutorial.");
        }
    }

    public void StopTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.SkipTutorial();
        }
    }

    public void RestartTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.ResetTutorial();
        }

        // Start tutorial again after a short delay
        Invoke(nameof(StartTutorial), 0.5f);
    }

    public void PauseTutorial()
    {
        // Implementation depends on how you want to handle pausing
        // Could pause time, disable input, etc.
        Time.timeScale = 0f;
    }

    public void ResumeTutorial()
    {
        Time.timeScale = 1f;
    }

    private void HandleTutorialCompleted()
    {
        Debug.Log("Tutorial completed!");
        OnTutorialCompleted?.Invoke();
    }

    public void SkipCurrentStep()
    {
        if (tutorialManager != null)
        {
            tutorialManager.NextTutorialStep();
        }
    }

    public void SkipEntireTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.SkipTutorial();
            OnTutorialSkipped?.Invoke();
            // Destroy this system as well so nothing remains
            Destroy(gameObject);
        }
    }

    // Public API for external systems
    public bool IsTutorialActive()
    {
        return tutorialManager != null && tutorialManager.IsTutorialActive();
    }

    public bool IsTutorialCompleted()
    {
        return tutorialManager != null && tutorialManager.IsTutorialCompleted();
    }

    public void TriggerCondition(TutorialCondition condition)
    {
        if (tutorialManager != null)
        {
            tutorialManager.OnConditionMet(condition);
        }
    }

    // Debug methods for testing
    [ContextMenu("Start Tutorial (Debug)")]
    public void Debug_StartTutorial()
    {
        StartTutorial();
    }

    [ContextMenu("Complete Tutorial (Debug)")]
    public void Debug_CompleteTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.SkipTutorial();
        }
    }

    [ContextMenu("Reset Tutorial (Debug)")]
    public void Debug_ResetTutorial()
    {
        RestartTutorial();
    }

    [ContextMenu("Trigger Shop Opened (Debug)")]
    public void Debug_TriggerShopOpened()
    {
        TriggerCondition(TutorialCondition.ShopOpened);
    }

    [ContextMenu("Trigger First Structure Placed (Debug)")]
    public void Debug_TriggerFirstStructure()
    {
        TriggerCondition(TutorialCondition.FirstStructurePlaced);
    }

    [ContextMenu("Force Show Tutorial UI (Debug)")]
    public void Debug_ForceShowTutorialUI()
    {
        // Find and force show the tutorial UI
        GameObject tutorialUI = GameObject.Find("TutorialUI");
        if (tutorialUI != null)
        {
            tutorialUI.SetActive(true);
            
            var canvasGroup = tutorialUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            Debug.Log("Forced tutorial UI to show!");
        }
        else
        {
            Debug.LogError("TutorialUI GameObject not found!");
        }
    }

    [ContextMenu("Debug Tutorial UI Components")]
    public void Debug_TutorialUIComponents()
    {
        GameObject tutorialUI = GameObject.Find("TutorialUI");
        if (tutorialUI != null)
        {
            Debug.Log($"TutorialUI found: {tutorialUI.name}");
            Debug.Log($"TutorialUI active: {tutorialUI.activeInHierarchy}");
            
            var canvasGroup = tutorialUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"CanvasGroup alpha: {canvasGroup.alpha}");
            }
            
            var uiScript = tutorialUI.GetComponent<TutorialUIPrefab>();
            if (uiScript != null)
            {
                Debug.Log("TutorialUIPrefab script found");
                Debug.Log($"Dialogue text: {(uiScript.dialogueText != null ? "Found" : "Missing")}");
                Debug.Log($"Next button: {(uiScript.nextButton != null ? "Found" : "Missing")}");
            }
        }
        else
        {
            Debug.LogError("TutorialUI GameObject not found in scene!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (tutorialManager != null)
        {
            tutorialManager.OnTutorialCompleted -= HandleTutorialCompleted;
        }
    }

    private void CreateTutorialUIIfNeeded()
    {
        // Check if tutorial UI already exists
        GameObject existingUI = GameObject.Find("TutorialDialoguePanel");
        if (existingUI == null)
        {
            existingUI = GameObject.Find("TutorialCanvas");
        }
        if (existingUI == null)
        {
            existingUI = GameObject.Find("TutorialCanvas(Clone)");
        }
        
        if (existingUI != null)
        {
            Debug.Log($"Tutorial UI already exists: {existingUI.name}");
            return;
        }
        
        // Try to find the prefab if not assigned
        if (tutorialCanvasPrefab == null)
        {
            FindTutorialCanvasPrefab();
        }
        
        // Create tutorial UI from prefab
        if (tutorialCanvasPrefab != null)
        {
            Debug.Log("Creating tutorial UI from prefab...");
            GameObject tutorialUI = Instantiate(tutorialCanvasPrefab);
            tutorialUI.name = "TutorialCanvas"; // Remove (Clone) suffix
            
            // Make sure it has the TutorialUIPrefab component
            Transform dialoguePanel = tutorialUI.transform.Find("TutorialDialoguePanel");
            if (dialoguePanel != null)
            {
                TutorialUIPrefab uiPrefabScript = dialoguePanel.GetComponent<TutorialUIPrefab>();
                if (uiPrefabScript == null)
                {
                    uiPrefabScript = dialoguePanel.gameObject.AddComponent<TutorialUIPrefab>();
                }
                Debug.Log("Tutorial UI created successfully!");
            }
            else
            {
                Debug.LogWarning("Created tutorial UI but couldn't find TutorialDialoguePanel inside it!");
            }
        }
        else
        {
            Debug.LogWarning("Could not find or create tutorial UI! Creating a simple fallback UI...");
            CreateFallbackTutorialUI();
        }
    }
    
    private void FindTutorialCanvasPrefab()
    {
        // First try Resources folders (if user moved prefab there)
        string[] resourcePaths = {
            "TutorialCanvas",
            "Prefabs/TutorialCanvas",
            "UI/Tutorial/TutorialCanvas",
            "Tutorial/TutorialCanvas"
        };
        
        foreach (string path in resourcePaths)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab != null)
            {
                tutorialCanvasPrefab = prefab;
                Debug.Log($"Found tutorial canvas prefab in Resources at: {path}");
                return;
            }
        }
        
        // If not found in Resources, search in the entire project using AddressableAssetSettings or AssetDatabase
        #if UNITY_EDITOR
        // This only works in the editor
        string[] guids = UnityEditor.AssetDatabase.FindAssets("TutorialCanvas t:GameObject");
        foreach (string guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null && prefab.name == "TutorialCanvas")
            {
                tutorialCanvasPrefab = prefab;
                Debug.Log($"Found tutorial canvas prefab at: {assetPath}");
                return;
            }
        }
        #endif
        
        Debug.LogWarning("Could not find TutorialCanvas prefab. Please manually assign it in the TutorialSystem inspector.");
    }
    
    private void CreateFallbackTutorialUI()
    {
        Debug.Log("Creating fallback tutorial UI...");
        
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create main tutorial panel
        GameObject tutorialPanel = new GameObject("TutorialDialoguePanel");
        tutorialPanel.transform.SetParent(canvas.transform, false);

        // Setup RectTransform
        RectTransform panelRect = tutorialPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.4f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        // Add background
        UnityEngine.UI.Image panelImage = tutorialPanel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // Add CanvasGroup
        CanvasGroup canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Create character name text
        GameObject nameObj = new GameObject("CharacterName");
        nameObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.7f);
        nameRect.anchorMax = new Vector2(0.5f, 0.9f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI nameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
        nameText.text = "Old Pete";
        nameText.fontSize = 24;
        nameText.color = Color.white;

        // Create dialogue text
        GameObject dialogueObj = new GameObject("DialogueText");
        dialogueObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform dialogueRect = dialogueObj.AddComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0.05f, 0.2f);
        dialogueRect.anchorMax = new Vector2(0.95f, 0.65f);
        dialogueRect.anchoredPosition = Vector2.zero;
        dialogueRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI dialogueText = dialogueObj.AddComponent<TMPro.TextMeshProUGUI>();
        dialogueText.text = "Welcome to the tutorial!";
        dialogueText.fontSize = 18;
        dialogueText.color = Color.white;
        dialogueText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Create next button
        GameObject buttonObj = new GameObject("NextButton");
        buttonObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.7f, 0.05f);
        buttonRect.anchorMax = new Vector2(0.95f, 0.15f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = Vector2.zero;

        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = new Color(0.3f, 0.6f, 1f, 1f);
        
        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();

        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.anchoredPosition = Vector2.zero;
        buttonTextRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        buttonText.text = "Next";
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TMPro.TextAlignmentOptions.Center;

        // Add TutorialUIPrefab component
        TutorialUIPrefab uiPrefab = tutorialPanel.AddComponent<TutorialUIPrefab>();
        uiPrefab.characterNameText = nameText;
        uiPrefab.dialogueText = dialogueText;
        uiPrefab.nextButton = button;
        uiPrefab.canvasGroup = canvasGroup;
        uiPrefab.backgroundPanel = panelImage;

        Debug.Log("Fallback tutorial UI created successfully!");
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (tutorialCanvasPrefab == null)
        {
            FindTutorialCanvasPrefab();
        }
    }
    #endif
}
