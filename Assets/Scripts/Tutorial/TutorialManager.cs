using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;


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

    FirstCropReady,
    
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

    // GameObject obj = GameObject.Find("");
    // TutorialUIPrefab tutScript = obj.GetComponent<TutorialUIPrefab>();
    
    [Header("Old Man Character")]
    [SerializeField] private Sprite oldManPortrait;
    
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

    TutorialUIPrefab tutScript;

    [SerializeField] private AudioClip[] oldManVoices;
    [SerializeField] private AudioSource voiceAudioSource;
    private Coroutine typingCoroutine;
    
private void PlayTypingWithMumble(string text)
{
    if (typingCoroutine != null)
        StopCoroutine(typingCoroutine);
    typingCoroutine = StartCoroutine(TypeTextWithMumble(text));
}
    
private IEnumerator TypeTextWithMumble(string text)
{
    if (tutScript == null || tutScript.description == null) yield break;
    tutScript.setDescription(""); // Clear text

    // Debugging
    Debug.Log($"voiceAudioSource: {voiceAudioSource}");
    Debug.Log($"oldManVoices.Length: {(oldManVoices != null ? oldManVoices.Length : 0)}");

    if (voiceAudioSource != null && oldManVoices != null && oldManVoices.Length > 0)
    {
        var clip = oldManVoices[UnityEngine.Random.Range(0, oldManVoices.Length)];
        Debug.Log($"Playing mumble clip: {clip?.name}");
        voiceAudioSource.clip = clip;
        voiceAudioSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
        voiceAudioSource.volume = UnityEngine.Random.Range(0.55f, 0.95f);
        voiceAudioSource.loop = true;
        voiceAudioSource.Play();
    }
    else
    {
        Debug.LogWarning("AudioSource or mumble clips not assigned!");
    }

    foreach (char c in text)
    {
        tutScript.setDescription(tutScript.description.text + c);
        yield return new WaitForSecondsRealtime(0.04f);
    }

    if (voiceAudioSource != null && voiceAudioSource.isPlaying)
    {
        voiceAudioSource.Stop();
        voiceAudioSource.loop = false;
    }
}

    private int currStep = 0;

    string title1 = "Old Pete's Farm Fiasco!";
        string description1 = "Howdy, greenhorn! I'm Old Pete, and this farm's your ticket to glory—if you can keep those darn wolves at bay!";


        // Step 2: Camera Controls (triggers when welcome step completes)

            string title2 = "Look Around Your Farm";
        string description2 = "First things first - let's learn to look around! Use <color=purple><i>W A S D</i></color></color> to move the camera, <color=purple><i> Q E </i></color> to rotate, your <color=purple><i>mouse wheel</i></color> to zoom in and out (you can also use 1 and 2), <color=purple><i>hold right click</i></color> to also move the camera (You can also push the camera with your mouse). Take a moment to explore your land, get familiar with the lay of the land!";


        // Step 3: Opening the Shop (triggers when camera step completes)

           string  title3 = "Open the Build Shop";
       string  description3 = "Now, see that shop icon in the <color=purple><i>bottom left of your screen</i></color>? That's your <color=purple><i>build shop</i></color>! Click on it to see what structures you can build, you can use <color=purple><i>R</i></color> to rotate the building! We'll need to construct some buildings to get this farm running properly!";


        // Step 4: Farm House First

         string   title4 = "Build Your Farm House";
       string  description4 = "Perfect! Now you can see all the buildings you can construct. Every farm needs a proper house! Look for the <color=purple><i>Farm House</i></color> in the shop and click on it, then click somewhere on your land to place it. This will be the heart of your operation!";


        // Step 5: Place Crop Plot

           string  title5 = "Plant Your First Crops";
      string   description5 = "A farm ain't a farm without crops! Build a <color=purple><i>Crop Plot</i></color> near your house. This is where you'll grow food - both to <color=purple><i>feed your animals</i></color> and to store for tough times ahead.";


        // Step 6: Plant First Crop

          string   title6 = "Plant Your Seeds";
      string   description6 = "Excellent! Now click on your crop plot and choose what to <color=purple><i>plant</i></color>. I'd recommend starting with <color=purple><i>Sunflowers</i></color> - they will be used to feed your <color=purple><i>chickens</i></color>. Remember, you can only <color=purple><i>plant during the day</i></color>!";



          string   title7 = "Build Storage - The Silo";
       string  description7 = "Good work! Now you'll need somewhere to store your harvest. Build a <color=purple><i>Silo</i></color> close to your crops - the <color=purple><i>closer it is, the more efficient</i></color> your farming will be! This is called 'synergy' - structures work better when they're placed strategically.";


        // Step 8: Explain Day/Night & Time Controls

          string   title8 = "Day, Night & Time Controls";
       string  description8 = "See those time controls on the <color=purple><i>bottom right</i></color> of your screen? You can <color=purple><i>pause time, play, or speed things up</i></color>! Your crops grow over time, and there's a day-night cycle. <color=purple><i>During the day, you farm and build. At night... well, that's when the wolves come</i></color>. But don't worry - I've made the days extra long for this tutorial so you have plenty of time to learn!";

        // Step 9: Chicken Coop for Production (after time controls explanation)

           string  title9 = "Start Animal Production";
       string  description9 = "Now let's add some livestock! Build a <color=purple><i>Chicken Coop</i></color>. Chickens will lay eggs that you can <color=purple><i>collect and sell for money</i></color>. Place it <color=purple><i>near your silo for better efficiency</i></color> - animals <color=purple><i>eat less food</i></color> when they're close to storage!";

        string title10 = "Harvest Your Sunflowers";
           string  description10 = "Look at that! Your sunflowers are ready to <color=purple><i>harvest</i></color>! Click on the <color=purple><i>crop plot</i></color> and harvest your sunflowers. You'll need these to feed your <color=purple><i>chickens</i></color>!";


        // Step 12: Feed Animals (after harvest AND chickens bought)

       string  title11 = "Feed Your Chickens";
         string    description11 = "Now that you have sunflowers, your chickens are hungry! Click on the <color=purple><i>chicken coop</i></color> and feed them with your harvested sunflowers. <color=purple><i>Well-fed animals</i></color> will start <color=purple><i>producing products which turns into money</i></color>!";


        // Step 13: Watch Production Start

      string   title12 = "Collect Your Eggs";
        string     description12 = "Excellent! Your chickens are now fed and <color=purple><i>producing eggs</i></color>. They will produce eggs quickly for the tutorial! Click on the chicken coop and <color=purple><i>collect the eggs to earn money</i></color>. This is how you'll fund your expansion and defenses!";


        // Step 14: Collect Products

    //    string  title13 = "Collect Your Eggs";
    //     string     description13 = "Perfect! Your chickens have finished producing eggs. Click on the chicken coop and <color=purple><i>collect the eggs to earn money</i></color>. This is how you'll fund your expansion and defenses!";

        // Step 15: Build Barracks for Defense

       string  title14 = "Prepare Your Defenses";
        string     description14 = "Now for the important part - defense! Those wolves I mentioned? They <color=purple><i>attack at night</i></color>. Build a Barracks near your chicken coop but not too close -> <color=purple><i>if they are too close it will cost more to recruit your army animals<color=purple><i>!! The <color=purple><i>barracks will recruit chickens to form an army that protects your farm</i></color>!";


        // Step 16: Place Defense Flag

       string  title16 = "Set Your Defense Position";
        string     description16 = "Great! Now <color=purple><i>click on your barracks and place a flag</i></color>. This flag shows your <color=purple><i>army where to gather and defend</i></color>. Place it in a strategic position where your army can protect your important buildings!";


        // Step 17: Recruit Army

        string title15 = "Recruit Your Army";
         string    description15 = "Perfect! Now <color=purple><i>recruit 2-3 soldier chickens</i></color> from your barracks. They'll <color=purple><i>cost money</i></color> and will <color=purple><i>take chickens from your coop</i></color>, but they're essential for defense. Start small - 2-3 soldiers should be enough for your first night. The further your barracks are to your chicken coop, the cheaper recruitment is!";


        // Step 18: First Night - Special Tutorial Night Start

    //    string  title17 = "Ready for Your First Night?";
    //     string     description17 = "Perfect! You've recruited your army and set up your defenses. When you're completely ready to face the night, click the 'Next' button below. The button will only be enabled when your defenses are properly set up. Take your time - you have full control!";


        // Step 19: Night Defense

       string  title19 = "Watch Your Army Fight!";
        string     description19 = "Look at them go! Your soldier chickens are defending your farm. Watch how they <color=purple><i>move to the flag position and fight off the wolves</i></color>. If <color=purple><i>all your structures get destroyed, it's game over</i></color>, so keep building up your defenses!";


        // Step 20: Show Synergies

       string  title18 = "Understanding Synergies";
          string   description18 = "Here's a pro tip! Buildings work better when placed near each other:\n• Silos <color=purple><i>near</i></color> crops = <color=purple><i>more</i></color> production\n• Animals <color=purple><i>near</i></color> silos = eat <color=purple><i>less</i></color> food\n• Barracks <color=purple><i>farther</i></color> from animals = <color=purple><i>cheaper</i></color> recruitment\nPlan your layout carefully! <color=purple>Click next to end tutorial</color>";


        // Step 21: Complete Tutorial

           string  title20 = "You're Ready to Farm!";
          string   description20 = "Congratulations! You've learned the basics of Cluck N Load. Keep expanding your farm, try <color=purple><i>different animals</i></color>, experiment with layouts, and survive as many nights as you can. Remember: <color=purple><i>Farm during the day, fight at night</i></color>, and always plan ahead!\n\nGood luck, farmer!";


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // InitializeTutorialSteps();
    }

    private void Update()
    {
        if (currStep == 1 && Input.GetKeyDown(KeyCode.W))
        {
            displayStuff(title3, description3);
            currStep++;
        }
    }

    public void endTut()
    {
        CompleteTutorial();
    }

    public void CheckStep2()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 2 && called == false)
        {
            Debug.Log("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            displayStuff(title4, description4);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }

    public void CheckStep3()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 3 && called == false)
        {
            displayStuff(title5, description5);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }

    public void CheckStep4()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 4 && called == false)
        {
            displayStuff(title6, description6);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }

    public void CheckStep5()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 5 && called == false)
        {
            displayStuff(title7, description7);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }
    public void CheckStep6()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 6 && called == false)
        {
            displayStuff(title8, description8);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }

    public void CheckStep7()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 7 && called == false)
        {
            displayStuff(title9, description9);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }
    public void CheckStep8()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 8 && called == false)
        {
            displayStuff(title10, description10);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }
    public void CheckStep9()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 9 && called == false)
        {
            displayStuff(title11, description11);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }

    public void CheckStep10()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 10 && called == false)
        {
            displayStuff(title12, description12);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }
    public void CheckStep11()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 11 && called == false)
        {
            Debug.Log("checkCollect called+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            displayStuff(title14, description14);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }
    public void CheckStep12()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 12 && called == false)
        {
            Debug.Log("checkCollect called+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            displayStuff(title15, description15);
            currStep++;
            called = true;
        }
        else
        {
            return;
        }
    }
    public void CheckStep13()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 13 && called == false)
        {
            Debug.Log("checkCollect called+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            displayStuff(title16, description16);
            currStep++;
            called = true;
            
        }
        else
        {
            return;
        }
    }
    public void CheckStep14()
    {
        bool called = false;
        Debug.Log($"Checking Step 2 conditions...{currStep}");
        if (currStep == 14 && called == false)
        {
            Debug.Log("checkCollect called+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            displayStuff(title18, description18);
            currStep++;
            called = true;             
            CompleteTutorial();
        }
        else
        {
            return;
        }
    }

    public void EndTutorial()
    {
        Debug.Log("Ending the tutorial and resuming normal gameplay.");

        // Mark the tutorial as completed
        tutorialCompleted = true;

        // Stop any ongoing tutorial-related coroutines
        if (conditionPollingCoroutine != null)
        {
            StopCoroutine(conditionPollingCoroutine);
            conditionPollingCoroutine = null;
        }

        // Clear any pending tutorial steps
        pendingSteps.Clear();

        // Hide the tutorial UI
        // if (tutorialPanel != null)
        // {
        //     tutorialPanel.SetActive(false);
        // }

        // Disable the world pointer
        if (worldPointer != null)
        {
            worldPointer.SetActive(false);
        }

        // Resume normal game time
        ResumeFromTutorial();

        // Save the tutorial completion state
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // Trigger any additional logic for ending the tutorial
        OnTutorialCompleted?.Invoke();

        Debug.Log("Tutorial has been successfully ended.");
    }

    private void Start()
    {
        tutScript = FindObjectOfType<TutorialUIPrefab>();


        displayStuff(title1, description1);


        // if (!enableTutorial)
        // {
        //     gameObject.SetActive(false);
        //     return;
        // }

        // if (skipTutorialOnRestart && HasCompletedTutorial())
        // {
        //     gameObject.SetActive(false);
        //     return;
        // }



        // SetupUI();
        // StartTutorial();
    }

    private void InitializeTutorialSteps()
{
    tutorialSteps.Clear();


        // Step 11: Watch Crops Grow (REMOVED - this was causing overlap with step 7)

        // Step 11: Harvest Your Crops (when they're ready)

        
    }

    public void nextThing()
    {
        if (currStep == 0)
        {
            displayStuff(title2, description2);
            Debug.Log("--------------------------------------------------------------------");     
            currStep++;       
        }
    }


       private void displayStuff(string title, string description)
    {
        tutScript.setTitle(title);
        if (tutScript != null)
            tutScript.PlayTypingWithMumble(description);
        else
            PlayTypingWithMumble(description); // fallback
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

                //             var uiScript = tutorialUI.GetComponent<TutorialUIPrefab>();
                //             if (uiScript != null)
                //             {
                //                 if (tutorialDescription == null) tutorialDescription = uiScript.dialogueText;
                //                 if (tutorialTitle == null) tutorialTitle = uiScript.characterNameText;
                //                 if (nextButton == null) nextButton = uiScript.nextButton;
                //                 if (skipButton == null) skipButton = uiScript.skipButton;
                //                 if (characterPortrait == null) characterPortrait = uiScript.characterPortrait;

                //                 Debug.Log("Auto-assigned UI components from TutorialUIPrefab");
                //             }
                //         }
                //     }

                //     if (tutorialPanel != null)
                //     {
                //         tutorialPanel.SetActive(false);
                //     }

                //     if (nextButton != null)
                //     {
                //         nextButton.onClick.AddListener(NextTutorialStep);
                //     }

                //     if (skipButton != null)
                //     {
                //         skipButton.onClick.AddListener(SkipTutorial);
                //     }

                //     if (startNightButton != null)
                //     {
                //         startNightButton.onClick.AddListener(OnStartNightClicked);
                //         startNightButton.gameObject.SetActive(false);
                //     }

                //     if (characterPortrait != null && oldManPortrait != null)
                //     {
                //         characterPortrait.sprite = oldManPortrait;
                //     }
                // }
            }
        }
    }

    private void StartTutorial()
    {
        // if (tutorialCompleted) return;

        // if (conditionPollingCoroutine == null)
        // {
        //     conditionPollingCoroutine = StartCoroutine(PollForMissedConditions());
        // }

        // OnConditionMet(TutorialCondition.GameStarted);
    }

    public void OnConditionMet(TutorialCondition condition)
{
    // if (!enableTutorial || tutorialCompleted) return;

    // if (completedConditions.Contains(condition)) 
    // {
    //     Debug.Log($"Tutorial condition {condition} already completed - ignoring duplicate");
    //     return;
    // }

    // Debug.Log($"Tutorial condition met: {condition}");
    // completedConditions.Add(condition);
    // OnConditionCompleted?.Invoke(condition);

    // TutorialStep nextStep = GetNextIncompleteStep();
    
    // if (nextStep != null && (nextStep.triggerCondition == condition || (nextStep.stepId == "time_controls" && condition == TutorialCondition.TimeControlsExplained)) && CanTriggerStepStrictly(nextStep))
    // {
    //     Debug.Log($"Tutorial: Triggering next sequential step {nextStep.stepId}");
    //     pendingSteps.Clear();
    //     pendingSteps.Enqueue(nextStep);
    //     ProcessPendingSteps();
    // }
    // else if (nextStep != null)
    // {
    //     Debug.Log($"Tutorial: Condition {condition} met, but next step is {nextStep.stepId} (waiting for {nextStep.triggerCondition})");
    // }
    // else
    // {
    //     Debug.Log($"Tutorial: No more incomplete steps to process");
    // }
}

    private TutorialStep GetNextIncompleteStep()
    {
        // foreach (var step in tutorialSteps)
        // {
        //     if (!step.isCompleted)
        //     {
        //         return step;
        //     }
        // }
        return null;
    }
private bool CanTriggerStepStrictly(TutorialStep step)
{
    // if (step.isCompleted)
    // {
    //     Debug.Log($"Tutorial step {step.stepId} blocked: Already completed");
    //     return false;
    // }

    // if (!ArePrerequisitesStrictlyMet(step))
    // {
    //     Debug.Log($"Tutorial step {step.stepId} blocked: Prerequisites not met");
    //     return false;
    // }

    // if (isTutorialActive && currentStep != null && currentStep.stepId != step.stepId)
    // {
    //     Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({currentStep.stepId}) is currently active");
    //     return false;
    // }

    // // Allow time_controls if either FirstCropHarvested or TimeControlsExplained is met
    // if (step.stepId == "time_controls" && (completedConditions.Contains(TutorialCondition.FirstCropHarvested) || completedConditions.Contains(TutorialCondition.TimeControlsExplained)))
    // {
    //     Debug.Log($"Tutorial step {step.stepId} allowed: FirstCropHarvested or TimeControlsExplained condition met");
    //     return true;
    // }

    // int stepIndex = tutorialSteps.FindIndex(s => s.stepId == step.stepId);
    // if (stepIndex > 0)
    // {
    //     for (int i = 0; i < stepIndex; i++)
    //     {
    //         if (!tutorialSteps[i].isCompleted)
    //         {
    //             Debug.Log($"Tutorial step {step.stepId} blocked: Previous step {tutorialSteps[i].stepId} not completed yet (index {i})");
    //             return false;
    //         }
    //     }
    // }

    // Debug.Log($"Tutorial step {step.stepId} can be triggered");
    return true;
}

    private void ProcessPendingSteps()
    {
        // if (isTutorialActive || pendingSteps.Count == 0)
        // {
        //     Debug.Log($"ProcessPendingSteps: Cannot process - Tutorial active: {isTutorialActive}, Pending steps: {pendingSteps.Count}");
        //     return;
        // }

        // var step = pendingSteps.Dequeue();
        // Debug.Log($"Processing pending step: {step.stepId} (Trigger: {step.triggerCondition})");
        // ShowTutorialStep(step);
    }

 public void ShowTutorialStep(TutorialStep step)
{
    // if (currentTutorialCoroutine != null)
    // {
    //     StopCoroutine(currentTutorialCoroutine);
    // }

    // if (step.stepId == "open_shop")
    // {
    //     Debug.Log("Tutorial: Preparing for open_shop step - forcing shop closed and enabling shop button");
    //     if (shopManager != null)
    //     {
    //         // shopManager.CloseShop();
    //         shopManager.ResetShopState();
    //         shopManager.enableShop();
    //     }
    //     if (NightManager.Instance != null && NightManager.Instance.shopManager != null)
    //     {
    //         // NightManager.Instance.shopManager.CloseShop();
    //         NightManager.Instance.shopManager.ResetShopState();
    //         if (NightManager.Instance.IsDay)
    //             NightManager.Instance.shopManager.enableShop();
    //     }
    // }
    // else if (step.stepId == "harvest_first_crops")
    // {
    //     CropStructure[] crops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
    //     bool cropReady = false;
    //     foreach (var crop in crops)
    //     {
    //         if (crop.CropReady)
    //         {
    //             cropReady = true;
    //             step.worldPosition = crop.transform.position + Vector3.up * 3f;
    //             Debug.Log($"Tutorial: Highlighting crop plot {crop.name} at {step.worldPosition} for harvest");
    //             StartCoroutine(PulseEffect(crop.gameObject));
    //             if (worldPointer != null)
    //             {
    //                 worldPointer.transform.position = step.worldPosition + Vector3.up * 1f;
    //                 worldPointer.SetActive(true);
    //             }
    //             break;
    //         }
    //     }
    //     if (!cropReady)
    //     {
    //         Debug.Log("Tutorial: No crop ready for harvest, delaying harvest_first_crops step");
    //         StartCoroutine(ShowNextStepAfterDelay("harvest_first_crops", 1f));
    //         return;
    //     }
    // }
    // else if (step.stepId == "time_controls")
    // {
    //     Debug.Log("Tutorial: Forcing time_controls step UI visibility");
    //     ForceShowTutorialUI();
    // }
    // else
    // {
    //     if (shopManager != null)
    //     {
    //         // shopManager.CloseShop();
    //     }
    //     if (NightManager.Instance != null && NightManager.Instance.shopManager != null)
    //     {
    //         // NightManager.Instance.shopManager.CloseShop();
    //         NightManager.Instance.shopManager.ResetShopState();
    //         if (NightManager.Instance.IsDay)
    //             NightManager.Instance.shopManager.enableShop();
    //     }
    // }

    // ForceShowTutorialUI(); // Ensure UI is visible for all steps
    // currentStep = step;
    // currentTutorialCoroutine = StartCoroutine(DisplayTutorialStep(step));
}

    private IEnumerator DisplayTutorialStep(TutorialStep step)
    {
        // isTutorialActive = true;

        // if (step.pauseGame)
        // {
        //     PauseForTutorial();
        // }

        // if (tutorialPanel != null)
        // {
        //     tutorialPanel.SetActive(true);
            
        //     var canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
        //     if (canvasGroup != null)
        //     {
        //         canvasGroup.alpha = 1f;
        //         canvasGroup.interactable = true;
        //         canvasGroup.blocksRaycasts = true;
        //     }

        //     if (tutorialTitle != null)
        //         tutorialTitle.text = step.title;

        //     var uiScript = tutorialPanel != null ? tutorialPanel.GetComponent<TutorialUIPrefab>() : null;
        //     if (uiScript != null)
        //         uiScript.PlayTypingWithMumble(step.description);
        //     else if (tutorialDescription != null)
        //         tutorialDescription.text = step.description;

        //     PlayOldManVoice();

        //     if (step.pointToWorldPosition && worldPointer != null)
        //     {
        //         worldPointer.SetActive(true);
        //         worldPointer.transform.position = step.worldPosition;
        //     }
        //     else if (worldPointer != null)
        //     {
        //         worldPointer.SetActive(false);
        //     }

        //     if (step.highlightUI && !string.IsNullOrEmpty(step.highlightUITag))
        //     {
        //         HighlightUIElement(step.highlightUITag);
        //     }

        //     if (step.showStartNightButton && startNightButton != null)
        //     {
        //         startNightButton.gameObject.SetActive(true);
        //         StartCoroutine(UpdateStartNightButtonState(step));
        //     }
        //     else if (startNightButton != null)
        //     {
        //         startNightButton.gameObject.SetActive(false);
        //     }
        // }

        // while (currentStep == step && isTutorialActive)
        // {
        //     yield return null;
        // }

            yield return null;
        // CompleteCurrentStep();
    }

private void CompleteCurrentStep()
{
    // if (currentStep != null)
    // {
    //     currentStep.isCompleted = true;

    //     if (currentStep.stepId == "welcome")
    //     {
    //         Debug.Log("Tutorial: Welcome completed, showing camera controls");
    //         StartCoroutine(ShowNextStepAfterDelay("camera_controls", 0.5f));
    //     }
    //     else if (currentStep.stepId == "camera_controls")
    //     {
    //         Debug.Log("Tutorial: Camera controls completed, showing shop instruction");
    //         StartCoroutine(ShowNextStepAfterDelay("open_shop", 0.5f));
    //     }
    //     else if (currentStep.stepId == "open_shop")
    //     {
    //         Debug.Log("Tutorial: Shop instruction completed, waiting for shop to be opened");
    //         StartCoroutine(DelayNextStepAfterShopOpened());
    //     }
    //     else if (currentStep.stepId == "harvest_first_crops")
    //     {
    //         Debug.Log("Tutorial: Harvest first crops completed, checking harvest status");
    //         var conditionTracker = FindFirstObjectByType<TutorialConditionTracker>();
    //         conditionTracker?.CheckInventoryForHarvest();
    //         if (conditionTracker != null && conditionTracker.hasHarvestedFirstCrop && completedConditions.Contains(TutorialCondition.FirstCropHarvested))
    //         {
    //             Debug.Log("Tutorial: Harvest confirmed, triggering time_controls step");
    //             OnConditionMet(TutorialCondition.TimeControlsExplained);
    //             StartCoroutine(ShowNextStepAfterDelay("time_controls", 0.5f));
    //         }
    //         else
    //         {
    //             Debug.Log("Tutorial: Waiting for player to harvest crop or FirstCropHarvested condition, scheduling recheck");
    //             StartCoroutine(WaitForHarvestAndProceed());
    //         }
    //     }
    //     else if (currentStep.stepId == "tutorial_complete")
    //     {
    //         CompleteTutorial();
    //     }
    // }

    //     // Hide UI
    //     // if (tutorialPanel != null)
    //     // {
    //     //     tutorialPanel.SetActive(false);
    //     // }

    // if (worldPointer != null)
    // {
    //     worldPointer.SetActive(false);
    // }

    // if (startNightButton != null)
    // {
    //     startNightButton.gameObject.SetActive(false);
    // }

    // ResumeFromTutorial();

    // isTutorialActive = false;
    // currentStep = null;

    // ProcessPendingSteps();
}

    private IEnumerator WaitForHarvestAndProceed()
    {
    return null;
        // Debug.Log("Tutorial: Waiting for harvest to be completed or FirstCropHarvested condition to be met");

        // // Wait for a short period to check if the player harvests the crop
    // var conditionTracker = FindFirstObjectByType<TutorialConditionTracker>();
        // float timeout = 5f;
        // float elapsed = 0f;

        // while (elapsed < timeout)
        // {
        //     conditionTracker?.CheckInventoryForHarvest();
        //     if (conditionTracker != null && conditionTracker.hasHarvestedFirstCrop && completedConditions.Contains(TutorialCondition.FirstCropHarvested))
        //     {
        //         Debug.Log("Tutorial: Harvest detected after delay, proceeding to time_controls");
        //         if (!pendingSteps.Any(s => s.stepId == "time_controls") && (currentStep == null || currentStep.stepId != "time_controls"))
        //         {
        //             OnConditionMet(TutorialCondition.TimeControlsExplained);
        //             StartCoroutine(ShowNextStepAfterDelay("time_controls", 0.5f));
        //         }
        //         else
        //         {
        //             Debug.Log("Tutorial: time_controls already queued or active, skipping duplicate");
        //         }
        //         yield break;
        //     }
        //     elapsed += 0.1f;
        //     yield return new WaitForSeconds(0.1f);
        // }

        // Debug.LogWarning("Tutorial: Harvest not detected within timeout, forcing FirstCropHarvested and time_controls step");
        // if (conditionTracker != null)
        // {
        //     conditionTracker.hasHarvestedFirstCrop = true;
        // }
        // OnConditionMet(TutorialCondition.FirstCropHarvested);
        // if (!pendingSteps.Any(s => s.stepId == "time_controls") && (currentStep == null || currentStep.stepId != "time_controls"))
        // {
        //     OnConditionMet(TutorialCondition.TimeControlsExplained);
        //     StartCoroutine(ShowNextStepAfterDelay("time_controls", 0.1f));
        // }
        // else
        // {
        //     Debug.Log("Tutorial: time_controls already queued or active, skipping duplicate");
        // }
    }
    private IEnumerator DelayNextStepAfterShopOpened()
    {
        return null;
        // yield return new WaitForSecondsRealtime(1.5f);
        // ProcessPendingSteps();
    }

    public void NextTutorialStep()
    {
        // Debug.Log("NextTutorialStep called!");
        // if (currentStep != null)
        // {
        //     Debug.Log($"Completing current step: {currentStep.stepId}");
        //     CompleteCurrentStep();
        // }
        // else
        // {
        //     Debug.LogWarning("NextTutorialStep called but currentStep is null!");
        // }
    }

    public void SkipTutorial()
    {
        // CompleteTutorial();
    }

    public void OnStartNightClicked()
    {
        // Debug.Log("Tutorial: Start Night button clicked!");
        
        // var tutorialTracker = FindFirstObjectByType<TutorialConditionTracker>();
        // if (tutorialTracker != null && tutorialTracker.AreDefensesReady())
        // {
        //     if (nightManager != null)
        //     {
        //         Debug.Log("Tutorial: Starting night manually via tutorial button");
        //         nightManager.ForceStartNight();
        //         OnConditionMet(TutorialCondition.NightStarted);
        //         NextTutorialStep();
        //     }
        //     else
        //     {
        //         Debug.LogError("Tutorial: NightManager not found!");
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("Tutorial: Defenses not ready - button should be disabled!");
        // }
    }

    private IEnumerator UpdateStartNightButtonState(TutorialStep step)
    {
        // while (currentStep == step && startNightButton != null && startNightButton.gameObject.activeInHierarchy)
        // {
        //     if (step.requiresDefensesReady)
        //     {
        //         var tutorialTracker = FindFirstObjectByType<TutorialConditionTracker>();
        //         bool defensesReady = tutorialTracker != null && tutorialTracker.AreDefensesReady();

        //         startNightButton.interactable = defensesReady;

        //         var buttonText = startNightButton.GetComponentInChildren<TextMeshProUGUI>();
        //         if (buttonText != null)
        //         {
        //             buttonText.text = defensesReady ? "Start Night!" : "Defenses Not Ready";
        //             buttonText.color = defensesReady ? Color.white : Color.gray;
        //         }
        //     }
        //     else
        //     {
        //         startNightButton.interactable = true;
        //     }

        //     yield return new WaitForSeconds(0.1f);
        // }
        return null;
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
        return true;
        // return PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
    }

    private void PlayOldManVoice()
    {
        // if (voiceAudioSource != null && oldManVoices != null && oldManVoices.Length > 0)
        // {
        //     var randomClip = oldManVoices[UnityEngine.Random.Range(0, oldManVoices.Length)];
        //     voiceAudioSource.clip = randomClip;
        //     voiceAudioSource.Play();
        // }
    }

    private void HighlightUIElement(string tag)
{
    // try
    // {
    //     GameObject uiElement = GameObject.FindGameObjectWithTag(tag);
    //     if (uiElement != null)
    //     {
    //         Debug.Log($"Tutorial: Highlighting UI element {uiElement.name} with tag: {tag}");
    //         StartCoroutine(PulseUIEffect(uiElement));
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"Tutorial: No GameObject found with tag: {tag}");
    //     }
    // }
    // catch (UnityException ex)
    // {
    //     Debug.LogWarning($"Tutorial: Tag '{tag}' is not defined. Skipping highlight. Error: {ex.Message}");
    // }
}

    public void ForceShowTutorialUI()
    {
        // if (tutorialPanel != null)
        // {
        //     tutorialPanel.SetActive(true);
            
        //     var canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
        //     if (canvasGroup != null)
        //     {
        //         canvasGroup.alpha = 1f;
        //         canvasGroup.interactable = true;
        //         canvasGroup.blocksRaycasts = true;
        //         Debug.Log("Forced tutorial UI alpha to 1");
        //     }
            
        //     Transform parent = tutorialPanel.transform.parent;
        //     while (parent != null)
        //     {
        //         var parentCanvasGroup = parent.GetComponent<CanvasGroup>();
        //         if (parentCanvasGroup != null)
        //         {
        //             parentCanvasGroup.alpha = 1f;
        //             Debug.Log($"Set parent {parent.name} alpha to 1");
        //         }
        //         parent = parent.parent;
        //     }
            
        //     Debug.Log("Tutorial UI forced visible");
        // }
        // else
        // {
        //     Debug.LogError("Tutorial panel is null!");
        // }
    }

    [ContextMenu("Test Tutorial Display")]
    public void TestTutorialDisplay()
    {
        // if (tutorialSteps.Count > 0)
        // {
        //     ForceShowTutorialUI();
        //     DisplayTutorialStep(tutorialSteps[0]);
        // }
    }

    [ContextMenu("Debug UI Connections")]
    public void DebugUIConnections()
    {
        // Debug.Log($"Tutorial Panel: {(tutorialPanel != null ? tutorialPanel.name : "NULL")}");
        // Debug.Log($"Tutorial Title: {(tutorialTitle != null ? tutorialTitle.name : "NULL")}");
        // Debug.Log($"Tutorial Description: {(tutorialDescription != null ? tutorialDescription.name : "NULL")}");
        // Debug.Log($"Next Button: {(nextButton != null ? nextButton.name : "NULL")}");
        // Debug.Log($"Skip Button: {(skipButton != null ? skipButton.name : "NULL")}");
        // Debug.Log($"Character Portrait: {(characterPortrait != null ? characterPortrait.name : "NULL")}");
    }

    [ContextMenu("Force Next Tutorial Step")]
    public void ForceNextTutorialStep()
    {
        // if (tutorialCompleted)
        // {
        //     Debug.Log("Tutorial already completed");
        //     return;
        // }
        
        // foreach (var step in tutorialSteps)
        // {
        //     if (!step.isCompleted)
        //     {
        //         Debug.Log($"Forcing tutorial step: {step.stepId} ({step.triggerCondition})");
        //         OnConditionMet(step.triggerCondition);
        //         break;
        //     }
        // }
    }

    [ContextMenu("Show Tutorial State")]
    public void ShowTutorialState()
    {
        // Debug.Log("=== TUTORIAL STATE ===");
        // Debug.Log($"Tutorial Active: {isTutorialActive}");
        // Debug.Log($"Tutorial Completed: {tutorialCompleted}");
        // Debug.Log($"Current Step: {(currentStep != null ? currentStep.stepId : "None")}");
        // Debug.Log($"Pending Steps: {pendingSteps.Count}");
        // Debug.Log($"Completed Conditions: {string.Join(", ", completedConditions)}");
        
        // Debug.Log("Next incomplete steps:");
        // int count = 0;
        // foreach (var step in tutorialSteps)
        // {
        //     if (!step.isCompleted)
        //     {
        //         Debug.Log($"  {step.stepId} (waiting for: {step.triggerCondition})");
        //         count++;
        //         if (count >= 3) break;
        //     }
        // }
        // Debug.Log("=====================");
    }

    [ContextMenu("Fix Animal Production Tutorial")]
    public void FixAnimalProductionTutorial()
    {
        // Debug.Log("=== FIXING ANIMAL PRODUCTION TUTORIAL ===");
        
        // if (!completedConditions.Contains(TutorialCondition.FirstChickenBought))
        // {
        //     Debug.Log("Manually triggering FirstChickenBought");
        //     OnConditionMet(TutorialCondition.FirstChickenBought);
        // }
        
        // if (!completedConditions.Contains(TutorialCondition.ChickensStartedProducing))
        // {
        //     Debug.Log("Manually triggering ChickensStartedProducing");
        //     OnConditionMet(TutorialCondition.ChickensStartedProducing);
        // }
        
        // if (!completedConditions.Contains(TutorialCondition.AnimalProductsReady))
        // {
        //     Debug.Log("Manually triggering AnimalProductsReady");
        //     OnConditionMet(TutorialCondition.AnimalProductsReady);
        // }
        
        // if (!completedConditions.Contains(TutorialCondition.AnimalProductsCollected))
        // {
        //     Debug.Log("Manually triggering AnimalProductsCollected");
        //     OnConditionMet(TutorialCondition.AnimalProductsCollected);
        // }
        
        // Debug.Log("Animal production tutorial fix completed!");
    }

    [ContextMenu("Fix Defense Tutorial")]
    public void FixDefenseTutorial()
    {
        // Debug.Log("=== FIXING DEFENSE TUTORIAL ===");
        
        // if (!completedConditions.Contains(TutorialCondition.BarracksPlaced))
        // {
        //     Debug.Log("Manually triggering BarracksPlaced");
        //     OnConditionMet(TutorialCondition.BarracksPlaced);
        // }
        
        // if (!completedConditions.Contains(TutorialCondition.FlagPlaced))
        // {
        //     Debug.Log("Manually triggering FlagPlaced");
        //     OnConditionMet(TutorialCondition.FlagPlaced);
        // }
        
        // if (!completedConditions.Contains(TutorialCondition.ArmyRecruited))
        // {
        //     Debug.Log("Manually triggering ArmyRecruited");
        //     OnConditionMet(TutorialCondition.ArmyRecruited);
        // }
        
        // Debug.Log("Defense tutorial fix completed!");
    }

    public bool IsTutorialActive()
    {
        // Tutorial is active if not completed and not skipped
        return enableTutorial && !tutorialCompleted;
    }

    public bool IsTutorialCompleted()
    {
        return tutorialCompleted;
    }

    public void ResetTutorial()
    {
        // PlayerPrefs.DeleteKey("TutorialCompleted");
        // tutorialCompleted = false;
        // completedConditions.Clear();
        
        // foreach (var step in tutorialSteps)
        // {
        //     step.isCompleted = false;
        // }
    }

    public List<TutorialStep> GetTutorialSteps()
    {
        return tutorialSteps;
    }

    private void PauseForTutorial()
    {
        // if (pauseManager != null)
        // {
        //     wasPausedBeforeTutorial = Time.timeScale == 0f;
        //     pauseManager.pauseGame();
        // }
        // else
        // {
        //     wasPausedBeforeTutorial = Time.timeScale == 0f;
        //     Time.timeScale = 0f;
        // }
        
        // Debug.Log("Tutorial: Game paused for tutorial step");
    }

    private void ResumeFromTutorial()
    {
        // if (!wasPausedBeforeTutorial)
        // {
        //     if (pauseManager != null)
        //     {
        //         pauseManager.playGame();
        //     }
        //     else
        //     {
        //         Time.timeScale = 1f;
        //     }
            
        //     Debug.Log("Tutorial: Game resumed after tutorial step");
        // }
        // else
        // {
        //     Debug.Log("Tutorial: Game remains paused (was paused before tutorial step)");
        // }
    }

    private bool ArePrerequisitesStrictlyMet(TutorialStep step)
    {
        return true;
        // foreach (var prereq in step.prerequisites)
        // {
        //     if (!completedConditions.Contains(prereq))
        //     {
        //         Debug.LogWarning($"Tutorial step {step.stepId} blocked: Missing prerequisite {prereq}");
        //         return false;
        //     }
        // }

        // if (step.prerequisites.Length > 0)
        // {
        //     Debug.Log($"Tutorial step {step.stepId}: All prerequisites met ({string.Join(", ", step.prerequisites)})");
        // }

        // return true;
    }

    private bool CanTriggerStep(TutorialStep step, TutorialCondition condition)
    {
        return true;
        // if (step.triggerCondition != condition)
        //     return false;

        // if (step.isCompleted)
        // {
        //     Debug.Log($"Tutorial step {step.stepId} blocked: Already completed");
        //     return false;
        // }

        // if (!ArePrerequisitesStrictlyMet(step))
        //     return false;

        // foreach (var pendingStep in pendingSteps)
        // {
        //     if (pendingStep.triggerCondition == condition && pendingStep.stepId != step.stepId)
        //     {
        //         Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({pendingStep.stepId}) with same trigger condition is already queued");
        //         return false;
        //     }
        // }

        // if (currentStep != null && currentStep.triggerCondition == condition && currentStep.stepId != step.stepId)
        // {
        //     Debug.Log($"Tutorial step {step.stepId} blocked: Another step ({currentStep.stepId}) with same trigger condition is currently active");
        //     return false;
        // }

        // int stepIndex = tutorialSteps.FindIndex(s => s.stepId == step.stepId);
        // if (stepIndex > 0)
        // {
        //     for (int i = 0; i < stepIndex; i++)
        //     {
        //         if (!tutorialSteps[i].isCompleted)
        //         {
        //             Debug.Log($"Tutorial step {step.stepId} blocked: Previous step {tutorialSteps[i].stepId} not completed yet");
        //             return false;
        //         }
        //     }
        // }

        // Debug.Log($"Tutorial step {step.stepId} validation passed - can be triggered");
        // return true;
    }

    private IEnumerator PollForMissedConditions()
    {
        return null;
        // while (!tutorialCompleted)
        // {
        //     yield return new WaitForSeconds(conditionCheckInterval);

        //     if (!enableTutorial || tutorialCompleted) 
        //         continue;

        //     CheckForMissedConditions();
        // }
    }

    private void CheckForMissedConditions()
    {
        // try
        // {
        //     if (!completedConditions.Contains(TutorialCondition.ShopOpened))
        //     {
        //         if (shopManager != null && shopManager.gameObject.activeInHierarchy)
        //         {
        //             var shopCanvas = shopManager.GetComponent<Canvas>();
        //             if (shopCanvas != null && shopCanvas.enabled)
        //             {
        //                 Debug.Log("Tutorial Polling: Detected missed ShopOpened condition");
        //                 OnConditionMet(TutorialCondition.ShopOpened);
        //             }
        //         }
        //     }
            
        //     if (!completedConditions.Contains(TutorialCondition.FirstCropPlanted))
        //     {
        //         var cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        //         foreach (var crop in cropStructures)
        //         {
        //             if (crop.gameObject.name != "BuildGhost" && crop.CurrentCropType != CropStructure.CropType.None)
        //             {
        //                 Debug.Log("Tutorial Polling: Detected missed FirstCropPlanted condition");
        //                 OnConditionMet(TutorialCondition.FirstCropPlanted);
        //                 break;
        //             }
        //         }
        //     }
            
        //     if (!completedConditions.Contains(TutorialCondition.FlagPlaced))
        //     {
        //         var barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        //         foreach (var barrack in barracks)
        //         {
        //             if (barrack.GetFlagPosition != Vector3.zero)
        //             {
        //                 Debug.Log("Tutorial Polling: Detected missed FlagPlaced condition");
        //                 OnConditionMet(TutorialCondition.FlagPlaced);
        //                 break;
        //             }
        //         }
        //     }
            
        //     if (!completedConditions.Contains(TutorialCondition.ArmyRecruited))
        //     {
        //         var barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        //         foreach (var barrack in barracks)
        //         {
        //             if (barrack.ArmyAnimalCount > 0)
        //             {
        //                 Debug.Log("Tutorial Polling: Detected missed ArmyRecruited condition");
        //                 OnConditionMet(TutorialCondition.ArmyRecruited);
        //                 break;
        //             }
        //         }
        //     }
        // }
        // catch (System.Exception ex)
        // {
        //     Debug.LogWarning($"Tutorial polling error: {ex.Message}");
        // }
    }

    public void CheckTutorialConditions()
    {
        if (!enableTutorial || tutorialCompleted) return;
        
        CheckForMissedConditions();
    }

    public void OnStructurePlaced(GameObject structure)
    {
        // if (!enableTutorial || tutorialCompleted) return;
        
        // string structureName = structure.name.ToLower();
        
        // if (structureName.Contains("farmhouse") || structureName.Contains("farm house"))
        // {
        //     OnConditionMet(TutorialCondition.FarmHousePlaced);
        // }
        // else if (structureName.Contains("silo"))
        // {
        //     OnConditionMet(TutorialCondition.SiloPlaced);
        // }
        // else if (structureName.Contains("crop") || structure.GetComponent<CropStructure>() != null)
        // {
        //     OnConditionMet(TutorialCondition.CropPlotPlaced);
        // }
        // else if (structureName.Contains("chicken") || structureName.Contains("coop") || structure.GetComponent<AnimalStructure>() != null)
        // {
        //     OnConditionMet(TutorialCondition.ChickenCoopPlaced);
        // }
        // else if (structureName.Contains("barracks") || structure.GetComponent<BarracksStructure>() != null)
        // {
        //     OnConditionMet(TutorialCondition.BarracksPlaced);
        // }
        
        // Debug.Log($"Tutorial: Structure placed - {structureName}");
    }

    private IEnumerator ShowNextStepAfterDelay(string stepId, float delay)
    {
        return null;
        // yield return new WaitForSeconds(delay);

        // foreach (var step in tutorialSteps)
        // {
        //     if (step.stepId == stepId && !step.isCompleted)
        //     {
        //         Debug.Log($"Tutorial: Manually showing step {stepId}");
        //         pendingSteps.Enqueue(step);
        //         ProcessPendingSteps();
        //         break;
        //     }
        // }
    }

    // In TutorialManager.cs, modify PulseUIEffect
    private IEnumerator PulseUIEffect(GameObject uiElement)
    {
        return null;
        // if (uiElement == null) yield break;
        // Vector3 originalScale = uiElement.transform.localScale;
        // while (uiElement != null && currentStep != null && currentStep.highlightUI)
        // {
        //     uiElement.transform.localScale = originalScale * (1f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f)); // Increased from 0.05f to 0.1f
        //     yield return null;
        // }
        // if (uiElement != null) uiElement.transform.localScale = originalScale;
    }

    // In TutorialManager.cs, modify PulseEffect
    private IEnumerator PulseEffect(GameObject obj)
    {
        return null;
    // if (obj == null) yield break;
        // Vector3 originalScale = obj.transform.localScale;
        // while (obj != null && currentStep != null && currentStep.pointToWorldPosition)
        // {
        //     obj.transform.localScale = originalScale * (1f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f)); // Increased from 0.1f to 0.2f
        //     yield return null;
        // }
        // if (obj != null) obj.transform.localScale = originalScale;
    }

    public string GetCurrentStepId()
    {
        return "";
        // return currentStep != null ? currentStep.stepId : "None";
    }
}