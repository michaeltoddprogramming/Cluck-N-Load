using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TutorialCondition
{
    // Basic Setup
    GameStarted,
    ShopOpened,
    FirstStructurePlaced,
    
    // Farm House & Infrastructure
    FarmHousePlaced,
    SiloPlaced,
    CropPlotPlaced,
    
    // Farming Mechanics
    FirstCropPlanted,
    FirstCropHarvested,
    StorageExplained,
    TimeControlsExplained,
    
    // Animals & Production
    ChickenCoopPlaced,
    FirstChickenBought,
    ChickensStartedProducing,
    AnimalProductsReady,
    AnimalProductsCollected,
    
    // Defense Mechanics
    BarracksPlaced,
    FlagPlaced,
    ArmyRecruited,
    NightStarted,
    FirstWolfDefeated,
    
    // Advanced Systems
    SynergyDiscovered,
    SecondDayStarted,
    MoneyEarned,
    LandExpanded,
    
    // Tutorial Complete
    TutorialFinished
}

[System.Serializable]
public class TutorialStep
{
    public string stepId;
    public string title;
    [TextArea(3, 6)]
    public string description;
    public TutorialCondition triggerCondition;
    public TutorialCondition[] prerequisites;
    public bool isCompleted;
    public bool isOptional;
    public float displayDuration = 999f; // Always wait for user input
    public Vector3 worldPosition = Vector3.zero;
    public bool pointToWorldPosition;
    public bool pauseGame;
    public bool highlightUI;
    public string highlightUITag;
    
    // Special button controls
    public bool showStartNightButton = false; // Show the "Start Night" button for this step
    public bool requiresDefensesReady = false; // Button only enabled when defenses are ready
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialTitle;
    [SerializeField] private TextMeshProUGUI tutorialDescription;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button startNightButton; // Special button for starting night during tutorial
    [SerializeField] private Image characterPortrait;
    [SerializeField] private GameObject worldPointer;
    
    [Header("Old Man Character")]
    [SerializeField] private Sprite oldManPortrait;
    [SerializeField] private AudioClip[] oldManVoices;
    [SerializeField] private AudioSource voiceAudioSource;
    
    [Header("Tutorial Configuration")]
    [SerializeField] private bool enableTutorial = true;
    [SerializeField] private bool skipTutorialOnRestart = false;
    [SerializeField] private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    
    [Header("Game References")]
    [SerializeField] private NightManager nightManager;
    [SerializeField] private ShopUIManager shopManager;
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private PauseManager pauseManager; // Reference to the game's pause manager
    
    [Header("Tutorial Polling")]
    [SerializeField] private float conditionCheckInterval = 1.0f; // Check every second
    
    private HashSet<TutorialCondition> completedConditions = new HashSet<TutorialCondition>();
    private Queue<TutorialStep> pendingSteps = new Queue<TutorialStep>();
    private TutorialStep currentStep;
    private bool isTutorialActive = false;
    private bool tutorialCompleted = false;
    private Coroutine currentTutorialCoroutine;
    private Coroutine conditionPollingCoroutine;

    // Tutorial-specific pause management
    private bool wasPausedBeforeTutorial = false;
    
    public event Action<TutorialCondition> OnConditionCompleted;
    public event Action OnTutorialCompleted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Only use DontDestroyOnLoad if this is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeTutorialSteps();
    }

    private void Start()
    {
        if (!enableTutorial)
        {
            gameObject.SetActive(false);
            return;
        }

        if (skipTutorialOnRestart && HasCompletedTutorial())
        {
            gameObject.SetActive(false);
            return;
        }

        SetupUI();
        StartTutorial();
    }

    private void InitializeTutorialSteps()
    {
        tutorialSteps.Clear();
        
        // Step 1: Welcome & Farm Explanation
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "welcome",
            title = "Old Pete",
            description = "Well hello there, young farmer! I'm Old Pete, and I'll be your guide. This here's your new farm, and boy do we have work to do! Those wolves have been causing trouble, but don't you worry - I'll teach you everything you need to know about farming and defending your land!",
            triggerCondition = TutorialCondition.GameStarted,
            prerequisites = new TutorialCondition[] { },
            displayDuration = 999f, // Wait for user input instead of auto-progressing
            pauseGame = true
        });

        // Step 2: Camera Controls (triggers when welcome step completes)
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "camera_controls",
            title = "Look Around Your Farm",
            description = "First things first - let's learn to look around! Use WASD to move the camera, QE to rotate, and your mouse wheel to zoom in and out. Take a moment to explore your land, get familiar with the lay of the land!",
            triggerCondition = TutorialCondition.GameStarted, // Will be manually triggered
            prerequisites = new TutorialCondition[] { },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 3: Opening the Shop (triggers when camera step completes)
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "open_shop",
            title = "Open the Build Shop",
            description = "Now, see that hammer icon on your screen? That's your build shop! Click on it to see what structures you can build. We'll need to construct some buildings to get this farm running properly!",
            triggerCondition = TutorialCondition.GameStarted, // Will be manually triggered  
            prerequisites = new TutorialCondition[] { },
            displayDuration = 5f,
            pauseGame = true,
            highlightUI = true,
            highlightUITag = "ShopButton"
        });

        // Step 4: Farm House First
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Your Farm House",
            description = "Perfect! Now you can see all the buildings you can construct. Every farm needs a proper house! Look for the 'Farm House' in the shop and click on it, then click somewhere on your land to place it. This will be the heart of your operation!",
            triggerCondition = TutorialCondition.ShopOpened,
            prerequisites = new TutorialCondition[] { TutorialCondition.ShopOpened },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 5: Place Crop Plot
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "place_crop_plot",
            title = "Plant Your First Crops",
            description = "A farm ain't a farm without crops! Build a Crop Plot near your house. This is where you'll grow food - both to feed your animals and to store for tough times ahead.",
            triggerCondition = TutorialCondition.FarmHousePlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.FarmHousePlaced },
            displayDuration = 5f,
            pauseGame = true
        });

        // Step 6: Plant First Crop
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "plant_first_crop",
            title = "Plant Your Seeds",
            description = "Excellent! Now click on your crop plot and choose what to plant. I'd recommend starting with Sunflowers - they're hardy and grow well. Remember, you can only plant during the day!",
            triggerCondition = TutorialCondition.CropPlotPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.CropPlotPlaced },
            displayDuration = 5f,
            pauseGame = true
        });

        // Step 7: Build Silo for Storage
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "build_silo",
            title = "Build Storage - The Silo",
            description = "Good work! Now you'll need somewhere to store your harvest. Build a Silo close to your crops - the closer it is, the more efficient your farming will be! This is called 'synergy' - structures work better when they're placed strategically.",
            triggerCondition = TutorialCondition.FirstCropPlanted,
            prerequisites = new TutorialCondition[] { TutorialCondition.FirstCropPlanted },
            displayDuration = 7f,
            pauseGame = true
        });

        // Step 8: Explain Day/Night & Time Controls
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "time_controls",
            title = "Day, Night & Time Controls",
            description = "See those time controls? You can pause time, play normally, or speed things up! Your crops grow over time, and there's a day-night cycle. During the day, you farm and build. At night... well, that's when the wolves come. But don't worry - I've made the days extra long for this tutorial so you have plenty of time to learn!",
            triggerCondition = TutorialCondition.SiloPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.SiloPlaced },
            displayDuration = 8f,
            pauseGame = true
        });

        // Step 9: Chicken Coop for Production (after time controls explanation)
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "build_chicken_coop",
            title = "Start Animal Production",
            description = "Now let's add some livestock! Build a Chicken Coop. Chickens will lay eggs that you can collect and sell for money. Place it near your silo for better efficiency - animals eat less food when they're close to storage!",
            triggerCondition = TutorialCondition.TimeControlsExplained,
            prerequisites = new TutorialCondition[] { TutorialCondition.TimeControlsExplained },
            displayDuration = 7f,
            pauseGame = true
        });

        // Step 10: Buy Chickens
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "buy_chickens",
            title = "Buy Your First Chickens",
            description = "An empty coop won't do you much good! Click on your chicken coop and buy 3-4 chickens to start with. This gives you a good balance of production without eating too much food. You can always buy more later when you have more resources!",
            triggerCondition = TutorialCondition.ChickenCoopPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.ChickenCoopPlaced },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 11: Watch Crops Grow (REMOVED - this was causing overlap with step 7)

        // Step 11: Harvest Your Crops (when they're ready)
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "harvest_first_crops",
            title = "Harvest Your Sunflowers",
            description = "Look at that! Your sunflowers are ready to harvest! Click on the crop plot and harvest your sunflowers. You'll need these to feed your animals!",
            triggerCondition = TutorialCondition.FirstCropHarvested,
            prerequisites = new TutorialCondition[] { TutorialCondition.FirstCropHarvested },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 12: Feed Animals (after harvest AND chickens bought)
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "feed_animals",
            title = "Feed Your Chickens",
            description = "Now that you have sunflowers, your chickens are hungry! Click on the chicken coop and feed them with your harvested sunflowers. Well-fed animals will start producing eggs!",
            triggerCondition = TutorialCondition.GameStarted, // Will be manually triggered
            prerequisites = new TutorialCondition[] { TutorialCondition.FirstCropHarvested, TutorialCondition.FirstChickenBought },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 13: Watch Production Start
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "watch_production",
            title = "Production Starting",
            description = "Excellent! Your chickens are now fed and producing. Take your time - when you're ready to continue, they'll quickly produce eggs for the tutorial! No rush, just click Next when you want to proceed.",
            triggerCondition = TutorialCondition.ChickensStartedProducing,
            prerequisites = new TutorialCondition[] { TutorialCondition.ChickensStartedProducing },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 14: Collect Products
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "collect_products",
            title = "Collect Your Eggs",
            description = "Perfect! Your chickens have finished producing eggs. Click on the chicken coop and collect the eggs to earn money. This is how you'll fund your expansion and defenses!",
            triggerCondition = TutorialCondition.AnimalProductsReady,
            prerequisites = new TutorialCondition[] { TutorialCondition.AnimalProductsReady },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 15: Build Barracks for Defense
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "build_barracks",
            title = "Prepare Your Defenses",
            description = "Now for the important part - defense! Those wolves I mentioned? They attack at night. Build a Barracks near your chicken coop. The barracks will recruit chickens to form an army that protects your farm!",
            triggerCondition = TutorialCondition.AnimalProductsCollected,
            prerequisites = new TutorialCondition[] { TutorialCondition.AnimalProductsCollected },
            displayDuration = 8f,
            pauseGame = true
        });

        // Step 16: Place Defense Flag
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "place_flag",
            title = "Set Your Defense Position",
            description = "Great! Now click on your barracks and place a flag. This flag shows your army where to gather and defend. Place it in a strategic position where your army can protect your important buildings!",
            triggerCondition = TutorialCondition.BarracksPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.BarracksPlaced },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 17: Recruit Army
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "recruit_army",
            title = "Recruit Your Army",
            description = "Perfect! Now recruit 2-3 soldier chickens from your barracks. They'll cost money and will take chickens from your coop, but they're essential for defense. Start small - 2-3 soldiers should be enough for your first night. The closer your barracks to your chicken coop, the cheaper recruitment is!",
            triggerCondition = TutorialCondition.FlagPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.FlagPlaced },
            displayDuration = 8f,
            pauseGame = true
        });

        // Step 18: First Night - Special Tutorial Night Start
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "first_night",
            title = "Ready for Your First Night?",
            description = "Perfect! You've recruited your army and set up your defenses. When you're completely ready to face the night, click the 'Start Night' button below. The button will only be enabled when your defenses are properly set up. Take your time - you have full control!",
            triggerCondition = TutorialCondition.ArmyRecruited,
            prerequisites = new TutorialCondition[] { TutorialCondition.ArmyRecruited },
            displayDuration = 999f, // Wait for user to click Start Night button
            pauseGame = true,
            showStartNightButton = true, // Show the special Start Night button
            requiresDefensesReady = true // Only enable when defenses are ready
        });

        // Step 19: Night Defense
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "night_defense",
            title = "Watch Your Army Fight!",
            description = "Look at them go! Your soldier chickens are defending your farm. Watch how they move to the flag position and fight off the wolves. If all your structures get destroyed, it's game over, so keep building up your defenses!",
            triggerCondition = TutorialCondition.NightStarted,
            prerequisites = new TutorialCondition[] { TutorialCondition.NightStarted },
            displayDuration = 6f,
            pauseGame = true
        });

        // Step 20: Show Synergies
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "synergy_explanation",
            title = "Understanding Synergies",
            description = "Here's a pro tip! Buildings work better when placed near each other:\n• Silos near crops = better storage\n• Animals near silos = eat less food\n• Barracks near animals = cheaper recruitment\nPlan your layout carefully!",
            triggerCondition = TutorialCondition.FirstWolfDefeated,
            prerequisites = new TutorialCondition[] { TutorialCondition.FirstWolfDefeated },
            displayDuration = 10f,
            pauseGame = true
        });

        // Step 21: Complete Tutorial
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "tutorial_complete",
            title = "You're Ready to Farm!",
            description = "Congratulations! You've learned the basics of Cluck N Load. Keep expanding your farm, try different animals, experiment with layouts, and survive as many nights as you can. Remember: Farm during the day, fight at night, and always plan ahead!\n\nGood luck, farmer!",
            triggerCondition = TutorialCondition.SecondDayStarted,
            prerequisites = new TutorialCondition[] { TutorialCondition.SecondDayStarted },
            displayDuration = 12f,
            pauseGame = true
        });
    }

    private void SetupUI()
    {
        // Auto-find UI components if not assigned
        if (tutorialPanel == null)
        {
            GameObject tutorialUI = GameObject.Find("TutorialUI");
            if (tutorialUI != null)
            {
                tutorialPanel = tutorialUI;
                Debug.Log("Auto-found TutorialUI panel");
                
                // Auto-assign other components
                var uiScript = tutorialUI.GetComponent<TutorialUIPrefab>();
                if (uiScript != null)
                {
                    if (tutorialDescription == null) tutorialDescription = uiScript.dialogueText;
                    if (tutorialTitle == null) tutorialTitle = uiScript.characterNameText;
                    if (nextButton == null) nextButton = uiScript.nextButton;
                    if (skipButton == null) skipButton = uiScript.skipButton;
                    if (characterPortrait == null) characterPortrait = uiScript.characterPortrait;
                    
                    Debug.Log("Auto-assigned UI components from TutorialUIPrefab");
                }
            }
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextTutorialStep);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipTutorial);
        }

        if (startNightButton != null)
        {
            startNightButton.onClick.AddListener(OnStartNightClicked);
            startNightButton.gameObject.SetActive(false); // Hidden by default
        }

        if (characterPortrait != null && oldManPortrait != null)
        {
            characterPortrait.sprite = oldManPortrait;
        }
    }

    private void StartTutorial()
    {
        if (tutorialCompleted) return;

        // Start the backup condition polling system
        if (conditionPollingCoroutine == null)
        {
            conditionPollingCoroutine = StartCoroutine(PollForMissedConditions());
        }

        OnConditionMet(TutorialCondition.GameStarted);
    }

    public void OnConditionMet(TutorialCondition condition)
    {
        if (!enableTutorial || tutorialCompleted) return;

        if (completedConditions.Contains(condition)) 
        {
            Debug.Log($"Tutorial condition {condition} already completed - ignoring duplicate");
            return;
        }

        Debug.Log($"Tutorial condition met: {condition}");
        completedConditions.Add(condition);
        OnConditionCompleted?.Invoke(condition);

        // STRICT SEQUENTIAL FLOW: Only process the NEXT step in sequence
        TutorialStep nextStep = GetNextIncompleteStep();
        
        if (nextStep != null && nextStep.triggerCondition == condition && CanTriggerStepStrictly(nextStep))
        {
            Debug.Log($"Tutorial: Triggering next sequential step {nextStep.stepId}");
            pendingSteps.Clear(); // Clear any old pending steps
            pendingSteps.Enqueue(nextStep);
            ProcessPendingSteps();
        }
        else if (nextStep != null)
        {
            Debug.Log($"Tutorial: Condition {condition} met, but next step is {nextStep.stepId} (waiting for {nextStep.triggerCondition})");
        }
        else
        {
            Debug.Log($"Tutorial: No more incomplete steps to process");
        }
    }

    /// <summary>
    /// Get the next incomplete step in the tutorial sequence
    /// </summary>
    private TutorialStep GetNextIncompleteStep()
    {
        foreach (var step in tutorialSteps)
        {
            if (!step.isCompleted)
            {
                return step;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Strictly check if a step can be triggered - MUCH more restrictive than before
    /// </summary>
    private bool CanTriggerStepStrictly(TutorialStep step)
    {
        // Must not be already completed
        if (step.isCompleted)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Already completed");
            return false;
        }
        
        // Must be the EXACT next step in sequence (no skipping allowed)
        int stepIndex = tutorialSteps.FindIndex(s => s.stepId == step.stepId);
        if (stepIndex > 0)
        {
            // Check that ALL previous steps are completed
            for (int i = 0; i < stepIndex; i++)
            {
                if (!tutorialSteps[i].isCompleted)
                {
                    Debug.Log($"Tutorial step {step.stepId} blocked: Previous step {tutorialSteps[i].stepId} not completed yet (index {i})");
                    return false;
                }
            }
        }
        
        // Must have all prerequisites
        if (!ArePrerequisitesStrictlyMet(step))
            return false;
        
        // Cannot trigger if another step is currently active
        if (isTutorialActive && currentStep != null)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({currentStep.stepId}) is currently active");
            return false;
        }
        
        Debug.Log($"Tutorial step {step.stepId} STRICT validation passed");
        return true;
    }

    private void ProcessPendingSteps()
    {
        if (isTutorialActive || pendingSteps.Count == 0) return;

        var step = pendingSteps.Dequeue();
        ShowTutorialStep(step);
    }

    private void ShowTutorialStep(TutorialStep step)
    {
        if (currentTutorialCoroutine != null)
        {
            StopCoroutine(currentTutorialCoroutine);
        }

        currentStep = step;
        currentTutorialCoroutine = StartCoroutine(DisplayTutorialStep(step));
    }

    private IEnumerator DisplayTutorialStep(TutorialStep step)
    {
        isTutorialActive = true;

        // Pause game if required
        if (step.pauseGame)
        {
            PauseForTutorial();
        }

        // Setup UI
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            
            // Make sure CanvasGroup is visible
            var canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (tutorialTitle != null)
                tutorialTitle.text = step.title;

            if (tutorialDescription != null)
                tutorialDescription.text = step.description;

            // Play voice if available
            PlayOldManVoice();

            // Handle world pointer
            if (step.pointToWorldPosition && worldPointer != null)
            {
                worldPointer.SetActive(true);
                worldPointer.transform.position = step.worldPosition;
            }
            else if (worldPointer != null)
            {
                worldPointer.SetActive(false);
            }

            // Handle UI highlighting
            if (step.highlightUI && !string.IsNullOrEmpty(step.highlightUITag))
            {
                HighlightUIElement(step.highlightUITag);
            }

            // Handle special Start Night button
            if (step.showStartNightButton && startNightButton != null)
            {
                startNightButton.gameObject.SetActive(true);
                
                // Start a coroutine to continuously update button state
                StartCoroutine(UpdateStartNightButtonState(step));
            }
            else if (startNightButton != null)
            {
                startNightButton.gameObject.SetActive(false);
            }
        }

        // Wait for manual progression ONLY (no auto-advance)
        while (currentStep == step && isTutorialActive)
        {
            yield return null;
        }

        // Complete the step
        CompleteCurrentStep();
    }

    private void CompleteCurrentStep()
    {
        if (currentStep != null)
        {
            currentStep.isCompleted = true;
            
            // Trigger special conditions when certain steps complete
            if (currentStep.stepId == "time_controls")
            {
                OnConditionMet(TutorialCondition.TimeControlsExplained);
            }
            
            // SEQUENTIAL TUTORIAL FLOW: Manually trigger next steps to avoid conflicts
            if (currentStep.stepId == "welcome")
            {
                // After welcome, show camera controls
                Debug.Log("Tutorial: Welcome completed, showing camera controls");
                StartCoroutine(ShowNextStepAfterDelay("camera_controls", 0.5f));
            }
            else if (currentStep.stepId == "camera_controls")
            {
                // After camera controls, show shop instruction
                Debug.Log("Tutorial: Camera controls completed, showing shop instruction");
                StartCoroutine(ShowNextStepAfterDelay("open_shop", 0.5f));
            }
            else if (currentStep.stepId == "open_shop")
            {
                // After shop instruction, wait for them to actually open the shop
                Debug.Log("Tutorial: Shop instruction completed, waiting for shop to be opened");
                // ShopOpened condition will trigger the farmhouse step
            }
            
            // TUTORIAL TIMING FIX: Only grow crops when we're ready for harvest step
            if (currentStep.stepId == "buy_chickens")
            {
                // User just bought chickens, now trigger crop growth so they'll be ready for harvest step
                var conditionTracker = FindFirstObjectByType<TutorialConditionTracker>();
                if (conditionTracker != null)
                {
                    Debug.Log("Tutorial: Triggering crop growth after buying chickens");
                    conditionTracker.TriggerCropGrowthForTutorial();
                }
            }
            
            // TUTORIAL FIX: Manually trigger animal production conditions when user progresses
            if (currentStep.stepId == "feed_animals")
            {
                // User just fed animals, trigger chickens started producing
                Debug.Log("Tutorial: Manually triggering ChickensStartedProducing after feeding animals");
                OnConditionMet(TutorialCondition.ChickensStartedProducing);
            }
            else if (currentStep.stepId == "watch_production")
            {
                // User watched production, now make products ready
                Debug.Log("Tutorial: Manually triggering AnimalProductsReady after watching production");
                OnConditionMet(TutorialCondition.AnimalProductsReady);
            }
            else if (currentStep.stepId == "harvest_first_crops")
            {
                // User just harvested crops, now show feed animals step
                Debug.Log("Tutorial: Harvest completed, showing feed animals step");
                StartCoroutine(ShowNextStepAfterDelay("feed_animals", 0.5f));
            }
            
            // Check if this was the final step
            if (currentStep.stepId == "tutorial_complete")
            {
                CompleteTutorial();
            }
        }

        // Hide UI
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (worldPointer != null)
        {
            worldPointer.SetActive(false);
        }

        // Hide Start Night button
        if (startNightButton != null)
        {
            startNightButton.gameObject.SetActive(false);
        }

        // Unpause game
        ResumeFromTutorial();

        isTutorialActive = false;
        currentStep = null;

        // Process any pending steps
        ProcessPendingSteps();
    }

    public void NextTutorialStep()
    {
        Debug.Log("NextTutorialStep called!");
        if (currentStep != null)
        {
            Debug.Log($"Completing current step: {currentStep.stepId}");
            CompleteCurrentStep();
        }
        else
        {
            Debug.LogWarning("NextTutorialStep called but currentStep is null!");
        }
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    /// <summary>
    /// Called when the special "Start Night" button is clicked during tutorial
    /// </summary>
    public void OnStartNightClicked()
    {
        Debug.Log("Tutorial: Start Night button clicked!");
        
        // Verify defenses are ready
        var tutorialTracker = FindFirstObjectByType<TutorialConditionTracker>();
        if (tutorialTracker != null && tutorialTracker.AreDefensesReady())
        {
            // Start the night through the night manager using the new manual method
            if (nightManager != null)
            {
                Debug.Log("Tutorial: Starting night manually via tutorial button");
                nightManager.ForceStartNight(); // Use the new manual night start method
                
                // Trigger the night started condition
                OnConditionMet(TutorialCondition.NightStarted);
                
                // Complete the current tutorial step
                NextTutorialStep();
            }
            else
            {
                Debug.LogError("Tutorial: NightManager not found!");
            }
        }
        else
        {
            Debug.LogWarning("Tutorial: Defenses not ready - button should be disabled!");
        }
    }

    /// <summary>
    /// Coroutine to continuously update the Start Night button enabled state
    /// </summary>
    private IEnumerator UpdateStartNightButtonState(TutorialStep step)
    {
        while (currentStep == step && startNightButton != null && startNightButton.gameObject.activeInHierarchy)
        {
            if (step.requiresDefensesReady)
            {
                var tutorialTracker = FindFirstObjectByType<TutorialConditionTracker>();
                bool defensesReady = tutorialTracker != null && tutorialTracker.AreDefensesReady();
                
                startNightButton.interactable = defensesReady;
                
                // Update button text based on state
                var buttonText = startNightButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = defensesReady ? "Start Night!" : "Defenses Not Ready";
                    buttonText.color = defensesReady ? Color.white : Color.gray;
                }
            }
            else
            {
                startNightButton.interactable = true;
            }
            
            yield return new WaitForSeconds(0.1f); // Check 10 times per second
        }
    }

    private void CompleteTutorial()
    {
        tutorialCompleted = true;
        
        // Stop the polling coroutine
        if (conditionPollingCoroutine != null)
        {
            StopCoroutine(conditionPollingCoroutine);
            conditionPollingCoroutine = null;
        }
        
        // Mark all steps as completed
        foreach (var step in tutorialSteps)
        {
            step.isCompleted = true;
        }

        // Save tutorial completion
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // Hide UI
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (worldPointer != null)
        {
            worldPointer.SetActive(false);
        }

        // Hide Start Night button
        if (startNightButton != null)
        {
            startNightButton.gameObject.SetActive(false);
        }

        // Resume game using our pause management
        ResumeFromTutorial();

        OnTutorialCompleted?.Invoke();
        
        Debug.Log("Tutorial completed!");
    }

    private bool HasCompletedTutorial()
    {
        return PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
    }

    private void PlayOldManVoice()
    {
        if (voiceAudioSource != null && oldManVoices != null && oldManVoices.Length > 0)
        {
            var randomClip = oldManVoices[UnityEngine.Random.Range(0, oldManVoices.Length)];
            voiceAudioSource.clip = randomClip;
            voiceAudioSource.Play();
        }
    }

    private void HighlightUIElement(string tag)
    {
        // This would highlight UI elements with specific tags
        // Implementation depends on your UI structure
        try
        {
            GameObject uiElement = GameObject.FindGameObjectWithTag(tag);
            if (uiElement != null)
            {
                // Add highlighting effect
                // Could use outline, glow, or animation
                Debug.Log($"Highlighting UI element with tag: {tag}");
            }
            else
            {
                Debug.LogWarning($"No GameObject found with tag: {tag}");
            }
        }
        catch (UnityException ex)
        {
            Debug.LogWarning($"Tag '{tag}' is not defined. Skipping highlight. Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Force the tutorial UI to be visible - for debugging
    /// </summary>
    public void ForceShowTutorialUI()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            
            // Force alpha to 1
            var canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                Debug.Log("Forced tutorial UI alpha to 1");
            }
            
            // Also check parent canvas
            Transform parent = tutorialPanel.transform.parent;
            while (parent != null)
            {
                var parentCanvasGroup = parent.GetComponent<CanvasGroup>();
                if (parentCanvasGroup != null)
                {
                    parentCanvasGroup.alpha = 1f;
                    Debug.Log($"Set parent {parent.name} alpha to 1");
                }
                parent = parent.parent;
            }
            
            Debug.Log("Tutorial UI forced visible");
        }
        else
        {
            Debug.LogError("Tutorial panel is null!");
        }
    }
    
    /// <summary>
    /// Debug method to test tutorial display
    /// </summary>
    [ContextMenu("Test Tutorial Display")]
    public void TestTutorialDisplay()
    {
        if (tutorialSteps.Count > 0)
        {
            ForceShowTutorialUI();
            DisplayTutorialStep(tutorialSteps[0]);
        }
    }
    
    /// <summary>
    /// Debug method to check UI connections
    /// </summary>
    [ContextMenu("Debug UI Connections")]
    public void DebugUIConnections()
    {
        Debug.Log($"Tutorial Panel: {(tutorialPanel != null ? tutorialPanel.name : "NULL")}");
        Debug.Log($"Tutorial Title: {(tutorialTitle != null ? tutorialTitle.name : "NULL")}");
        Debug.Log($"Tutorial Description: {(tutorialDescription != null ? tutorialDescription.name : "NULL")}");
        Debug.Log($"Next Button: {(nextButton != null ? nextButton.name : "NULL")}");
        Debug.Log($"Skip Button: {(skipButton != null ? skipButton.name : "NULL")}");
        Debug.Log($"Character Portrait: {(characterPortrait != null ? characterPortrait.name : "NULL")}");
    }

    /// <summary>
    /// Debug method to force the next logical tutorial step (for recovery from stuck states)
    /// </summary>
    [ContextMenu("Force Next Tutorial Step")]
    public void ForceNextTutorialStep()
    {
        if (tutorialCompleted)
        {
            Debug.Log("Tutorial already completed");
            return;
        }
        
        // Find the next incomplete step
        foreach (var step in tutorialSteps)
        {
            if (!step.isCompleted)
            {
                Debug.Log($"Forcing tutorial step: {step.stepId} ({step.triggerCondition})");
                
                // Mark the trigger condition as met
                OnConditionMet(step.triggerCondition);
                break;
            }
        }
    }
    
    /// <summary>
    /// Debug method to show current tutorial state
    /// </summary>
    [ContextMenu("Show Tutorial State")]
    public void ShowTutorialState()
    {
        Debug.Log("=== TUTORIAL STATE ===");
        Debug.Log($"Tutorial Active: {isTutorialActive}");
        Debug.Log($"Tutorial Completed: {tutorialCompleted}");
        Debug.Log($"Current Step: {(currentStep != null ? currentStep.stepId : "None")}");
        Debug.Log($"Pending Steps: {pendingSteps.Count}");
        Debug.Log($"Completed Conditions: {string.Join(", ", completedConditions)}");
        
        Debug.Log("Next incomplete steps:");
        int count = 0;
        foreach (var step in tutorialSteps)
        {
            if (!step.isCompleted)
            {
                Debug.Log($"  {step.stepId} (waiting for: {step.triggerCondition})");
                count++;
                if (count >= 3) break; // Show max 3 upcoming steps
            }
        }
        Debug.Log("=====================");
    }

    /// <summary>
    /// Debug method to manually trigger animal production conditions
    /// </summary>
    [ContextMenu("Fix Animal Production Tutorial")]
    public void FixAnimalProductionTutorial()
    {
        Debug.Log("=== FIXING ANIMAL PRODUCTION TUTORIAL ===");
        
        // Check what conditions are missing and try to fix them
        if (!completedConditions.Contains(TutorialCondition.FirstChickenBought))
        {
            Debug.Log("Manually triggering FirstChickenBought");
            OnConditionMet(TutorialCondition.FirstChickenBought);
        }
        
        if (!completedConditions.Contains(TutorialCondition.ChickensStartedProducing))
        {
            Debug.Log("Manually triggering ChickensStartedProducing");
            OnConditionMet(TutorialCondition.ChickensStartedProducing);
        }
        
        if (!completedConditions.Contains(TutorialCondition.AnimalProductsReady))
        {
            Debug.Log("Manually triggering AnimalProductsReady");
            OnConditionMet(TutorialCondition.AnimalProductsReady);
        }
        
        if (!completedConditions.Contains(TutorialCondition.AnimalProductsCollected))
        {
            Debug.Log("Manually triggering AnimalProductsCollected");
            OnConditionMet(TutorialCondition.AnimalProductsCollected);
        }
        
        Debug.Log("Animal production tutorial fix completed!");
    }

    /// <summary>
    /// Debug method to manually trigger defense setup conditions
    /// </summary>
    [ContextMenu("Fix Defense Tutorial")]
    public void FixDefenseTutorial()
    {
        Debug.Log("=== FIXING DEFENSE TUTORIAL ===");
        
        // Check what conditions are missing and try to fix them
        if (!completedConditions.Contains(TutorialCondition.BarracksPlaced))
        {
            Debug.Log("Manually triggering BarracksPlaced");
            OnConditionMet(TutorialCondition.BarracksPlaced);
        }
        
        if (!completedConditions.Contains(TutorialCondition.FlagPlaced))
        {
            Debug.Log("Manually triggering FlagPlaced");
            OnConditionMet(TutorialCondition.FlagPlaced);
        }
        
        if (!completedConditions.Contains(TutorialCondition.ArmyRecruited))
        {
            Debug.Log("Manually triggering ArmyRecruited");
            OnConditionMet(TutorialCondition.ArmyRecruited);
        }
        
        Debug.Log("Defense tutorial fix completed!");
    }
    
    // Public methods for external systems to call
    public bool IsTutorialActive()
    {
        return isTutorialActive;
    }

    public bool IsTutorialCompleted()
    {
        return tutorialCompleted;
    }

    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialCompleted");
        tutorialCompleted = false;
        completedConditions.Clear();
        
        foreach (var step in tutorialSteps)
        {
            step.isCompleted = false;
        }
    }

    /// <summary>
    /// Get the list of tutorial steps for external systems to check completion status
    /// </summary>
    public List<TutorialStep> GetTutorialSteps()
    {
        return tutorialSteps;
    }

    /// <summary>
    /// Pause the game for tutorial steps - integrates with PauseManager if available
    /// </summary>
    private void PauseForTutorial()
    {
        // Store the current pause state
        if (pauseManager != null)
        {
            wasPausedBeforeTutorial = Time.timeScale == 0f;
            pauseManager.pauseGame();
        }
        else
        {
            // Fallback to direct Time.timeScale manipulation
            wasPausedBeforeTutorial = Time.timeScale == 0f;
            Time.timeScale = 0f;
        }
        
        Debug.Log("Tutorial: Game paused for tutorial step");
    }
    
    /// <summary>
    /// Resume game after tutorial step - only if it wasn't paused before
    /// </summary>
    private void ResumeFromTutorial()
    {
        // Only resume if the game wasn't already paused before the tutorial
        if (!wasPausedBeforeTutorial)
        {
            if (pauseManager != null)
            {
                pauseManager.playGame();
            }
            else
            {
                // Fallback to direct Time.timeScale manipulation
                Time.timeScale = 1f;
            }
            
            Debug.Log("Tutorial: Game resumed after tutorial step");
        }
        else
        {
            Debug.Log("Tutorial: Game remains paused (was paused before tutorial step)");
        }
    }
    
    /// <summary>
    /// Check if all prerequisites are strictly met before allowing progression
    /// </summary>
    private bool ArePrerequisitesStrictlyMet(TutorialStep step)
    {
        foreach (var prereq in step.prerequisites)
        {
            if (!completedConditions.Contains(prereq))
            {
                Debug.LogWarning($"Tutorial step {step.stepId} blocked: Missing prerequisite {prereq}");
                return false;
            }
        }
        
        if (step.prerequisites.Length > 0)
        {
            Debug.Log($"Tutorial step {step.stepId}: All prerequisites met ({string.Join(", ", step.prerequisites)})");
        }
        
        return true;
    }
    
    /// <summary>
    /// Enhanced condition checking that prevents multiple steps from triggering simultaneously
    /// </summary>
    private bool CanTriggerStep(TutorialStep step, TutorialCondition condition)
    {
        // Must match trigger condition
        if (step.triggerCondition != condition)
            return false;
            
        // Must not be already completed
        if (step.isCompleted)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Already completed");
            return false;
        }
            
        // Must have all prerequisites
        if (!ArePrerequisitesStrictlyMet(step))
            return false;
            
        // CRITICAL: Prevent multiple steps from triggering on the same condition
        // Only allow if no other step with the same trigger condition is already queued or active
        foreach (var pendingStep in pendingSteps)
        {
            if (pendingStep.triggerCondition == condition && pendingStep.stepId != step.stepId)
            {
                Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({pendingStep.stepId}) with same trigger condition is already queued");
                return false;
            }
        }
        
        // Check if a step with the same trigger is currently active
        if (currentStep != null && currentStep.triggerCondition == condition && currentStep.stepId != step.stepId)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({currentStep.stepId}) with same trigger condition is currently active");
            return false;
        }
        
        // ADDITIONAL CHECK: For sequential tutorial flow, ensure we're not skipping ahead
        // Find the index of this step and make sure previous steps are completed
        int stepIndex = tutorialSteps.FindIndex(s => s.stepId == step.stepId);
        if (stepIndex > 0)
        {
            for (int i = 0; i < stepIndex; i++)
            {
                if (!tutorialSteps[i].isCompleted)
                {
                    Debug.Log($"Tutorial step {step.stepId} blocked: Previous step {tutorialSteps[i].stepId} not completed yet");
                    return false;
                }
            }
        }
        
        Debug.Log($"Tutorial step {step.stepId} validation passed - can be triggered");
        return true;
    }
    
    /// <summary>
    /// Backup polling system to catch missed conditions and prevent tutorial from getting stuck
    /// </summary>
    private IEnumerator PollForMissedConditions()
    {
        while (!tutorialCompleted)
        {
            yield return new WaitForSeconds(conditionCheckInterval);
            
            if (!enableTutorial || tutorialCompleted) 
                continue;
                
            CheckForMissedConditions();
        }
    }
    
    /// <summary>
    /// Check for conditions that might have been missed by the event system
    /// </summary>
    private void CheckForMissedConditions()
    {
        try
        {
            // Shop opened check
            if (!completedConditions.Contains(TutorialCondition.ShopOpened))
            {
                if (shopManager != null && shopManager.gameObject.activeInHierarchy)
                {
                    // Check if shop UI is visible/active
                    var shopCanvas = shopManager.GetComponent<Canvas>();
                    if (shopCanvas != null && shopCanvas.enabled)
                    {
                        Debug.Log("Tutorial Polling: Detected missed ShopOpened condition");
                        OnConditionMet(TutorialCondition.ShopOpened);
                    }
                }
            }
            
            // DISABLED: Farm house placed check - now handled by BuildController -> TutorialConditionTracker
            /*
            if (!completedConditions.Contains(TutorialCondition.FarmHousePlaced))
            {
                var farmHouses = FindObjectsByType<Structure>(FindObjectsSortMode.None);
                foreach (var structure in farmHouses)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (structure.gameObject.name != "BuildGhost" && 
                        (structure.name.ToLower().Contains("farmhouse") || structure.name.ToLower().Contains("farm house")))
                    {
                        Debug.Log("Tutorial Polling: Detected missed FarmHousePlaced condition");
                        OnConditionMet(TutorialCondition.FarmHousePlaced);
                        break;
                    }
                }
            }
            */
            
            // DISABLED: Crop plot placed check - now handled by BuildController -> TutorialConditionTracker
            // The polling system was causing premature detection of BuildGhost objects
            /*
            if (!completedConditions.Contains(TutorialCondition.CropPlotPlaced))
            {
                var cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
                bool hasRealCropPlot = false;
                foreach (var crop in cropStructures)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (crop.gameObject.name != "BuildGhost")
                    {
                        hasRealCropPlot = true;
                        break;
                    }
                }
                
                if (hasRealCropPlot)
                {
                    Debug.Log("Tutorial Polling: Detected missed CropPlotPlaced condition");
                    OnConditionMet(TutorialCondition.CropPlotPlaced);
                }
            }
            */
            
            // First crop planted check
            if (!completedConditions.Contains(TutorialCondition.FirstCropPlanted))
            {
                var cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
                foreach (var crop in cropStructures)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (crop.gameObject.name != "BuildGhost" && crop.CurrentCropType != CropStructure.CropType.None)
                    {
                        Debug.Log("Tutorial Polling: Detected missed FirstCropPlanted condition");
                        OnConditionMet(TutorialCondition.FirstCropPlanted);
                        break;
                    }
                }
            }
            
            // DISABLED: Silo placed check - now handled by BuildController -> TutorialConditionTracker
            /*
            if (!completedConditions.Contains(TutorialCondition.SiloPlaced))
            {
                var structures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
                foreach (var structure in structures)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (structure.gameObject.name != "BuildGhost" && structure.name.ToLower().Contains("silo"))
                    {
                        Debug.Log("Tutorial Polling: Detected missed SiloPlaced condition");
                        OnConditionMet(TutorialCondition.SiloPlaced);
                        break;
                    }
                }
            }
            */
            
            // DISABLED: Chicken coop placed check - now handled by BuildController -> TutorialConditionTracker
            /*
            if (!completedConditions.Contains(TutorialCondition.ChickenCoopPlaced))
            {
                var animalStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
                foreach (var animal in animalStructures)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (animal.gameObject.name != "BuildGhost" && 
                        (animal.name.ToLower().Contains("chicken") || animal.name.ToLower().Contains("coop")))
                    {
                        Debug.Log("Tutorial Polling: Detected missed ChickenCoopPlaced condition");
                        OnConditionMet(TutorialCondition.ChickenCoopPlaced);
                        break;
                    }
                }
            }
            */
            
            // First chicken bought check
            if (!completedConditions.Contains(TutorialCondition.FirstChickenBought))
            {
                var animalStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
                foreach (var animal in animalStructures)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (animal.gameObject.name != "BuildGhost" && animal.AnimalCount > 0)
                    {
                        Debug.Log("Tutorial Polling: Detected missed FirstChickenBought condition");
                        OnConditionMet(TutorialCondition.FirstChickenBought);
                        break;
                    }
                }
            }
            
            // Chickens started producing check
            if (!completedConditions.Contains(TutorialCondition.ChickensStartedProducing))
            {
                var animalStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
                foreach (var animal in animalStructures)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (animal.gameObject.name != "BuildGhost" && animal.AnimalCount > 0 && animal.IsProducing)
                    {
                        Debug.Log("Tutorial Polling: Detected missed ChickensStartedProducing condition");
                        OnConditionMet(TutorialCondition.ChickensStartedProducing);
                        break;
                    }
                }
            }
            
            // Animal products ready check
            if (!completedConditions.Contains(TutorialCondition.AnimalProductsReady))
            {
                var animalStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
                foreach (var animal in animalStructures)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (animal.gameObject.name != "BuildGhost" && animal.ProductReady)
                    {
                        Debug.Log("Tutorial Polling: Detected missed AnimalProductsReady condition");
                        OnConditionMet(TutorialCondition.AnimalProductsReady);
                        break;
                    }
                }
            }
            
            // Animal products collected check
            if (!completedConditions.Contains(TutorialCondition.AnimalProductsCollected))
            {
                // This condition should be triggered by the animal structure when products are collected
                // We'll add a fallback check to see if we have money and the animals don't have products anymore
                if (moneyManager != null && moneyManager.GetCurrentMoney() > 0)
                {
                    var animalStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
                    bool hasAnimalsWithoutProducts = false;
                    foreach (var animal in animalStructures)
                    {
                        if (animal.gameObject.name != "BuildGhost" && animal.AnimalCount > 0 && !animal.ProductReady)
                        {
                            hasAnimalsWithoutProducts = true;
                            break;
                        }
                    }
                    
                    if (hasAnimalsWithoutProducts && completedConditions.Contains(TutorialCondition.AnimalProductsReady))
                    {
                        Debug.Log("Tutorial Polling: Detected missed AnimalProductsCollected condition (inferred from money and empty animals)");
                        OnConditionMet(TutorialCondition.AnimalProductsCollected);
                    }
                }
            }
            
            // DISABLED: Crop harvested check - this should only trigger when player actually harvests
            // The polling was incorrectly triggering when crops became ready rather than when harvested
            /*
            if (!completedConditions.Contains(TutorialCondition.FirstCropHarvested))
            {
                var cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
                foreach (var crop in cropStructures)
                {
                    if (crop.gameObject.name != "BuildGhost" && crop.CropReady)
                    {
                        Debug.Log("Tutorial Polling: Detected crop ready for FirstCropHarvested condition");
                        OnConditionMet(TutorialCondition.FirstCropHarvested);
                        break;
                    }
                }
            }
            */
            
            // DISABLED: Barracks placed check - now handled by BuildController -> TutorialConditionTracker
            /*
            if (!completedConditions.Contains(TutorialCondition.BarracksPlaced))
            {
                var barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
                bool hasRealBarracks = false;
                foreach (var barrack in barracks)
                {
                    // Skip BuildGhost objects (they're just previews)
                    if (barrack.gameObject.name != "BuildGhost")
                    {
                        hasRealBarracks = true;
                        break;
                    }
                }
                
                if (hasRealBarracks)
                {
                    Debug.Log("Tutorial Polling: Detected missed BarracksPlaced condition");
                    OnConditionMet(TutorialCondition.BarracksPlaced);
                }
            }
            */
            
            // Flag placed check
            if (!completedConditions.Contains(TutorialCondition.FlagPlaced))
            {
                var barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
                foreach (var barrack in barracks)
                {
                    if (barrack.GetFlagPosition != Vector3.zero)
                    {
                        Debug.Log("Tutorial Polling: Detected missed FlagPlaced condition");
                        OnConditionMet(TutorialCondition.FlagPlaced);
                        break;
                    }
                }
            }
            
            // Army recruited check
            if (!completedConditions.Contains(TutorialCondition.ArmyRecruited))
            {
                var barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
                foreach (var barrack in barracks)
                {
                    if (barrack.ArmyAnimalCount > 0)
                    {
                        Debug.Log("Tutorial Polling: Detected missed ArmyRecruited condition");
                        OnConditionMet(TutorialCondition.ArmyRecruited);
                        break;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Tutorial polling error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Public method for external systems to manually check if they should trigger tutorial conditions
    /// Call this when placing structures, changing game state, etc.
    /// </summary>
    public void CheckTutorialConditions()
    {
        if (!enableTutorial || tutorialCompleted) return;
        
        CheckForMissedConditions();
    }
    
    /// <summary>
    /// Specific method for structure placement events
    /// </summary>
    public void OnStructurePlaced(GameObject structure)
    {
        if (!enableTutorial || tutorialCompleted) return;
        
        string structureName = structure.name.ToLower();
        
        if (structureName.Contains("farmhouse") || structureName.Contains("farm house"))
        {
            OnConditionMet(TutorialCondition.FarmHousePlaced);
        }
        else if (structureName.Contains("silo"))
        {
            OnConditionMet(TutorialCondition.SiloPlaced);
        }
        else if (structureName.Contains("crop") || structure.GetComponent<CropStructure>() != null)
        {
            OnConditionMet(TutorialCondition.CropPlotPlaced);
        }
        else if (structureName.Contains("chicken") || structureName.Contains("coop") || structure.GetComponent<AnimalStructure>() != null)
        {
            OnConditionMet(TutorialCondition.ChickenCoopPlaced);
        }
        else if (structureName.Contains("barracks") || structure.GetComponent<BarracksStructure>() != null)
        {
            OnConditionMet(TutorialCondition.BarracksPlaced);
        }
        
        Debug.Log($"Tutorial: Structure placed - {structureName}");
    }
    
    /// <summary>
    /// Helper method to show a specific tutorial step after a delay
    /// Used for sequential tutorial flow
    /// </summary>
    private IEnumerator ShowNextStepAfterDelay(string stepId, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Find the step by ID and show it directly
        foreach (var step in tutorialSteps)
        {
            if (step.stepId == stepId && !step.isCompleted)
            {
                Debug.Log($"Tutorial: Manually showing step {stepId}");
                pendingSteps.Enqueue(step);
                ProcessPendingSteps();
                break;
            }
        }
    }
}
