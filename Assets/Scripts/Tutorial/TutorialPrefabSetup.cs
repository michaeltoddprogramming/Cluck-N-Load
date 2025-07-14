using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple tutorial setup using your existing prefab - much more reliable!
/// </summary>
public class TutorialPrefabSetup : MonoBehaviour
{
    [Header("Use Your Existing Prefab")]
    [SerializeField] private GameObject tutorialCanvasPrefab;
    [SerializeField] private bool setupOnStart = true;
    
    private GameObject spawnedTutorialCanvas;
    private TutorialUIPrefab tutorialUIScript;

    [ContextMenu("Setup Tutorial with Prefab")]
    public void SetupTutorialWithPrefab()
    {
        Debug.Log("Setting up tutorial with your prefab...");
        
        // Clean up any existing tutorial UIs first
        CleanupExistingTutorialUIs();
        
        // Spawn your prefab
        if (tutorialCanvasPrefab != null)
        {
            spawnedTutorialCanvas = Instantiate(tutorialCanvasPrefab);
            spawnedTutorialCanvas.name = "TutorialCanvas"; // Remove (Clone)
            
            // Find the dialogue panel in your prefab
            Transform dialoguePanel = spawnedTutorialCanvas.transform.Find("TutorialDialoguePanel");
            if (dialoguePanel == null)
            {
                // Try alternative names
                dialoguePanel = spawnedTutorialCanvas.transform.GetComponentInChildren<TutorialUIPrefab>()?.transform;
            }
            
            if (dialoguePanel != null)
            {
                // Get or add the TutorialUIPrefab script
                tutorialUIScript = dialoguePanel.GetComponent<TutorialUIPrefab>();
                if (tutorialUIScript == null)
                {
                    tutorialUIScript = dialoguePanel.gameObject.AddComponent<TutorialUIPrefab>();
                }
                
                // Auto-assign components if not already assigned
                AutoAssignUIComponents(dialoguePanel.gameObject);
                
                // Connect to tutorial manager
                ConnectToTutorialManager(dialoguePanel.gameObject);
                
                Debug.Log("Tutorial prefab setup complete!");
            }
            else
            {
                Debug.LogError("Could not find TutorialDialoguePanel in your prefab!");
            }
        }
        else
        {
            Debug.LogError("No tutorial canvas prefab assigned! Please assign your TutorialCanvas prefab.");
        }
    }

    private void CleanupExistingTutorialUIs()
    {
        // Remove any existing tutorial UIs
        GameObject[] tutorialObjects = {
            GameObject.Find("TutorialCanvas"),
            GameObject.Find("TutorialCanvas(Clone)"),
            GameObject.Find("TutorialUI"),
            GameObject.Find("TutorialDialoguePanel")
        };

        foreach (var obj in tutorialObjects)
        {
            if (obj != null && obj != spawnedTutorialCanvas)
            {
                DestroyImmediate(obj);
                Debug.Log($"Cleaned up old tutorial UI: {obj.name}");
            }
        }
    }

    private void AutoAssignUIComponents(GameObject dialoguePanel)
    {
        if (tutorialUIScript == null) return;

        // Auto-find components by name if not already assigned
        if (tutorialUIScript.characterPortrait == null)
        {
            var portrait = dialoguePanel.transform.Find("CharacterPortrait")?.GetComponent<Image>();
            if (portrait != null) tutorialUIScript.characterPortrait = portrait;
        }

        if (tutorialUIScript.characterNameText == null)
        {
            var nameText = dialoguePanel.transform.Find("CharacterName")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) tutorialUIScript.characterNameText = nameText;
        }

        if (tutorialUIScript.dialogueText == null)
        {
            var dialogue = dialoguePanel.transform.Find("DialogueText")?.GetComponent<TextMeshProUGUI>();
            if (dialogue != null) tutorialUIScript.dialogueText = dialogue;
        }

        if (tutorialUIScript.nextButton == null)
        {
            var nextBtn = dialoguePanel.transform.Find("NextButton")?.GetComponent<Button>();
            if (nextBtn != null) tutorialUIScript.nextButton = nextBtn;
        }

        if (tutorialUIScript.skipButton == null)
        {
            var skipBtn = dialoguePanel.transform.Find("SkipButton")?.GetComponent<Button>();
            if (skipBtn != null) tutorialUIScript.skipButton = skipBtn;
        }

        if (tutorialUIScript.skipAllButton == null)
        {
            var skipAllBtn = dialoguePanel.transform.Find("SkipAllButton")?.GetComponent<Button>();
            if (skipAllBtn != null) tutorialUIScript.skipAllButton = skipAllBtn;
        }

        if (tutorialUIScript.progressSlider == null)
        {
            var slider = dialoguePanel.transform.Find("ProgressSlider")?.GetComponent<Slider>();
            if (slider != null) tutorialUIScript.progressSlider = slider;
        }

        if (tutorialUIScript.progressText == null)
        {
            var progressTxt = dialoguePanel.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            if (progressTxt != null) tutorialUIScript.progressText = progressTxt;
        }

        if (tutorialUIScript.backgroundPanel == null)
        {
            var bg = dialoguePanel.GetComponent<Image>();
            if (bg != null) tutorialUIScript.backgroundPanel = bg;
        }

        if (tutorialUIScript.canvasGroup == null)
        {
            var cg = dialoguePanel.GetComponent<CanvasGroup>();
            if (cg != null) tutorialUIScript.canvasGroup = cg;
        }

        Debug.Log("UI components auto-assigned from prefab");
    }

    private void ConnectToTutorialManager(GameObject dialoguePanel)
    {
        // Connect to tutorial manager using reflection (since fields are private)
        if (TutorialManager.Instance != null)
        {
            var tutorialManagerType = typeof(TutorialManager);
            
            // Set tutorial panel
            var tutorialPanelField = tutorialManagerType.GetField("tutorialPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            tutorialPanelField?.SetValue(TutorialManager.Instance, dialoguePanel);
            
            // Set other UI references
            if (tutorialUIScript != null)
            {
                var tutorialDescriptionField = tutorialManagerType.GetField("tutorialDescription", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                tutorialDescriptionField?.SetValue(TutorialManager.Instance, tutorialUIScript.dialogueText);
                
                var tutorialTitleField = tutorialManagerType.GetField("tutorialTitle", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                tutorialTitleField?.SetValue(TutorialManager.Instance, tutorialUIScript.characterNameText);
                
                var nextButtonField = tutorialManagerType.GetField("nextButton", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                nextButtonField?.SetValue(TutorialManager.Instance, tutorialUIScript.nextButton);
                
                var skipButtonField = tutorialManagerType.GetField("skipButton", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                skipButtonField?.SetValue(TutorialManager.Instance, tutorialUIScript.skipButton);
                
                var characterPortraitField = tutorialManagerType.GetField("characterPortrait", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                characterPortraitField?.SetValue(TutorialManager.Instance, tutorialUIScript.characterPortrait);
            }
            
            Debug.Log("Connected prefab to TutorialManager!");
        }
    }

    [ContextMenu("Test Tutorial UI")]
    public void TestTutorialUI()
    {
        if (spawnedTutorialCanvas != null)
        {
            var dialoguePanel = spawnedTutorialCanvas.transform.Find("TutorialDialoguePanel");
            if (dialoguePanel != null)
            {
                dialoguePanel.gameObject.SetActive(true);
                
                var canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                
                Debug.Log("Tutorial UI should now be visible!");
            }
        }
    }

    private void Start()
    {
        if (setupOnStart && tutorialCanvasPrefab != null)
        {
            Invoke(nameof(SetupTutorialWithPrefab), 0.5f);
        }
    }

    private void OnValidate()
    {
        // Auto-find the tutorial canvas prefab if not assigned
        if (tutorialCanvasPrefab == null)
        {
            // Look for it in Resources or Assets
            tutorialCanvasPrefab = Resources.Load<GameObject>("TutorialCanvas");
            if (tutorialCanvasPrefab == null)
            {
                Debug.Log("Please assign your TutorialCanvas prefab to the Tutorial Prefab Setup component");
            }
        }
    }
}
