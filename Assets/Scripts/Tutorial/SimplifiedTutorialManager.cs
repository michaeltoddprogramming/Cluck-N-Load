using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simplified Tutorial Manager focused on Pete3D integration
/// Removes old complex systems and focuses on clean Pete-driven tutorials
/// 
/// Pete Display Integration:
/// - UIHelper context: Pete appears as RawImage in tutorial dialogue panel (using render texture)
/// - WorldGuide context: Pete appears in 3D world space 
/// - CornerBuddy context: Pete appears in UI corner
/// 
/// Setup Requirements:
/// - Assign Pete3DGuide with render texture components configured
/// - Pete RawImage should be child of tutorialDialoguePanel
/// - Tutorial dialogue panel contains title, message, next button, and Pete display
/// </summary>
public class SimplifiedTutorialManager : MonoBehaviour
{
    [Header("Essential UI References")]
    public GameObject tutorialDialoguePanel;
    public GameObject panel;                   // The background panel to fade
    public GameObject contentContainer;        // The inner content that moves down
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI titleText;
    public RawImage characterPortraitImage;
    public Button nextStepButton;
    public Button skipTutorialButton;
    
    [Header("Game UI Control")]
    public GameObject gameUICanvas;            // Main game UI canvas to hide during tutorial
    public Button shopButton;                  // Reference to shop button for highlighting
    
    [Header("UI Highlighting")]
    [Range(0.5f, 3.0f)]
    public float highlightPulseSpeed = 1.5f;  // Speed of the pulsing animation
    [Range(0.3f, 1.0f)] 
    public float highlightMinAlpha = 0.4f;    // Minimum alpha during pulse
    public Color highlightColor = new Color(1f, 0.8f, 0.2f, 0.8f); // Golden glow color
    
    [Header("Key Indicator System")]
    public GameObject keyIndicatorPrefab;
    public RectTransform keyIndicatorContainer;
    
    [Header("Mouse Icon Sprites")]
    public Sprite lmbIcon;      // Left Mouse Button
    public Sprite rmbIcon;      // Right Mouse Button  
    public Sprite mmbIcon;      // Middle Mouse Button
    public Sprite mmbDownIcon;  // Middle Mouse Button Down (scroll down)
    public Sprite mmbUpIcon;    // Middle Mouse Button Up (scroll up)
    
    [Header("Mouse Icon Settings")]
    [Range(0.5f, 5.0f)]
    public float mouseIconScale = 2.0f;  // Configurable scale for mouse icons
    // Small offset so designers can fine tune mouse icon placement relative to keyboard keys
    // Raised default Y so mouse icons sit higher above keyboard keys (avoids overlap)
    public Vector2 mouseIconOffset = new Vector2(0f, 14f);
    
    [System.Serializable]
    public class KeyPositionOverride
    {
        public KeyCode key;
        public Vector2 offset = Vector2.zero;
    }

    [Header("Per-Key Position Overrides")]
    // Add entries here in the inspector to nudge individual key or mouse icon positions
    public List<KeyPositionOverride> keyPositionOverrides = new List<KeyPositionOverride>();
    
    [Header("Simplified Progress (No Complex Checklist)")]
    public GameObject progressPanel;
    public TextMeshProUGUI progressText;
    
    [Header("Pete3D System")]
    public Pete3DGuide pete3DGuide;
    public bool usePete3D = true;
    
    [Header("Audio - Pete Speaking System")]
    public AudioSource mumbleAudioSource;      // Pete's voice/mumble sounds
    public AudioClip[] mumbleClips;            // Array of Pete mumble sounds
    public float typeSpeed = 0.04f;            // Speed of text typing animation
    public AudioClip keyPressSound;            // Sound when keys are pressed
    private AudioSource effectsAudioSource;   // For sound effects
    private bool isMumblePaused = false;       // Control mumble playback
    
    // Core tutorial data
    [System.Serializable]
    public class SimpleTutorialStep
    {
        public string stepId;
        public string title;
        [TextArea(2, 4)]
        public string message;
        public PeteContext peteContext = PeteContext.Auto;
        public PeteEmotion peteEmotion = PeteEmotion.Neutral;
        public Vector3 peteWorldPosition = Vector3.zero;
        public Transform peteTarget;
        public GameObject uiToHighlight;
        public bool waitForAction = false;
        public string waitForTrigger = "";
        
        [Header("Input Detection")]
        public List<KeyCode> requiredInputs = new List<KeyCode>();  // Keys to detect
        public bool waitForAllInputs = false;                        // Wait for all keys or just any key
        
        [Header("Simple Panel Control")]
        public bool movePanelDown = false;          // Move content container to bottom of screen
        public bool movePanelRight = false;         // Move content container to right side of screen
        public float panelAlpha = 1.0f;             // Background panel opacity (0-1, fade the background)
        public bool disablePanelRaycast = false;    // Disable panel blocking clicks (for UI interaction steps)
        
        [Header("Game UI Control")]
        public bool showGameUI = false;             // Show the main game UI for this step (hidden by default during tutorial)
        public GameObject highlightUIElement;       // UI element to highlight for this step (e.g., shop button)
    }
    
    [Header("Tutorial Steps")]
    public List<SimpleTutorialStep> tutorialSteps = new List<SimpleTutorialStep>();
    
    // Runtime state
    private int currentStepIndex = -1;
    private bool tutorialActive = false;
    private bool waitingForPlayerAction = false;
    private HashSet<string> completedTriggers = new HashSet<string>();
    private Coroutine typingCoroutine; // Track typing coroutine for cleanup
    
    // Simple panel control
    private RectTransform contentContainerRect;
    private CanvasGroup panelCanvasGroup;        // CanvasGroup on the Panel for fading
    private Vector2 originalContentPosition;
    private Coroutine panelTweenCoroutine;       // Track tween animation
    
    // Key indicator system
    private HashSet<KeyCode> detectedInputs = new HashSet<KeyCode>();
    private List<GameObject> keyIndicators = new List<GameObject>();
    private Dictionary<KeyCode, GameObject> keyIndicatorMap = new Dictionary<KeyCode, GameObject>();
    
    // UI highlighting system
    private GameObject currentHighlightEffect;
    private float stepStartTime; // Track when current step started
    
    // Singleton
    public static SimplifiedTutorialManager Instance { get; private set; }
    
    private void Awake()
    {
        // Simple singleton - don't persist across scenes
        // Tutorial should restart fresh in each scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        SetupTutorialSteps();
        SetupUI();
    }
    
    private void Start()
    {
        StartTutorial();
    }
    
    private void Update()
    {
        // Handle key input detection for current step
        HandleRequiredInputDetection();
    }
    
    private void SetupTutorialSteps()
    {
        tutorialSteps.Clear();
        
        // Welcome step - normal position, full opacity
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "welcome",
            title = "Theres a new guy in town",
            message = "I don't know why I am here, but someone told theres a new idiot in town!",
            peteContext = PeteContext.UIHelper,
            peteEmotion = PeteEmotion.Excited,
            waitForAction = false
        });
        
        // Camera movement - show WASD key indicators
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "camera_movement_wasd",
            title = "Move Camera",
            message = "Use WASD to move the camera around. Try it!",
            peteContext = PeteContext.CornerBuddy,
            peteEmotion = PeteEmotion.Pointing,
            waitForAction = true,
            waitForTrigger = "camera_moved_wasd",
            requiredInputs = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D },
            waitForAllInputs = true,
            movePanelDown = true,
            panelAlpha = 0f
        });
        
        // Camera rotation - show QE and mouse key indicators
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "camera_rotation",
            title = "Rotate Camera",
            message = "Use Q and E, or hold middle mouse button and drag to rotate the camera.",
            peteContext = PeteContext.CornerBuddy,
            peteEmotion = PeteEmotion.Pointing,
            waitForAction = true,
            waitForTrigger = "camera_rotated",
            requiredInputs = new List<KeyCode> { KeyCode.Q, KeyCode.E, KeyCode.Mouse2 }, // Mouse2 = MMB
            waitForAllInputs = true, // MUST do all inputs
            movePanelDown = true,
            panelAlpha = 0f
        });
        
        // Camera zoom - require keys 1 & 2 AND scroll wheel up/down
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "camera_zoom",
            title = "Zoom Camera",
            message = "Use the scroll wheel AND the 1 and 2 keys to zoom in and out.",
            peteContext = PeteContext.CornerBuddy,
            peteEmotion = PeteEmotion.Pointing,
            waitForAction = true,
            waitForTrigger = "camera_zoomed",
            // Require Alpha1, Alpha2 (number keys) plus mouse scroll up/down (Mouse3/Mouse4)
            requiredInputs = new List<KeyCode> { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Mouse3, KeyCode.Mouse4 },
            waitForAllInputs = true,
            movePanelDown = true,
            panelAlpha = 0f
        });
        
        // Camera drag - show click + hold + drag
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "camera_drag",
            title = "Drag Camera",
            message = "Hold left mouse button and drag to rotate the camera view.",
            peteContext = PeteContext.CornerBuddy,
            peteEmotion = PeteEmotion.Pointing,
            waitForAction = true,
            waitForTrigger = "camera_dragged",
            requiredInputs = new List<KeyCode> { KeyCode.Mouse0 }, // Mouse0 = LMB
            waitForAllInputs = true,
            movePanelDown = true,
            panelAlpha = 0f
        });
        
        // Shop introduction - normal position, full opacity, allow clicks through panel
        var openShopStep = new SimpleTutorialStep
        {
            stepId = "open_shop",
            title = "Let's Build!",
            message = "Click the shop button to start building structures!",
            peteContext = PeteContext.UIHelper,
            peteEmotion = PeteEmotion.Excited,
            waitForAction = true,
            waitForTrigger = "shop_opened",
            disablePanelRaycast = true,  // Allow clicks through the panel to reach the shop button
            panelAlpha = 0f,
            showGameUI = true            // Show game UI so player can see shop button
        };
        
        // Set shop button as highlight target if available
        if (shopButton != null)
        {
            openShopStep.highlightUIElement = shopButton.gameObject;
        }
        
        tutorialSteps.Add(openShopStep);
        
        // Build farmhouse - move dialogue to right, invisible panel, allow clicks
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Your Home",
            message = "Every farm needs a farmhouse! Build yours first.",
            peteContext = PeteContext.CornerBuddy,
            peteEmotion = PeteEmotion.Worried,
            waitForAction = true,
            waitForTrigger = "farmhouse_built",
            movePanelRight = true,       // Move dialogue to right side
            panelAlpha = 0f,             // Fully transparent panel
            disablePanelRaycast = true,  // Allow clicks to place building
            showGameUI = true            // Keep game UI visible for building interaction
        });
        
        // Tutorial complete - back to normal
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "complete",
            title = "Great Job!",
            message = "You're ready to farm! Good luck out there!",
            peteContext = PeteContext.UIHelper,
            peteEmotion = PeteEmotion.Celebrating,
            showGameUI = true            // Ensure game UI is visible at the end
        });
    }
    
    private void SetupUI()
    {
        if (nextStepButton != null)
        {
            nextStepButton.onClick.AddListener(NextStep);
        }
        
        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.AddListener(SkipTutorial);
        }
        
        if (tutorialDialoguePanel != null)
        {
            tutorialDialoguePanel.SetActive(false);
        }
        
        // Get the Panel GameObject and setup CanvasGroup for fading
        if (panel != null)
        {
            // Get or add CanvasGroup for fading the background panel
            panelCanvasGroup = panel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = panel.AddComponent<CanvasGroup>();
            }
            Debug.Log($"[SetupUI] Panel CanvasGroup setup complete: {panelCanvasGroup != null}");
        }
        else
        {
            Debug.LogError("[SetupUI] Panel is NULL! Panel fading won't work.");
        }
        
        // Get content container for moving down
        if (contentContainer != null)
        {
            contentContainerRect = contentContainer.GetComponent<RectTransform>();
            if (contentContainerRect != null)
            {
                originalContentPosition = contentContainerRect.anchoredPosition;
                Debug.Log($"[SetupUI] ContentContainer setup. Original position: {originalContentPosition}");
            }
        }
        else
        {
            Debug.LogError("[SetupUI] ContentContainer is NULL! Panel movement won't work.");
        }
        
        // Setup effects audio source for key press sounds
        if (effectsAudioSource == null)
        {
            effectsAudioSource = gameObject.AddComponent<AudioSource>();
            effectsAudioSource.playOnAwake = false;
            effectsAudioSource.volume = 0.7f;
        }
        
        // Debug check for Pete's mumble audio
        if (mumbleAudioSource == null)
        {
            Debug.LogError("[SetupUI] mumbleAudioSource is NULL! Pete won't speak.");
        }
        else
        {
            Debug.Log($"[SetupUI] mumbleAudioSource is assigned.");
        }
        
        if (mumbleClips == null || mumbleClips.Length == 0)
        {
            Debug.LogError("[SetupUI] mumbleClips is NULL or empty! Pete won't speak.");
        }
        else
        {
            Debug.Log($"[SetupUI] mumbleClips has {mumbleClips.Length} clips.");
        }
    }
    
    public void StartTutorial()
    {
        if (tutorialActive) return;
        
        // Hide main game UI at start of tutorial
        HideGameUI();
        
        tutorialActive = true;
        currentStepIndex = -1;
        NextStep();
    }
    
    public void NextStep()
    {
        if (!tutorialActive) return;
        
        // Stop any ongoing typing and mumbling from previous step
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        StopMumbling();
        
        // Clear key indicators from previous step
        ClearKeyIndicators();
        
        currentStepIndex++;
        
        if (currentStepIndex >= tutorialSteps.Count)
        {
            EndTutorial();
            return;
        }
        
        ShowCurrentStep();
    }
    
    private void ShowCurrentStep()
    {
        var step = tutorialSteps[currentStepIndex];
        
        // Track when this step started
        stepStartTime = Time.time;
        
        Debug.Log($"Showing tutorial step {currentStepIndex}: {step.title}");
        
        // Show dialogue
        if (tutorialDialoguePanel != null)
        {
            tutorialDialoguePanel.SetActive(true);
            
            // Update panel position and opacity for this step
            UpdatePanelForStep(step);
            
            Debug.Log("Tutorial dialogue panel activated");
        }
        else
        {
            Debug.LogError("Tutorial dialogue panel is null!");
        }
        
        // Set text content
        if (titleText != null)
        {
            titleText.text = step.title;
            Debug.Log($"Set title text: {step.title}");
        }
        else
        {
            Debug.LogError("Title text component is null!");
        }
        
        if (dialogueText != null)
        {
            // Stop any existing typing before starting new text
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                StopMumbling();
            }
            
            typingCoroutine = StartCoroutine(TypeText(step.message));
            Debug.Log($"Starting to type message: {step.message}");
        }
        else
        {
            Debug.LogError("Dialogue text component is null!");
        }
        
        // Update progress
        UpdateProgress();
        
        // Show Pete - will display in dialogue panel for UIHelper context
        if (usePete3D && pete3DGuide != null)
        {
            var peteStep = new TutorialStep
            {
                stepId = step.stepId,
                title = step.title,
                instructionText = step.message,
                peteContext = step.peteContext,
                peteEmotion = step.peteEmotion,
                peteWorldPosition = step.peteWorldPosition,
                peteWorldTarget = step.peteTarget,
                uiToHighlight = step.uiToHighlight
            };
            
            pete3DGuide.OnStepStart(peteStep);
        }
        
        // Handle step requirements
        if (step.waitForAction && !string.IsNullOrEmpty(step.waitForTrigger))
        {
            waitingForPlayerAction = true;
            if (nextStepButton != null)
            {
                nextStepButton.gameObject.SetActive(false); // Hide button entirely for required actions
            }
            Debug.Log($"Waiting for action: {step.waitForTrigger}");
            
            // Show key indicators if this step has required inputs
            if (step.requiredInputs != null && step.requiredInputs.Count > 0)
            {
                Debug.Log($"[ShowCurrentStep] Step has {step.requiredInputs.Count} required inputs. Calling ShowKeyIndicators...");
                detectedInputs.Clear();
                ShowKeyIndicators(step.requiredInputs);
            }
            else
            {
                Debug.Log("[ShowCurrentStep] Step has NO required inputs");
            }
        }
        else
        {
            waitingForPlayerAction = false;
            if (nextStepButton != null)
            {
                nextStepButton.gameObject.SetActive(true); // Show button for optional steps
                nextStepButton.interactable = true;
            }
            Debug.Log("Next button enabled - ready for manual progression");
        }
        
        // Play audio
        PlayTutorialSound();
    }
    
    private IEnumerator TypeText(string text)
    {
        if (dialogueText == null) yield break;
        
        // Stop any existing mumbling before starting new text
        StopMumbling();
        
        dialogueText.text = "";
        
        // Start Pete's mumbling during typing (like old tutorial)
        bool shouldPlayMumble = mumbleAudioSource != null && mumbleClips != null && mumbleClips.Length > 0;
        Coroutine mumbleCoroutine = null;
        
        Debug.Log($"[TypeText] shouldPlayMumble: {shouldPlayMumble} (audioSource: {mumbleAudioSource != null}, clips: {mumbleClips?.Length ?? 0})");
        
        if (shouldPlayMumble)
        {
            mumbleCoroutine = StartCoroutine(PlayMumbleDuringTyping());
            Debug.Log("[TypeText] Started Pete's mumbling coroutine");
        }
        else
        {
            Debug.LogWarning("[TypeText] Pete's mumbling NOT started - check inspector references!");
        }
        
        // Type each character with timing
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        
        // ALWAYS stop mumbling when typing is done (regardless of mumbleCoroutine reference)
        StopMumbling();
        
        if (mumbleCoroutine != null)
        {
            StopCoroutine(mumbleCoroutine);
            Debug.Log("[TypeText] Stopped Pete's mumbling coroutine");
        }
        
        typingCoroutine = null; // Clear the typing coroutine reference
    }
    
    private IEnumerator PlayMumbleDuringTyping()
    {
        isMumblePaused = false;
        
        while (!isMumblePaused)
        {
            if (mumbleAudioSource != null && !mumbleAudioSource.isPlaying && mumbleClips != null && mumbleClips.Length > 0)
            {
                // Play a random mumble clip
                AudioClip mumbleClip = mumbleClips[Random.Range(0, mumbleClips.Length)];
                mumbleAudioSource.clip = mumbleClip;
                mumbleAudioSource.Play();
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text = $"Step {currentStepIndex + 1} of {tutorialSteps.Count}";
        }
    }
    
    private void PlayTutorialSound()
    {
        // Pete's speaking is handled during text typing via mumble system
        // This method can be used for other tutorial sounds if needed
        
        // Stop any existing mumble to prepare for new dialogue
        if (mumbleAudioSource != null && mumbleAudioSource.isPlaying)
        {
            mumbleAudioSource.Stop();
        }
        isMumblePaused = false;
    }
    
    // Method to play key press sound (like old tutorial)
    public void PlayKeyPressSound()
    {
        if (keyPressSound != null && effectsAudioSource != null)
        {
            effectsAudioSource.PlayOneShot(keyPressSound, 0.7f);
        }
    }
    
    // Method to stop Pete's mumbling (for skip/end tutorial)
    private void StopMumbling()
    {
        isMumblePaused = true;
        if (mumbleAudioSource != null && mumbleAudioSource.isPlaying)
        {
            mumbleAudioSource.Stop();
        }
    }
    
    // Simple panel control methods
    private void UpdatePanelForStep(SimpleTutorialStep step)
    {
        Debug.Log($"[UpdatePanelForStep] Called for step: {step.stepId}, panelAlpha: {step.panelAlpha}, movePanelDown: {step.movePanelDown}, movePanelRight: {step.movePanelRight}, disableRaycast: {step.disablePanelRaycast}, showGameUI: {step.showGameUI}");
        
        // Control main game UI visibility
        if (step.showGameUI)
        {
            ShowGameUI();
        }
        else
        {
            HideGameUI();
        }
        
        // Handle UI highlighting - delay to ensure UI is active
        ClearUIHighlight();
        if (step.highlightUIElement != null)
        {
            StartCoroutine(DelayedHighlight(step.highlightUIElement, 0.1f));
        }
        
        // Control panel raycast blocking
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.blocksRaycasts = !step.disablePanelRaycast;
        }
        
        // Stop any existing tween animation
        if (panelTweenCoroutine != null)
        {
            StopCoroutine(panelTweenCoroutine);
        }
        
        // Start smooth tween animation
        panelTweenCoroutine = StartCoroutine(TweenPanelAndContent(step));
    }
    
    private IEnumerator TweenPanelAndContent(SimpleTutorialStep step)
    {
        float duration = 0.5f; // Smooth animation duration
        float elapsed = 0f;
        
        // Get starting values
        float startAlpha = panelCanvasGroup != null ? panelCanvasGroup.alpha : 1f;
        Vector2 startPosition = contentContainerRect != null ? contentContainerRect.anchoredPosition : originalContentPosition;
        
        // Determine target values
        float targetAlpha = step.panelAlpha;
        Vector2 targetPosition = originalContentPosition;
        
        // Handle different panel positioning
        if (step.movePanelDown)
        {
            targetPosition = new Vector2(originalContentPosition.x, originalContentPosition.y - 280f);
        }
        else if (step.movePanelRight)
        {
            targetPosition = new Vector2(originalContentPosition.x + 500f, originalContentPosition.y);
        }
        
        // Smooth tween animation
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Use ease-out curve for smooth deceleration
            t = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease-out
            
            // Fade the background Panel
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }
            
            // Move content container
            if (contentContainerRect != null)
            {
                contentContainerRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            }
            
            yield return null;
        }
        
        // Ensure final values are set
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = targetAlpha;
        }
        
        if (contentContainerRect != null)
        {
            contentContainerRect.anchoredPosition = targetPosition;
        }
        
        panelTweenCoroutine = null;
    }
    
    // Reset panel to original state
    private void ResetPanel()
    {
        // Stop any ongoing animation
        if (panelTweenCoroutine != null)
        {
            StopCoroutine(panelTweenCoroutine);
            panelTweenCoroutine = null;
        }
        
        // Instant reset (no animation on cleanup)
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1.0f;
            panelCanvasGroup.blocksRaycasts = true; // Re-enable raycast blocking
        }
        
        if (contentContainerRect != null)
        {
            contentContainerRect.anchoredPosition = originalContentPosition;
        }
    }
    
    public void TriggerAction(string triggerName)
    {
        if (!tutorialActive || !waitingForPlayerAction) return;
        if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count) return;
        
        var currentStep = tutorialSteps[currentStepIndex];
        
        if (currentStep.waitForTrigger == triggerName)
        {
            completedTriggers.Add(triggerName);
            waitingForPlayerAction = false;
            
            // Clear key indicators when action is completed
            ClearKeyIndicators();
            
            // Pete celebrates
            if (usePete3D && pete3DGuide != null)
            {
                pete3DGuide.OnStepComplete();
            }
            
            Debug.Log($"Trigger '{triggerName}' matched! Auto-advancing to next step.");
            
            // Auto-advance to next step after a short delay
            StartCoroutine(DelayedNextStep(0.2f));
        }
    }
    
    private IEnumerator DelayedNextStep(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextStep();
    }
    
    public void SkipTutorial()
    {
        StopMumbling(); // Stop Pete's speaking
        EndTutorial();
    }
    
    private void EndTutorial()
    {
        tutorialActive = false;
        waitingForPlayerAction = false;
        
        // Stop any ongoing typing and mumbling
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        StopMumbling();
        
        // Clear key indicators
        ClearKeyIndicators();
        
        // Clear any UI highlights
        ClearUIHighlight();
        
        // Reset panel to original state
        ResetPanel();
        
        // Show main game UI when tutorial ends
        ShowGameUI();
        
        // Hide Pete first
        if (usePete3D && pete3DGuide != null)
        {
            pete3DGuide.HidePete();
        }
        
        if (tutorialDialoguePanel != null)
        {
            tutorialDialoguePanel.SetActive(false);
        }
        
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
        
        if (usePete3D && pete3DGuide != null)
        {
            pete3DGuide.HidePete();
        }
    }
    
    // Game UI control methods
    private void HideGameUI()
    {
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
            Debug.Log("[HideGameUI] Main game UI hidden for tutorial focus");
        }
        else
        {
            Debug.LogWarning("[HideGameUI] gameUICanvas is null - assign the main UI canvas in inspector");
        }
    }
    
    private void ShowGameUI()
    {
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("[ShowGameUI] Main game UI restored");
        }
        else
        {
            Debug.LogWarning("[ShowGameUI] gameUICanvas is null - assign the main UI canvas in inspector");
        }
    }
    
    // UI highlighting methods
    private void HighlightUIElement(GameObject targetElement)
    {
        if (targetElement == null)
        {
            Debug.LogWarning("[HighlightUIElement] Target element is null");
            return;
        }
        
        // Debug the target element's state
        Debug.Log($"[HighlightUIElement] Target: {targetElement.name}, Active: {targetElement.activeInHierarchy}, Enabled: {targetElement.activeSelf}");
        
        // Check if target has a parent that might be disabled
        Transform parent = targetElement.transform.parent;
        while (parent != null)
        {
            Debug.Log($"[HighlightUIElement] Parent: {parent.name}, Active: {parent.gameObject.activeInHierarchy}");
            parent = parent.parent;
        }
        
        // Clear any existing highlight
        ClearUIHighlight();
        
        // Create a beautiful glow effect programmatically
        CreateGlowEffect(targetElement);
        
        Debug.Log($"[HighlightUIElement] Highlighting {targetElement.name} with procedural glow");
    }
    
    private void CreateGlowEffect(GameObject target)
    {
        // Create container for the effect
        GameObject effectContainer = new GameObject("TutorialHighlight");
        effectContainer.transform.SetParent(target.transform, false);
        currentHighlightEffect = effectContainer;
        
        // Get target's RectTransform
        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect == null) return;
        
        // Add Canvas component to ensure proper sorting
        Canvas highlightCanvas = effectContainer.AddComponent<Canvas>();
        highlightCanvas.overrideSorting = true;
        highlightCanvas.sortingOrder = 32767; // Very high sorting order to ensure visibility
        
        // Add GraphicRaycaster but disable it
        GraphicRaycaster raycaster = effectContainer.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;
        
        // Create outer glow ring
        GameObject outerGlow = CreateGlowRing(effectContainer, "OuterGlow", 1.6f, 0.4f);
        GameObject middleGlow = CreateGlowRing(effectContainer, "MiddleGlow", 1.3f, 0.6f);
        GameObject innerGlow = CreateGlowRing(effectContainer, "InnerGlow", 1.1f, 0.8f);
        
        // Start pulsing animations
        StartCoroutine(PulseGlow(outerGlow.GetComponent<Image>(), 0f));
        StartCoroutine(PulseGlow(middleGlow.GetComponent<Image>(), 0.3f));
        StartCoroutine(PulseGlow(innerGlow.GetComponent<Image>(), 0.6f));
        
        // Add more visible scale pulse to target
        StartCoroutine(PulseScale(target.transform));
        
        Debug.Log($"[CreateGlowEffect] Created highlight with Canvas sortingOrder: {highlightCanvas.sortingOrder}");
    }
    
    private GameObject CreateGlowRing(GameObject parent, string name, float scale, float alphaMultiplier)
    {
        GameObject glow = new GameObject(name);
        glow.transform.SetParent(parent.transform, false);
        
        // Add Image component for the glow
        Image glowImage = glow.AddComponent<Image>();
        
        // Create a simple circle sprite programmatically
        glowImage.sprite = CreateCircleSprite();
        glowImage.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, highlightColor.a * alphaMultiplier);
        glowImage.raycastTarget = false; // Don't block clicks
        
        // Setup RectTransform to scale around target
        RectTransform glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = Vector2.zero;
        glowRect.offsetMax = Vector2.zero;
        glowRect.localScale = Vector3.one * scale;
        
        return glow;
    }
    
    private Sprite CreateCircleSprite()
    {
        // Create a simple circle texture
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.4f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.SmoothStep(1f, 0f, distance / radius);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private IEnumerator PulseGlow(Image glowImage, float timeOffset)
    {
        Color originalColor = glowImage.color;
        
        while (currentHighlightEffect != null && glowImage != null)
        {
            float time = Time.time * highlightPulseSpeed + timeOffset;
            float pulse = (Mathf.Sin(time) + 1f) * 0.5f; // 0 to 1
            float alpha = Mathf.Lerp(highlightMinAlpha, 1f, pulse);
            
            glowImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * alpha);
            
            yield return null;
        }
    }
    
    private IEnumerator PulseScale(Transform target)
    {
        Vector3 originalScale = target.localScale;
        
        // Also add outline effect like old tutorial
        Outline outline = target.GetComponent<Outline>();
        bool addedOutline = false;
        if (outline == null)
        {
            outline = target.gameObject.AddComponent<Outline>();
            addedOutline = true;
        }
        
        outline.enabled = true;
        outline.effectColor = new Color(0f, 1f, 0.4f, 1f); // Bright green like old tutorial
        outline.effectDistance = new Vector2(4, 4);
        
        while (currentHighlightEffect != null && target != null)
        {
            float time = Time.time * highlightPulseSpeed * 0.7f; // Scale pulse speed
            float pulse = (Mathf.Sin(time) + 1f) * 0.5f;
            float scaleMultiplier = Mathf.Lerp(1f, 1.15f, pulse); // More visible 15% scale increase
            
            target.localScale = originalScale * scaleMultiplier;
            
            // Pulse outline thickness too
            if (outline != null)
            {
                float outlineSize = Mathf.Lerp(3f, 8f, pulse);
                outline.effectDistance = new Vector2(outlineSize, outlineSize);
            }
            
            yield return null;
        }
        
        // Restore original scale and remove outline when done
        if (target != null)
        {
            target.localScale = originalScale;
            if (outline != null && addedOutline)
            {
                DestroyImmediate(outline);
            }
            else if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }
    
    private void ClearUIHighlight()
    {
        if (currentHighlightEffect != null)
        {
            Destroy(currentHighlightEffect);
            currentHighlightEffect = null;
            Debug.Log("[ClearUIHighlight] Cleared procedural UI highlight");
        }
    }
    
    private IEnumerator DelayedHighlight(GameObject targetElement, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Double-check that the target is still valid and active
        if (targetElement != null && targetElement.activeInHierarchy)
        {
            HighlightUIElement(targetElement);
        }
        else
        {
            Debug.LogWarning($"[DelayedHighlight] Target element {targetElement?.name ?? "null"} is not active, skipping highlight");
        }
    }
    
    // Public methods for other systems to call
    public void OnShopOpened() => TriggerAction("shop_opened");
    public void OnFarmhouseBuilt() => TriggerAction("farmhouse_built");
    
    // Camera control triggers
    public void OnCameraMovedWASD() => TriggerAction("camera_moved_wasd");
    public void OnCameraRotated() => TriggerAction("camera_rotated");
    public void OnCameraZoomed() => TriggerAction("camera_zoomed");
    public void OnCameraDragged() => TriggerAction("camera_dragged");
    
    public bool IsTutorialActive() => tutorialActive;
    public bool IsWaitingForAction() => waitingForPlayerAction;
    
    // ========== KEY INDICATOR SYSTEM (from old tutorial) ==========
    
    void HandleRequiredInputDetection()
    {
        if (!waitingForPlayerAction || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return;

        var step = tutorialSteps[currentStepIndex];
        
        if (step.requiredInputs == null || step.requiredInputs.Count == 0)
            return;

        foreach (KeyCode key in step.requiredInputs)
        {
            // Handle scroll wheel separately
            if (key == KeyCode.Mouse3) // Scroll up
            {
                if (Input.mouseScrollDelta.y > 0)
                {
                    detectedInputs.Add(key);
                    UpdateKeyIndicatorVisual(key, true);
                }
            }
            else if (key == KeyCode.Mouse4) // Scroll down
            {
                if (Input.mouseScrollDelta.y < 0)
                {
                    detectedInputs.Add(key);
                    UpdateKeyIndicatorVisual(key, true);
                }
            }
            else if (Input.GetKeyDown(key))
            {
                detectedInputs.Add(key);
                UpdateKeyIndicatorVisual(key, true);
            }
        }

        bool shouldAdvance = step.waitForAllInputs ? detectedInputs.Count >= step.requiredInputs.Count : detectedInputs.Count > 0;
        if (shouldAdvance)
            TriggerAction(step.waitForTrigger);
    }

    void ShowKeyIndicators(List<KeyCode> keys)
    {
        Debug.Log($"[ShowKeyIndicators] Called with {keys?.Count ?? 0} keys");
        
        ClearKeyIndicators();
        keyIndicatorMap.Clear();
        
        if (keyIndicatorPrefab == null)
        {
            Debug.LogError("[ShowKeyIndicators] keyIndicatorPrefab is NULL!");
            return;
        }
        
        if (keyIndicatorContainer == null)
        {
            Debug.LogError("[ShowKeyIndicators] keyIndicatorContainer is NULL!");
            return;
        }
        
        if (keys == null || keys.Count == 0)
        {
            Debug.LogWarning("[ShowKeyIndicators] No keys to show");
            return;
        }
        
        Debug.Log($"[ShowKeyIndicators] Creating {keys.Count} key indicators in container: {keyIndicatorContainer.name}");

        foreach (KeyCode key in keys)
        {
            GameObject indicator = Instantiate(keyIndicatorPrefab, keyIndicatorContainer, false);
            indicator.name = $"Key_{key}";
            Vector2 basePos = GetKeyPositionForLayout(key);
            Vector2 overrideOffset = GetPositionOverride(key);
            Vector2 finalPos = basePos + overrideOffset;
            Debug.Log($"[ShowKeyIndicators] Created indicator for key: {key} basePos: {basePos} override: {overrideOffset} final: {finalPos}");
            // For UI elements use RectTransform.anchoredPosition so positioning works correctly in the Canvas
            RectTransform rt = indicator.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = finalPos;
            }
            else
            {
                // Fallback for non-UI objects
                indicator.transform.localPosition = finalPos;
                Debug.LogWarning($"[ShowKeyIndicators] Indicator for {key} has no RectTransform; used localPosition fallback.");
            }
            
            // Check if this is a mouse button that should use an icon instead of text
            Sprite mouseIcon = GetMouseIconSprite(key);
            if (mouseIcon != null)
            {
                // Use the mouse icon sprite
                Image iconImage = indicator.GetComponentInChildren<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = mouseIcon;
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false; // Allow clicks to pass through
                    
                    // Use configurable scale for mouse icons
                    iconImage.transform.localScale = Vector3.one * mouseIconScale;
                }
                
                // Hide the text label for mouse buttons
                TextMeshProUGUI label = indicator.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.gameObject.SetActive(false);
            }
            else
            {
                // Use text label for keyboard keys
                TextMeshProUGUI label = indicator.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = GetKeyDisplayName(key);
                
                // Also disable raycast for keyboard key backgrounds
                Image backgroundImage = indicator.GetComponentInChildren<Image>();
                if (backgroundImage != null)
                    backgroundImage.raycastTarget = false;
            }
            
            keyIndicatorMap[key] = indicator;
            keyIndicators.Add(indicator);
        }
    }

    void UpdateKeyIndicatorVisual(KeyCode key, bool pressed)
    {
        if (!keyIndicatorMap.TryGetValue(key, out GameObject indicator))
            return;

        Image background = indicator.GetComponentInChildren<Image>();
        if (background != null)
            background.color = pressed ? Color.green : Color.white;

        if (pressed && keyPressSound != null)
            effectsAudioSource.PlayOneShot(keyPressSound, 0.7f);

        if (pressed)
            LeanTween.scale(indicator, Vector3.one * 1.2f, 0.2f).setEasePunch();
    }

    Sprite GetMouseIconSprite(KeyCode key)
    {
        return key switch
        {
            KeyCode.Mouse0 => lmbIcon,        // Left Mouse Button
            KeyCode.Mouse1 => rmbIcon,        // Right Mouse Button  
            KeyCode.Mouse2 => mmbIcon,        // Middle Mouse Button
            KeyCode.Mouse3 => mmbUpIcon,      // Mouse Wheel Up
            KeyCode.Mouse4 => mmbDownIcon,    // Mouse Wheel Down
            _ => null // Not a mouse button, return null to use text
        };
    }

    string GetKeyDisplayName(KeyCode key)
    {
        if (key.ToString().StartsWith("Alpha"))
            return key.ToString().Substring(5);
        return key switch
        {
            KeyCode.Mouse0 => "LC",
            KeyCode.Mouse1 => "RC",
            KeyCode.Mouse2 => "MWM",
            KeyCode.Mouse3 => "MWU",
            KeyCode.Mouse4 => "MWD",
            _ => key.ToString()
        };
    }

    void ClearKeyIndicators()
    {
        foreach (var obj in keyIndicators)
            Destroy(obj);
        keyIndicators.Clear();
    }

    Vector2 GetKeyPositionForLayout(KeyCode key)
    {
        float spacing = 80f;
        return key switch
        {
            // Mouse buttons - moved to the left (reduced X values)
            // Note: mouseIconOffset will be added to these so icons don't overlap keyboard keys
            KeyCode.Mouse0 => new Vector2(spacing * 1.2f, spacing * 0.9f) + mouseIconOffset,      // Left mouse
            KeyCode.Mouse1 => new Vector2(spacing * 2.2f, spacing * 0.9f) + mouseIconOffset,      // Right mouse
            // Place middle mouse centered and higher so it sits clearly above Q and E
            KeyCode.Mouse2 => new Vector2(0f, spacing * 1.4f) + mouseIconOffset,               // Middle mouse (higher above)
            KeyCode.Mouse3 => new Vector2(spacing * 0.5f, -spacing) + mouseIconOffset,         // Mouse wheel up
            KeyCode.Mouse4 => new Vector2(spacing * 1.5f, -spacing) + mouseIconOffset,         // Mouse wheel down
            
            // Number keys - moved down (reduced Y values)
            KeyCode.Alpha1 => new Vector2(-spacing * 0.5f, spacing * 1f),  // (was spacing * 2, now 1)
            KeyCode.Alpha2 => new Vector2(spacing * 0.5f, spacing * 1f),   // (was spacing * 2, now 1)
            
            // WASD keys - moved down (reduced Y values)
            KeyCode.W => new Vector2(0, 0),                              // (was spacing, now 0)
            KeyCode.A => new Vector2(-spacing, -spacing),                // (was 0, now -spacing)
            KeyCode.S => new Vector2(0, -spacing),                       // (was 0, now -spacing)
            KeyCode.D => new Vector2(spacing, -spacing),                 // (was 0, now -spacing)
            
            // Q and E keys - spaced further apart to make room for mouse icon
            KeyCode.Q => new Vector2(-spacing * 1.5f, 0),                // Q further left
            KeyCode.E => new Vector2(spacing * 1.5f, 0),                 // E further right
            
            _ => Vector2.zero
        };
    }

    // Returns any per-key override offset (inspector-configured). If none provided, returns Vector2.zero
    Vector2 GetPositionOverride(KeyCode key)
    {
        if (keyPositionOverrides == null || keyPositionOverrides.Count == 0)
            return Vector2.zero;

        for (int i = 0; i < keyPositionOverrides.Count; i++)
        {
            if (keyPositionOverrides[i].key == key)
                return keyPositionOverrides[i].offset;
        }

        return Vector2.zero;
    }
}