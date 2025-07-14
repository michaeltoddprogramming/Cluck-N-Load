using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple manual tutorial setup - more reliable than auto-setup
/// </summary>
public class TutorialManualSetup : MonoBehaviour
{
    [Header("Manual Setup")]
    [SerializeField] private bool setupOnStart = true;
    
    [ContextMenu("Create Simple Tutorial UI")]
    public void CreateSimpleTutorialUI()
    {
        Debug.Log("Creating simple tutorial UI manually...");
        
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found! Please create a Canvas first.");
            return;
        }

        // Delete any existing tutorial UI
        GameObject existingUI = GameObject.Find("TutorialUI");
        if (existingUI != null)
        {
            DestroyImmediate(existingUI);
        }

        // Create main panel
        GameObject tutorialPanel = new GameObject("TutorialUI");
        tutorialPanel.transform.SetParent(canvas.transform, false);

        // Setup RectTransform to be at bottom of screen with proper margins
        RectTransform panelRect = tutorialPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0);    // 5% margin from left
        panelRect.anchorMax = new Vector2(0.95f, 0.28f); // 5% margin from right, 28% height
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        // Add background with rounded corners effect
        Image panelImage = tutorialPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Darker, more opaque background

        // Add CanvasGroup for fading
        CanvasGroup canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // Start visible
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Create character portrait area
        GameObject portraitObj = new GameObject("CharacterPortrait");
        portraitObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.02f, 0.1f);
        portraitRect.anchorMax = new Vector2(0.18f, 0.9f);
        portraitRect.anchoredPosition = Vector2.zero;
        portraitRect.sizeDelta = Vector2.zero;

        Image portraitImage = portraitObj.AddComponent<Image>();
        portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Placeholder gray

        // Create character name text
        GameObject nameObj = new GameObject("CharacterName");
        nameObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.2f, 0.75f);
        nameRect.anchorMax = new Vector2(0.6f, 0.95f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Old Pete";
        nameText.fontSize = 20;
        nameText.color = new Color(1f, 0.8f, 0.3f, 1f); // Golden color
        nameText.fontStyle = FontStyles.Bold;

        // Create dialogue text with better positioning
        GameObject dialogueObj = new GameObject("DialogueText");
        dialogueObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform dialogueRect = dialogueObj.AddComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0.2f, 0.15f);
        dialogueRect.anchorMax = new Vector2(0.75f, 0.7f);
        dialogueRect.anchoredPosition = Vector2.zero;
        dialogueRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI dialogueText = dialogueObj.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "Well hello there, young farmer! I'm Old Pete, and I'll be your guide. Click 'Next' to continue!";
        dialogueText.fontSize = 16;
        dialogueText.color = Color.white;
        dialogueText.enableWordWrapping = true;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;

        // Create Next button with better positioning
        GameObject nextBtnObj = new GameObject("NextButton");
        nextBtnObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform nextRect = nextBtnObj.AddComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.78f, 0.15f);
        nextRect.anchorMax = new Vector2(0.95f, 0.4f);
        nextRect.anchoredPosition = Vector2.zero;
        nextRect.sizeDelta = Vector2.zero;

        Button nextButton = nextBtnObj.AddComponent<Button>();
        Image nextImage = nextBtnObj.AddComponent<Image>();
        nextImage.color = new Color(0.2f, 0.8f, 0.3f, 1f); // Brighter green

        // Next button text
        GameObject nextTextObj = new GameObject("Text");
        nextTextObj.transform.SetParent(nextBtnObj.transform, false);
        RectTransform nextTextRect = nextTextObj.AddComponent<RectTransform>();
        nextTextRect.anchorMin = Vector2.zero;
        nextTextRect.anchorMax = Vector2.one;
        nextTextRect.anchoredPosition = Vector2.zero;
        nextTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI nextButtonText = nextTextObj.AddComponent<TextMeshProUGUI>();
        nextButtonText.text = "Next";
        nextButtonText.fontSize = 14;
        nextButtonText.color = Color.white;
        nextButtonText.alignment = TextAlignmentOptions.Center;
        nextButtonText.fontStyle = FontStyles.Bold;

        // Create Skip All button with better positioning
        GameObject skipBtnObj = new GameObject("SkipAllButton");
        skipBtnObj.transform.SetParent(tutorialPanel.transform, false);
        RectTransform skipRect = skipBtnObj.AddComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.78f, 0.5f);
        skipRect.anchorMax = new Vector2(0.95f, 0.75f);
        skipRect.anchoredPosition = Vector2.zero;
        skipRect.sizeDelta = Vector2.zero;

        Button skipButton = skipBtnObj.AddComponent<Button>();
        Image skipImage = skipBtnObj.AddComponent<Image>();
        skipImage.color = new Color(0.8f, 0.3f, 0.2f, 1f); // Brighter red

        // Skip button text
        GameObject skipTextObj = new GameObject("Text");
        skipTextObj.transform.SetParent(skipBtnObj.transform, false);
        RectTransform skipTextRect = skipTextObj.AddComponent<RectTransform>();
        skipTextRect.anchorMin = Vector2.zero;
        skipTextRect.anchorMax = Vector2.one;
        skipTextRect.anchoredPosition = Vector2.zero;
        skipTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI skipButtonText = skipTextObj.AddComponent<TextMeshProUGUI>();
        skipButtonText.text = "Skip All";
        skipButtonText.fontSize = 12;
        skipButtonText.color = Color.white;
        skipButtonText.alignment = TextAlignmentOptions.Center;
        skipButtonText.fontStyle = FontStyles.Bold;

        // Connect buttons to tutorial manager
        nextButton.onClick.AddListener(() => {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NextTutorialStep();
                Debug.Log("Manual Next clicked!");
            }
        });

        skipButton.onClick.AddListener(() => {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SkipTutorial();
                Debug.Log("Manual Skip clicked!");
            }
        });

        // Add TutorialUIPrefab script and assign references
        TutorialUIPrefab uiScript = tutorialPanel.AddComponent<TutorialUIPrefab>();
        uiScript.characterNameText = nameText;
        uiScript.dialogueText = dialogueText;
        uiScript.nextButton = nextButton;
        uiScript.skipAllButton = skipButton;
        uiScript.backgroundPanel = panelImage;
        uiScript.canvasGroup = canvasGroup;

        Debug.Log("Simple tutorial UI created successfully!");
        
        // Force tutorial manager to find the new UI
        if (TutorialManager.Instance != null)
        {
            var tutorialManagerType = typeof(TutorialManager);
            var tutorialPanelField = tutorialManagerType.GetField("tutorialPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tutorialPanelField?.SetValue(TutorialManager.Instance, tutorialPanel);
            
            var tutorialDescriptionField = tutorialManagerType.GetField("tutorialDescription", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tutorialDescriptionField?.SetValue(TutorialManager.Instance, dialogueText);
            
            var tutorialTitleField = tutorialManagerType.GetField("tutorialTitle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tutorialTitleField?.SetValue(TutorialManager.Instance, nameText);
            
            Debug.Log("Connected to TutorialManager!");
        }
    }

    [ContextMenu("Clean Up Old Tutorial UI")]
    public void CleanUpOldTutorialUI()
    {
        // Find and destroy all old tutorial UI elements
        GameObject[] oldUIs = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (GameObject obj in oldUIs)
        {
            if (obj.name.Contains("Tutorial") && obj != gameObject)
            {
                DestroyImmediate(obj);
                Debug.Log($"Removed old tutorial element: {obj.name}");
            }
        }

        // Specifically look for the green box and other problematic elements
        GameObject greenBox = GameObject.Find("TutorialUI");
        if (greenBox != null)
        {
            DestroyImmediate(greenBox);
            Debug.Log("Removed old TutorialUI");
        }

        // Look for any objects with TutorialUIPrefab components
        TutorialUIPrefab[] oldScripts = FindObjectsOfType<TutorialUIPrefab>();
        foreach (var script in oldScripts)
        {
            if (script.gameObject != null && script.gameObject.name != "TutorialUI")
            {
                DestroyImmediate(script.gameObject);
                Debug.Log($"Removed old tutorial UI object: {script.gameObject.name}");
            }
        }

        Debug.Log("Old tutorial UI cleanup complete!");
    }

    private void Start()
    {
        if (setupOnStart)
        {
            Invoke(nameof(CreateSimpleTutorialUI), 0.5f);
        }
    }
}
