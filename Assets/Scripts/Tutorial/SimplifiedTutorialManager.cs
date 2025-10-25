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
        public float panelAlpha = 1.0f;             // Background panel opacity (0-1, fade the background)
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
        
        // Shop introduction - normal position, full opacity
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "open_shop",
            title = "Let's Build!",
            message = "Click the shop button to start building structures!",
            peteContext = PeteContext.UIHelper,
            peteEmotion = PeteEmotion.Excited,
            waitForAction = true,
            waitForTrigger = "shop_opened"
        });
        
        // Build farmhouse - fade background, move content down for world focus
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Your Home",
            message = "Every farm needs a farmhouse! Build yours first.",
            peteContext = PeteContext.CornerBuddy,
            peteEmotion = PeteEmotion.Worried,
            waitForAction = true,
            waitForTrigger = "farmhouse_built",
            movePanelDown = true,    // Move content to bottom
            panelAlpha = 0.3f        // Fade background to 30% so world is visible
        });
        
        // Tutorial complete - back to normal
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "complete",
            title = "Great Job!",
            message = "You're ready to farm! Good luck out there!",
            peteContext = PeteContext.UIHelper,
            peteEmotion = PeteEmotion.Celebrating
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
        
        tutorialActive = true;
        currentStepIndex = -1;
        NextStep();
    }
    
    public void NextStep()
    {
        if (!tutorialActive) return;
        
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
                nextStepButton.interactable = false;
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
        
        // Stop mumbling when typing is done
        if (mumbleCoroutine != null)
        {
            StopCoroutine(mumbleCoroutine);
            if (mumbleAudioSource != null && mumbleAudioSource.isPlaying)
            {
                mumbleAudioSource.Stop();
            }
            Debug.Log("[TypeText] Stopped Pete's mumbling");
        }
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
        Debug.Log($"[UpdatePanelForStep] Called for step: {step.stepId}, panelAlpha: {step.panelAlpha}, movePanelDown: {step.movePanelDown}");
        
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
        Vector2 targetPosition = step.movePanelDown 
            ? new Vector2(originalContentPosition.x, originalContentPosition.y - 280f)
            : originalContentPosition;
        
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
            
            if (nextStepButton != null)
            {
                nextStepButton.interactable = true;
            }
            
            // Pete celebrates
            if (usePete3D && pete3DGuide != null)
            {
                pete3DGuide.OnStepComplete();
            }
        }
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
        
        // Reset panel to original state
        ResetPanel();
        
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
    
    // Public methods for other systems to call
    public void OnShopOpened() => TriggerAction("shop_opened");
    public void OnFarmhouseBuilt() => TriggerAction("farmhouse_built");
    public void OnCameraMoved() => TriggerAction("camera_moved");
    
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