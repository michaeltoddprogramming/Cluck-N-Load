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
    [SerializeField] private float conditionCheckInterval = 3.0f; // Check every second
    
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

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "welcome",
        title = "Old Pete's Farm Fiasco!",
        description = "Howdy, greenhorn! I'm Old Pete, and this farm's your ticket to glory—if you can keep those darn wolves at bay!",
        triggerCondition = TutorialCondition.GameStarted,
        prerequisites = new TutorialCondition[] { },
        displayDuration = 5f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "camera_controls",
        title = "Scope Out Your Land!",
        description = "Use WASD to scoot, QE to spin, and mouse wheel to zoom. Get a feel for your wolf-battlin' battlefield!",
        triggerCondition = TutorialCondition.GameStarted,
        prerequisites = new TutorialCondition[] { },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "open_shop",
        title = "Hit the Shop, Partner!",
        description = "Click that hammer icon to check out buildings. Time to build your empire, one coop at a time!",
        triggerCondition = TutorialCondition.GameStarted,
        prerequisites = new TutorialCondition[] { },
        displayDuration = 4f,
        pauseGame = false,
        highlightUI = true,
        highlightUITag = "ShopButton"
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "build_farmhouse",
        title = "Raise a Farmhouse!",
        description = "Pick 'Farm House' from the shop and plop it down. It’s your HQ for cluckin’ greatness!",
        triggerCondition = TutorialCondition.ShopOpened,
        prerequisites = new TutorialCondition[] { TutorialCondition.ShopOpened },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "place_crop_plot",
        title = "Plant Some Chow!",
        description = "Build a Crop Plot near your farmhouse. Gotta grow food for your feathered army!",
        triggerCondition = TutorialCondition.FarmHousePlaced,
        prerequisites = new TutorialCondition[] { TutorialCondition.FarmHousePlaced },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "plant_first_crop",
        title = "Sow Some Sunflowers!",
        description = "Click your crop plot and plant Sunflowers. They’re the best grub for your future cluckers!",
        triggerCondition = TutorialCondition.CropPlotPlaced,
        prerequisites = new TutorialCondition[] { TutorialCondition.CropPlotPlaced },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "harvest_first_crops",
        title = "Reap Those Sunflowers!",
        description = "Your sunflowers are ripe! Click the plot to harvest 'em for your hungry chickens!",
        triggerCondition = TutorialCondition.FirstCropHarvested,
        prerequisites = new TutorialCondition[] { TutorialCondition.FirstCropPlanted },
        displayDuration = 999f,
        pauseGame = true,
        pointToWorldPosition = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "build_silo",
        title = "Stash Your Goods!",
        description = "Build a Silo near your crops to store those sunflowers. Close silos mean happy chickens!",
        triggerCondition = TutorialCondition.FirstCropHarvested,
        prerequisites = new TutorialCondition[] { TutorialCondition.FirstCropHarvested },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "time_controls",
        title = "Master Time Itself!",
        description = "Use time controls to pause or speed up. Day’s for farmin’, night’s for fightin’ wolves!",
        triggerCondition = TutorialCondition.SiloPlaced,
        prerequisites = new TutorialCondition[] { TutorialCondition.SiloPlaced },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "build_chicken_coop",
        title = "Cluckin’ Coop Time!",
        description = "Build a Chicken Coop near your silo. Those egg-layin’ heroes are your cash cows!",
        triggerCondition = TutorialCondition.TimeControlsExplained,
        prerequisites = new TutorialCondition[] { TutorialCondition.SiloPlaced, TutorialCondition.TimeControlsExplained },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "feed_animals",
        title = "Feed the Cluckers!",
        description = "Click your coop and toss in those sunflowers. Fat chickens lay eggs faster!",
        triggerCondition = TutorialCondition.FirstChickenBought,
        prerequisites = new TutorialCondition[] { TutorialCondition.FirstCropHarvested, TutorialCondition.FirstChickenBought },
        displayDuration = 4f,
        pauseGame = true,
        pointToWorldPosition = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "watch_production",
        title = "Eggs Are Cookin’!",
        description = "Your chickens are workin’ hard. Hit Next when you’re ready to grab those eggs!",
        triggerCondition = TutorialCondition.ChickensStartedProducing,
        prerequisites = new TutorialCondition[] { TutorialCondition.FirstChickenBought, TutorialCondition.ChickensStartedProducing },
        displayDuration = 4f,
        pauseGame = true,
        pointToWorldPosition = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "collect_products",
        title = "Snag Those Eggs!",
        description = "Eggs are ready! Click the coop to collect ‘em and rake in the gold!",
        triggerCondition = TutorialCondition.AnimalProductsReady,
        prerequisites = new TutorialCondition[] { TutorialCondition.ChickensStartedProducing, TutorialCondition.AnimalProductsReady },
        displayDuration = 4f,
        pauseGame = true,
        pointToWorldPosition = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "build_barracks",
        title = "Build Your War Coop!",
        description = "Wolves are comin’! Build a Barracks near your coop to train chickens for battle!",
        triggerCondition = TutorialCondition.AnimalProductsCollected,
        prerequisites = new TutorialCondition[] { TutorialCondition.AnimalProductsCollected },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "place_flag",
        title = "Raise the Battle Flag!",
        description = "Click your barracks and plant a flag. It’s where your chicken army will rally!",
        triggerCondition = TutorialCondition.BarracksPlaced,
        prerequisites = new TutorialCondition[] { TutorialCondition.BarracksPlaced },
        displayDuration = 4f,
        pauseGame = true,
        pointToWorldPosition = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "recruit_army",
        title = "Cluck ‘n’ Load!",
        description = "Recruit 2-3 soldier chickens from your barracks. They’ll peck those wolves to bits!",
        triggerCondition = TutorialCondition.FlagPlaced,
        prerequisites = new TutorialCondition[] { TutorialCondition.FlagPlaced },
        displayDuration = 4f,
        pauseGame = true,
        pointToWorldPosition = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "first_night",
        title = "Ready for Wolf Whackin’?",
        description = "Your army’s set! Hit 'Start Night' to unleash your chickens on those pesky wolves!",
        triggerCondition = TutorialCondition.ArmyRecruited,
        prerequisites = new TutorialCondition[] { TutorialCondition.ArmyRecruited },
        displayDuration = 999f,
        pauseGame = true,
        showStartNightButton = true,
        requiresDefensesReady = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "night_defense",
        title = "Chickens vs. Wolves!",
        description = "Watch your feathered warriors clobber those wolves! Protect your farm or it’s game over!",
        triggerCondition = TutorialCondition.NightStarted,
        prerequisites = new TutorialCondition[] { TutorialCondition.NightStarted },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "synergy_explanation",
        title = "Farm Smarts 101!",
        description = "Place silos near crops, coops near silos, and barracks near coops for big boosts!",
        triggerCondition = TutorialCondition.FirstWolfDefeated,
        prerequisites = new TutorialCondition[] { TutorialCondition.FirstWolfDefeated },
        displayDuration = 4f,
        pauseGame = true
    });

    tutorialSteps.Add(new TutorialStep
    {
        stepId = "tutorial_complete",
        title = "You’re a Farm Legend!",
        description = "You’ve tamed the farm and crushed the wolves! Keep growin’, cluckin’, and fightin’!",
        triggerCondition = TutorialCondition.SecondDayStarted,
        prerequisites = new TutorialCondition[] { TutorialCondition.SecondDayStarted },
        displayDuration = 5f,
        pauseGame = true
    });
}

    private void SetupUI()
    {
        if (tutorialPanel == null)
        {
            GameObject tutorialUI = GameObject.Find("TutorialUI");
            if (tutorialUI != null)
            {
                tutorialPanel = tutorialUI;
                Debug.Log("Auto-found TutorialUI panel");
                
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
            startNightButton.gameObject.SetActive(false);
        }

        if (characterPortrait != null && oldManPortrait != null)
        {
            characterPortrait.sprite = oldManPortrait;
        }
    }

    private void StartTutorial()
    {
        if (tutorialCompleted) return;

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

        TutorialStep nextStep = GetNextIncompleteStep();
        
        if (nextStep != null && nextStep.triggerCondition == condition && CanTriggerStepStrictly(nextStep))
        {
            Debug.Log($"Tutorial: Triggering next sequential step {nextStep.stepId}");
            pendingSteps.Clear();
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

    private bool CanTriggerStepStrictly(TutorialStep step)
    {
        if (step.isCompleted)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Already completed");
            return false;
        }

        int stepIndex = tutorialSteps.FindIndex(s => s.stepId == step.stepId);
        if (stepIndex > 0)
        {
            for (int i = 0; i < stepIndex; i++)
            {
                if (!tutorialSteps[i].isCompleted)
                {
                    Debug.Log($"Tutorial step {step.stepId} blocked: Previous step {tutorialSteps[i].stepId} not completed yet (index {i})");
                    return false;
                }
            }
        }

        if (!ArePrerequisitesStrictlyMet(step))
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Prerequisites not met");
            return false;
        }

        if (isTutorialActive && currentStep != null)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({currentStep.stepId}) is currently active");
            return false;
        }

        Debug.Log($"Tutorial step {step.stepId} can be triggered");
        return true;
    }

    private void ProcessPendingSteps()
    {
        if (isTutorialActive || pendingSteps.Count == 0)
        {
            Debug.Log($"ProcessPendingSteps: Cannot process - Tutorial active: {isTutorialActive}, Pending steps: {pendingSteps.Count}");
            return;
        }

        var step = pendingSteps.Dequeue();
        Debug.Log($"Processing pending step: {step.stepId} (Trigger: {step.triggerCondition})");
        ShowTutorialStep(step);
    }

    private void ShowTutorialStep(TutorialStep step)
    {
        if (currentTutorialCoroutine != null)
        {
            StopCoroutine(currentTutorialCoroutine);
        }

        if (step.stepId == "open_shop")
        {
            Debug.Log("Tutorial: Preparing for open_shop step - forcing shop closed and enabling shop button");
            if (shopManager != null)
            {
                shopManager.CloseShop();
                shopManager.ResetShopState();
                shopManager.enableShop();
            }
            if (NightManager.Instance != null && NightManager.Instance.shopManager != null)
            {
                NightManager.Instance.shopManager.CloseShop();
                NightManager.Instance.shopManager.ResetShopState();
                if (NightManager.Instance.IsDay)
                    NightManager.Instance.shopManager.enableShop();
            }
        }
        // In TutorialManager.cs, modify ShowTutorialStep for harvest_first_crops
else if (step.stepId == "harvest_first_crops")
{
    CropStructure[] crops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
    foreach (var crop in crops)
    {
        if (crop.CropReady)
        {
            step.worldPosition = crop.transform.position + Vector3.up * 3f;
            Debug.Log($"Tutorial: Highlighting crop plot {crop.name} at {step.worldPosition} for harvest");
            StartCoroutine(PulseEffect(crop.gameObject));
            if (worldPointer != null)
            {
                worldPointer.transform.position = step.worldPosition + Vector3.up * 1f; // Adjusted for visibility
                worldPointer.SetActive(true);
            }
            break;
        }
    }
}
        else
        {
            if (shopManager != null)
            {
                shopManager.CloseShop();
            }
            if (NightManager.Instance != null && NightManager.Instance.shopManager != null)
            {
                NightManager.Instance.shopManager.CloseShop();
                NightManager.Instance.shopManager.ResetShopState();
                if (NightManager.Instance.IsDay)
                    NightManager.Instance.shopManager.enableShop();
            }
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            var uiScript = tutorialPanel.GetComponent<TutorialUIPrefab>();
            if (uiScript != null)
            {
                uiScript.AnimatePanelIn();
            }
        }

        currentStep = step;
        currentTutorialCoroutine = StartCoroutine(DisplayTutorialStep(step));
    }

    private IEnumerator DisplayTutorialStep(TutorialStep step)
    {
        isTutorialActive = true;

        if (step.pauseGame)
        {
            PauseForTutorial();
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            
            var canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (tutorialTitle != null)
                tutorialTitle.text = step.title;

            var uiScript = tutorialPanel != null ? tutorialPanel.GetComponent<TutorialUIPrefab>() : null;
            if (uiScript != null)
                uiScript.PlayTypingWithMumble(step.description);
            else if (tutorialDescription != null)
                tutorialDescription.text = step.description;

            PlayOldManVoice();

            if (step.pointToWorldPosition && worldPointer != null)
            {
                worldPointer.SetActive(true);
                worldPointer.transform.position = step.worldPosition;
            }
            else if (worldPointer != null)
            {
                worldPointer.SetActive(false);
            }

            if (step.highlightUI && !string.IsNullOrEmpty(step.highlightUITag))
            {
                HighlightUIElement(step.highlightUITag);
            }

            if (step.showStartNightButton && startNightButton != null)
            {
                startNightButton.gameObject.SetActive(true);
                StartCoroutine(UpdateStartNightButtonState(step));
            }
            else if (startNightButton != null)
            {
                startNightButton.gameObject.SetActive(false);
            }
        }

        while (currentStep == step && isTutorialActive)
        {
            yield return null;
        }

        CompleteCurrentStep();
    }

    private void CompleteCurrentStep()
    {
        if (currentStep != null)
        {
            currentStep.isCompleted = true;

            if (currentStep.stepId == "welcome")
            {
                Debug.Log("Tutorial: Welcome completed, showing camera controls");
                StartCoroutine(ShowNextStepAfterDelay("camera_controls", 0.5f));
            }
            else if (currentStep.stepId == "camera_controls")
            {
                Debug.Log("Tutorial: Camera controls completed, showing shop instruction");
                StartCoroutine(ShowNextStepAfterDelay("open_shop", 0.5f));
            }
            else if (currentStep.stepId == "open_shop")
            {
                Debug.Log("Tutorial: Shop instruction completed, waiting for shop to be opened");
                StartCoroutine(DelayNextStepAfterShopOpened());
            }

            if (currentStep.stepId == "tutorial_complete")
            {
                CompleteTutorial();
            }
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (worldPointer != null)
        {
            worldPointer.SetActive(false);
        }

        if (startNightButton != null)
        {
            startNightButton.gameObject.SetActive(false);
        }

        ResumeFromTutorial();

        isTutorialActive = false;
        currentStep = null;

        ProcessPendingSteps();
    }

    private IEnumerator DelayNextStepAfterShopOpened()
    {
        yield return new WaitForSecondsRealtime(1.5f);
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

    public void OnStartNightClicked()
    {
        Debug.Log("Tutorial: Start Night button clicked!");
        
        var tutorialTracker = FindFirstObjectByType<TutorialConditionTracker>();
        if (tutorialTracker != null && tutorialTracker.AreDefensesReady())
        {
            if (nightManager != null)
            {
                Debug.Log("Tutorial: Starting night manually via tutorial button");
                nightManager.ForceStartNight();
                OnConditionMet(TutorialCondition.NightStarted);
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

    private IEnumerator UpdateStartNightButtonState(TutorialStep step)
    {
        while (currentStep == step && startNightButton != null && startNightButton.gameObject.activeInHierarchy)
        {
            if (step.requiresDefensesReady)
            {
                var tutorialTracker = FindFirstObjectByType<TutorialConditionTracker>();
                bool defensesReady = tutorialTracker != null && tutorialTracker.AreDefensesReady();
                
                startNightButton.interactable = defensesReady;
                
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
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void CompleteTutorial()
    {
        tutorialCompleted = true;
        
        if (conditionPollingCoroutine != null)
        {
            StopCoroutine(conditionPollingCoroutine);
            conditionPollingCoroutine = null;
        }
        
        foreach (var step in tutorialSteps)
        {
            step.isCompleted = true;
        }

        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (worldPointer != null)
        {
            worldPointer.SetActive(false);
        }

        if (startNightButton != null)
        {
            startNightButton.gameObject.SetActive(false);
        }

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
    try
    {
        GameObject uiElement = GameObject.FindGameObjectWithTag(tag);
        if (uiElement != null)
        {
            Debug.Log($"Tutorial: Highlighting UI element {uiElement.name} with tag: {tag}");
            StartCoroutine(PulseUIEffect(uiElement));
        }
        else
        {
            Debug.LogWarning($"Tutorial: No GameObject found with tag: {tag}");
        }
    }
    catch (UnityException ex)
    {
        Debug.LogWarning($"Tutorial: Tag '{tag}' is not defined. Skipping highlight. Error: {ex.Message}");
    }
}

    public void ForceShowTutorialUI()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            
            var canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                Debug.Log("Forced tutorial UI alpha to 1");
            }
            
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

    [ContextMenu("Test Tutorial Display")]
    public void TestTutorialDisplay()
    {
        if (tutorialSteps.Count > 0)
        {
            ForceShowTutorialUI();
            DisplayTutorialStep(tutorialSteps[0]);
        }
    }

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

    [ContextMenu("Force Next Tutorial Step")]
    public void ForceNextTutorialStep()
    {
        if (tutorialCompleted)
        {
            Debug.Log("Tutorial already completed");
            return;
        }
        
        foreach (var step in tutorialSteps)
        {
            if (!step.isCompleted)
            {
                Debug.Log($"Forcing tutorial step: {step.stepId} ({step.triggerCondition})");
                OnConditionMet(step.triggerCondition);
                break;
            }
        }
    }

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
                if (count >= 3) break;
            }
        }
        Debug.Log("=====================");
    }

    [ContextMenu("Fix Animal Production Tutorial")]
    public void FixAnimalProductionTutorial()
    {
        Debug.Log("=== FIXING ANIMAL PRODUCTION TUTORIAL ===");
        
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

    [ContextMenu("Fix Defense Tutorial")]
    public void FixDefenseTutorial()
    {
        Debug.Log("=== FIXING DEFENSE TUTORIAL ===");
        
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

    public List<TutorialStep> GetTutorialSteps()
    {
        return tutorialSteps;
    }

    private void PauseForTutorial()
    {
        if (pauseManager != null)
        {
            wasPausedBeforeTutorial = Time.timeScale == 0f;
            pauseManager.pauseGame();
        }
        else
        {
            wasPausedBeforeTutorial = Time.timeScale == 0f;
            Time.timeScale = 0f;
        }
        
        Debug.Log("Tutorial: Game paused for tutorial step");
    }

    private void ResumeFromTutorial()
    {
        if (!wasPausedBeforeTutorial)
        {
            if (pauseManager != null)
            {
                pauseManager.playGame();
            }
            else
            {
                Time.timeScale = 1f;
            }
            
            Debug.Log("Tutorial: Game resumed after tutorial step");
        }
        else
        {
            Debug.Log("Tutorial: Game remains paused (was paused before tutorial step)");
        }
    }

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

    private bool CanTriggerStep(TutorialStep step, TutorialCondition condition)
    {
        if (step.triggerCondition != condition)
            return false;
            
        if (step.isCompleted)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Already completed");
            return false;
        }
            
        if (!ArePrerequisitesStrictlyMet(step))
            return false;
            
        foreach (var pendingStep in pendingSteps)
        {
            if (pendingStep.triggerCondition == condition && pendingStep.stepId != step.stepId)
            {
                Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({pendingStep.stepId}) with same trigger condition is already queued");
                return false;
            }
        }
        
        if (currentStep != null && currentStep.triggerCondition == condition && currentStep.stepId != step.stepId)
        {
            Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({currentStep.stepId}) with same trigger condition is currently active");
            return false;
        }
        
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

    private void CheckForMissedConditions()
    {
        try
        {
            if (!completedConditions.Contains(TutorialCondition.ShopOpened))
            {
                if (shopManager != null && shopManager.gameObject.activeInHierarchy)
                {
                    var shopCanvas = shopManager.GetComponent<Canvas>();
                    if (shopCanvas != null && shopCanvas.enabled)
                    {
                        Debug.Log("Tutorial Polling: Detected missed ShopOpened condition");
                        OnConditionMet(TutorialCondition.ShopOpened);
                    }
                }
            }
            
            if (!completedConditions.Contains(TutorialCondition.FirstCropPlanted))
            {
                var cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
                foreach (var crop in cropStructures)
                {
                    if (crop.gameObject.name != "BuildGhost" && crop.CurrentCropType != CropStructure.CropType.None)
                    {
                        Debug.Log("Tutorial Polling: Detected missed FirstCropPlanted condition");
                        OnConditionMet(TutorialCondition.FirstCropPlanted);
                        break;
                    }
                }
            }
            
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

    public void CheckTutorialConditions()
    {
        if (!enableTutorial || tutorialCompleted) return;
        
        CheckForMissedConditions();
    }

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

    private IEnumerator ShowNextStepAfterDelay(string stepId, float delay)
    {
        yield return new WaitForSeconds(delay);
        
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

    // In TutorialManager.cs, modify PulseUIEffect
    private IEnumerator PulseUIEffect(GameObject uiElement)
    {
        if (uiElement == null) yield break;
        Vector3 originalScale = uiElement.transform.localScale;
        while (uiElement != null && currentStep != null && currentStep.highlightUI)
        {
            uiElement.transform.localScale = originalScale * (1f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f)); // Increased from 0.05f to 0.1f
            yield return null;
        }
        if (uiElement != null) uiElement.transform.localScale = originalScale;
    }

    // In TutorialManager.cs, modify PulseEffect
private IEnumerator PulseEffect(GameObject obj)
{
    if (obj == null) yield break;
    Vector3 originalScale = obj.transform.localScale;
    while (obj != null && currentStep != null && currentStep.pointToWorldPosition)
    {
        obj.transform.localScale = originalScale * (1f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f)); // Increased from 0.1f to 0.2f
        yield return null;
    }
    if (obj != null) obj.transform.localScale = originalScale;
}

    public string GetCurrentStepId()
    {
        return currentStep != null ? currentStep.stepId : "None";
    }
}