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
    
    // Animals & Production
    ChickenCoopPlaced,
    FirstChickenBought,
    ChickensStartedProducing,
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
    public float displayDuration = 5f;
    public Vector3 worldPosition = Vector3.zero;
    public bool pointToWorldPosition;
    public bool pauseGame;
    public bool highlightUI;
    public string highlightUITag;
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
    
    private HashSet<TutorialCondition> completedConditions = new HashSet<TutorialCondition>();
    private Queue<TutorialStep> pendingSteps = new Queue<TutorialStep>();
    private TutorialStep currentStep;
    private bool isTutorialActive = false;
    private bool tutorialCompleted = false;
    private Coroutine currentTutorialCoroutine;

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

        // Step 2: Camera Controls
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "camera_controls",
            title = "Look Around Your Farm",
            description = "First things first - let's learn to look around! Use WASD to move the camera, QE to rotate, and your mouse wheel to zoom in and out. Take a moment to explore your land, get familiar with the lay of the land!",
            triggerCondition = TutorialCondition.GameStarted,
            prerequisites = new TutorialCondition[] { },
            displayDuration = 6f,
            pauseGame = false
        });

        // Step 3: Opening the Shop
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "open_shop",
            title = "Open the Build Shop",
            description = "Now, see that hammer icon on your screen? That's your build shop! Click on it to see what structures you can build. We'll need to construct some buildings to get this farm running properly!",
            triggerCondition = TutorialCondition.GameStarted,
            prerequisites = new TutorialCondition[] { },
            displayDuration = 5f,
            pauseGame = false,
            highlightUI = true,
            highlightUITag = "ShopButton"
        });

        // Step 4: Farm House First
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Your Farm House",
            description = "Every farm needs a proper house! Find the Farm House in the shop and place it somewhere central on your land. This will be the heart of your operation. Click on it in the shop, then click on the ground to place it!",
            triggerCondition = TutorialCondition.ShopOpened,
            prerequisites = new TutorialCondition[] { TutorialCondition.ShopOpened },
            displayDuration = 6f,
            pauseGame = false
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
            pauseGame = false
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
            pauseGame = false
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
            pauseGame = false
        });

        // Step 8: Explain Day/Night & Time Controls
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "time_controls",
            title = "Day, Night & Time Controls",
            description = "See those time controls? You can pause time, play normally, or speed things up! Your crops grow over time, and there's a day-night cycle. During the day, you farm and build. At night... well, that's when the wolves come. But don't worry about that yet!",
            triggerCondition = TutorialCondition.SiloPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.SiloPlaced },
            displayDuration = 8f,
            pauseGame = true
        });

        // Step 9: Chicken Coop for Production
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "build_chicken_coop",
            title = "Start Animal Production",
            description = "Now let's add some livestock! Build a Chicken Coop. Chickens will lay eggs that you can collect and sell for money. Place it near your silo for better efficiency - animals eat less food when they're close to storage!",
            triggerCondition = TutorialCondition.SiloPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.SiloPlaced },
            displayDuration = 7f,
            pauseGame = false
        });

        // Step 10: Buy Chickens
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "buy_chickens",
            title = "Buy Your First Chickens",
            description = "An empty coop won't do you much good! Click on your chicken coop and buy some chickens. Start with just a few - you can always buy more later when you have more money.",
            triggerCondition = TutorialCondition.ChickenCoopPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.ChickenCoopPlaced },
            displayDuration = 5f,
            pauseGame = false
        });

        // Step 11: Harvest & Feed Cycle
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "harvest_crops",
            title = "Harvest Your First Crops",
            description = "Look at that! Your crops are ready to harvest. Click on the crop plot and harvest them. The food will go to your silo automatically. You'll need this food to feed your animals!",
            triggerCondition = TutorialCondition.FirstChickenBought,
            prerequisites = new TutorialCondition[] { TutorialCondition.FirstChickenBought },
            displayDuration = 6f,
            pauseGame = false
        });

        // Step 12: Feed Animals
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "feed_animals",
            title = "Feed Your Animals",
            description = "Your chickens are getting hungry! Click on the chicken coop and feed them. Well-fed animals produce more goods. Make sure to keep them fed regularly!",
            triggerCondition = TutorialCondition.FirstCropHarvested,
            prerequisites = new TutorialCondition[] { TutorialCondition.FirstCropHarvested },
            displayDuration = 5f,
            pauseGame = false
        });

        // Step 13: Collect Products
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "collect_products",
            title = "Collect Animal Products",
            description = "Excellent! Your chickens have started producing eggs. Click on the chicken coop and collect the eggs to earn money. This is how you'll fund your expansion and defenses!",
            triggerCondition = TutorialCondition.ChickensStartedProducing,
            prerequisites = new TutorialCondition[] { TutorialCondition.ChickensStartedProducing },
            displayDuration = 5f,
            pauseGame = false
        });

        // Step 14: Build Barracks for Defense
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "build_barracks",
            title = "Prepare Your Defenses",
            description = "Now for the important part - defense! Those wolves I mentioned? They attack at night. Build a Barracks near your chicken coop. The barracks will recruit chickens to form an army that protects your farm!",
            triggerCondition = TutorialCondition.AnimalProductsCollected,
            prerequisites = new TutorialCondition[] { TutorialCondition.AnimalProductsCollected },
            displayDuration = 8f,
            pauseGame = false
        });

        // Step 15: Place Defense Flag
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "place_flag",
            title = "Set Your Defense Position",
            description = "Great! Now click on your barracks and place a flag. This flag shows your army where to gather and defend. Place it in a strategic position where your army can protect your important buildings!",
            triggerCondition = TutorialCondition.BarracksPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.BarracksPlaced },
            displayDuration = 6f,
            pauseGame = false
        });

        // Step 16: Recruit Army
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "recruit_army",
            title = "Recruit Your Army",
            description = "Perfect! Now recruit some soldier chickens from your barracks. They'll cost money and will take chickens from your coop, but they're essential for defense. The closer your barracks to your chicken coop, the cheaper recruitment is!",
            triggerCondition = TutorialCondition.FlagPlaced,
            prerequisites = new TutorialCondition[] { TutorialCondition.FlagPlaced },
            displayDuration = 7f,
            pauseGame = false
        });

        // Step 17: First Night
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "first_night",
            title = "Survive Your First Night",
            description = "You're ready! Now let's start the night and see how you do. Click the 'Start Night' button when you're prepared. During night, your army animals will activate and defend against wolves. You can't build or farm at night, so make sure you're ready!",
            triggerCondition = TutorialCondition.ArmyRecruited,
            prerequisites = new TutorialCondition[] { TutorialCondition.ArmyRecruited },
            displayDuration = 8f,
            pauseGame = true
        });

        // Step 18: Night Defense
        tutorialSteps.Add(new TutorialStep
        {
            stepId = "night_defense",
            title = "Watch Your Army Fight!",
            description = "Look at them go! Your soldier chickens are defending your farm. Watch how they move to the flag position and fight off the wolves. If all your structures get destroyed, it's game over, so keep building up your defenses!",
            triggerCondition = TutorialCondition.NightStarted,
            prerequisites = new TutorialCondition[] { TutorialCondition.NightStarted },
            displayDuration = 6f,
            pauseGame = false
        });

        // Step 19: Show Synergies
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

        // Step 20: Complete Tutorial
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

        if (characterPortrait != null && oldManPortrait != null)
        {
            characterPortrait.sprite = oldManPortrait;
        }
    }

    private void StartTutorial()
    {
        if (tutorialCompleted) return;

        OnConditionMet(TutorialCondition.GameStarted);
    }

    public void OnConditionMet(TutorialCondition condition)
    {
        if (!enableTutorial || tutorialCompleted) return;

        if (completedConditions.Contains(condition)) return;

        completedConditions.Add(condition);
        OnConditionCompleted?.Invoke(condition);

        // Check for tutorial steps that can now be triggered
        foreach (var step in tutorialSteps)
        {
            if (step.isCompleted) continue;
            
            if (step.triggerCondition == condition)
            {
                // Check if all prerequisites are met
                bool canTrigger = true;
                foreach (var prereq in step.prerequisites)
                {
                    if (!completedConditions.Contains(prereq))
                    {
                        canTrigger = false;
                        break;
                    }
                }

                if (canTrigger)
                {
                    pendingSteps.Enqueue(step);
                }
            }
        }

        // Process pending steps
        ProcessPendingSteps();
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
            Time.timeScale = 0f;
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
        }

        // Wait for step duration or manual progression
        float timer = 0f;
        while (timer < step.displayDuration && currentStep == step)
        {
            timer += (step.pauseGame ? Time.unscaledDeltaTime : Time.deltaTime);
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

        // Unpause game
        Time.timeScale = 1f;

        isTutorialActive = false;
        currentStep = null;

        // Process any pending steps
        ProcessPendingSteps();
    }

    public void NextTutorialStep()
    {
        if (currentStep != null)
        {
            CompleteCurrentStep();
        }
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        tutorialCompleted = true;
        
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

        // Unpause game
        Time.timeScale = 1f;

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
}
