using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
    public bool movePanelDownRight = false;     // Move down and slightly to the right (used when shop is open)
    public float panelAlpha = 1.0f;             // Background panel opacity (0-1, fade the background)
        public bool disablePanelRaycast = false;    // Disable panel blocking clicks (for UI interaction steps)
        
        [Header("Game UI Control")]
        public bool showGameUI = false;             // Show the main game UI for this step (hidden by default during tutorial)
        public GameObject highlightUIElement;       // UI element to highlight for this step (e.g., shop button)
        public string highlightUIByName = "";       // UI element name to find and highlight (alternative to highlightUIElement)
        
        [Header("Shop Tutorial Control")]
        public bool highlightShopButton = false;    // Highlight the shop button to prompt opening
        public bool openShopAutomatically = false;  // Automatically open the shop when this step starts
        public bool enableShopButton = true;        // Enable/disable shop button for this step (disabled by default for non-shop steps)
        public bool restrictShopBuildings = false;  // Restrict shop to only allow specific buildings
        public List<string> allowedBuildingNames = new List<string>(); // Building names that can be purchased (e.g., "FarmHouse")
        public string requiredShopTab = "";         // Which shop tab should be active (e.g., "C" for Coops, "P" for Plants, "A" for Army, "S" for Defense)
        public string shopTabButtonName = "";       // Name of the tab button to highlight (e.g., "Plant Button", "Coop Button")
        public bool highlightRepairButton = false;  // Highlight the repair all button in shop
        
        [Header("World Structure Interaction")]
        public string highlightStructureType = "";  // Type of structure to highlight in world (e.g., "crop plot", "chicken coop")
        public List<string> highlightStructureUIButtons = new List<string>(); // Button names to highlight in sequence on structure UI panel
        
        [Header("Building Damage Control")]
        public bool damageAllBuildingsOnStart = false; // Damage all buildings when this step starts (for repair tutorial)
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
    private Coroutine delayedHighlightCoroutine; // Track pending delayed highlight coroutine so we can cancel it
    private Coroutine delayedNextStepCoroutine; // Track pending auto-advance coroutine so we can cancel it
    
    // World structure highlighting system
    private GameObject currentHighlightedStructure;
    private int currentUIButtonIndex = 0; // Track which button in the sequence we're highlighting
    private List<string> currentUIButtonSequence = new List<string>();
    
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
        // Check if tutorial was already completed AND this is a loaded game
        // Only skip tutorial if both conditions are true
        #if !UNITY_EDITOR
        bool tutorialCompleted = PlayerPrefs.GetInt("SimplifiedTutorialCompleted", 0) == 1;
        bool isLoadedGame = PlayerPrefs.HasKey("SelectedSaveSlot");
        
        if (tutorialCompleted && isLoadedGame)
        {
            Debug.Log("[SimplifiedTutorialManager] Tutorial already completed and game loaded, skipping");
            return;
        }
        
        // If it's a new game, clear the tutorial completed flag to allow it to run
        if (!isLoadedGame)
        {
            Debug.Log("[SimplifiedTutorialManager] New game detected, starting tutorial");
            PlayerPrefs.SetInt("SimplifiedTutorialCompleted", 0);
        }
        #endif
        
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
    
    // Welcome step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "welcome",
        title = "New Guy in Town",
        message = "Hey! Someone said there's a new farmer here. Let's get started!",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Excited,
        waitForAction = false,
        enableShopButton = false
    });
    
    // Camera movement - show WASD key indicators
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "camera_movement_wasd",
        title = "Move Camera",
        message = "Use WASD to look around!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Pointing,
        waitForAction = true,
        waitForTrigger = "camera_moved_wasd",
        requiredInputs = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D },
        waitForAllInputs = true,
        movePanelDown = true,
        panelAlpha = 0f,
        enableShopButton = false
    });
    
    // Camera rotation
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "camera_rotation",
        title = "Rotate Camera",
        message = "Q and E rotate, or middle mouse + drag!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Pointing,
        waitForAction = true,
        waitForTrigger = "camera_rotated",
        requiredInputs = new List<KeyCode> { KeyCode.Q, KeyCode.E, KeyCode.Mouse2 },
        waitForAllInputs = true,
        movePanelDown = true,
        panelAlpha = 0f,
        enableShopButton = false
    });
    
    // Camera zoom
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "camera_zoom",
        title = "Zoom Camera",
        message = "Scroll wheel OR keys 1 and 2 to zoom!",
        peteContext = PeteContext.CornerBuddy,
        waitForAction = true,
        waitForTrigger = "camera_zoomed",
        requiredInputs = new List<KeyCode> { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Mouse3, KeyCode.Mouse4 },
        waitForAllInputs = true,
        movePanelDown = true,
        panelAlpha = 0f,
        enableShopButton = false
    });
    
    // Camera drag
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "camera_drag",
        title = "Drag Camera",
        message = "Right mouse + drag to rotate view!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Pointing,
        waitForAction = true,
        waitForTrigger = "camera_dragged",
        requiredInputs = new List<KeyCode> { KeyCode.Mouse1 },
        waitForAllInputs = true,
        movePanelDown = true,
        panelAlpha = 0f,
        enableShopButton = false
    });
    
    // Spacebar pause
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "spacebar_pause",
        title = "Pause Time",
        message = "SPACEBAR pauses time. Useful for thinking!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Thinking,
        waitForAction = false,
        movePanelDown = true,
        panelAlpha = 0f,
        enableShopButton = false
    });
    
    // Shop introduction
    var openShopStep = new SimpleTutorialStep
    {
        stepId = "open_shop",
        title = "Let's Build!",
        message = "Click the shop button!",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Excited,
        waitForAction = true,
        waitForTrigger = "shop_opened",
        disablePanelRaycast = true,
        panelAlpha = 0f,
        showGameUI = true,
        highlightShopButton = true,
        enableShopButton = true
    };
    
    if (shopButton != null)
    {
        openShopStep.highlightUIElement = shopButton.gameObject;
    }
    
    tutorialSteps.Add(openShopStep);
    
    // Build farmhouse
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "build_farmhouse",
        title = "Build Your Home",
        message = "Every farm needs a farmhouse! Even you...",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Worried,
        waitForAction = true,
        waitForTrigger = "farmhouse_built",
        movePanelRight = true,
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        enableShopButton = true,
        restrictShopBuildings = true,
        allowedBuildingNames = new List<string> { "FarmHouse", "Farmhouse", "Farm House" },
        requiredShopTab = "C",
        shopTabButtonName = "Coop Button"
    });
    
    // UI Explanation Steps
    
    // Explain money panel
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "explain_money",
        title = "Your Money",
        message = "This is your CLUCK BUCKS! You earn them by feeding and collecting from your animals.",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Pointing,
        highlightUIByName = "GoldPanel",
        showGameUI = true,
        panelAlpha = 0f,
        waitForAction = false,
        enableShopButton = false
    });
    
    // Explain time controls
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "explain_time",
        title = "Time Controls",
        message = "Pause, play, or speed up time!",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Thinking,
        highlightUIByName = "PAUSE BG",
        showGameUI = true,
        panelAlpha = 0f,
        waitForAction = false,
        enableShopButton = false
    });
    
    // Explain day/night cycle
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "explain_daytime",
        title = "Day & Night",
        message = "Watch this closely — at night time, things get weird...",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Worried,
        highlightUIByName = "DayNightPanel",
        showGameUI = true,
        panelAlpha = 0f,
        waitForAction = false,
        enableShopButton = false
    });
    
    // Explain enemy indicator
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "explain_enemy_indicator",
        title = "Enemy Warning",
        message = "Shows which enemies attack tonight. Keep an eye on it!",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Worried,
        highlightUIByName = "Enemy Indicator",
        showGameUI = true,
        panelAlpha = 0f,
        waitForAction = false,
        enableShopButton = false
    });

    // Explain seasonal bonus
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "explain_production_bonus",
        title = "Production Bonus",
        message = "Seasonal bonuses! These boost the price your animals' products sell for.",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Thinking,
        highlightUIByName = "Animal production bonus indicator",
        showGameUI = true,
        panelAlpha = 0f,
        waitForAction = false,
        enableShopButton = false
    });

    // Explain crop amounts
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "explain_crop_amounts",
        title = "Crop Storage",
        message = "Your crop count — use these seeds to grow food for your animals!",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Pointing,
        highlightUIByName = "CropPanel",
        showGameUI = true,
        panelAlpha = 0f,
        waitForAction = false,
        enableShopButton = false
    });

    // UI learning complete
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "ui_learning_complete",
        title = "Done the Knobs!",
        message = "Nice! You now know how farming works... hopefully. Let's get to building!",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Celebrating,
        showGameUI = true,
        waitForAction = false,
        enableShopButton = false
    });

    // Build: Crop Plot
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "build_crop_plot",
        title = "Build Crop Plot",
        message = "Build a crop plot to plant seeds!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Excited,
        waitForAction = true,
        waitForTrigger = "build_crop_plot",
        movePanelRight = true,
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        enableShopButton = true,
        openShopAutomatically = true,
        highlightShopButton = true,
        restrictShopBuildings = true,
        allowedBuildingNames = new List<string> { "CropPlot", "Crop Plot" },
        requiredShopTab = "P",
        shopTabButtonName = "Plant Button"
    });

    // Build: Chicken Coop
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "build_chicken_coop",
        title = "Build Chicken Coop",
        message = "Chickens need a coop for space to lay eggs!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Excited,
        waitForAction = true,
        waitForTrigger = "build_chicken_coop",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        enableShopButton = true,
        movePanelRight = true,
        restrictShopBuildings = true,
        allowedBuildingNames = new List<string> { "ChickenCoop", "Chicken Coop" },
        requiredShopTab = "C",
        shopTabButtonName = "Coop Button"
    });

    // Build: Chicken Barracks
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "build_chicken_barracks",
        title = "Build Barracks",
        message = "Train army versions of your animals here!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Thinking,
        waitForAction = true,
        waitForTrigger = "build_chicken_barracks",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        enableShopButton = true,
        movePanelRight = true,
        restrictShopBuildings = true,
        allowedBuildingNames = new List<string> { "ChickenBarracks", "Chicken Barracks" },
        requiredShopTab = "A",
        shopTabButtonName = "Army Button"
    });

    // Build Walls
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "build_walls",
        title = "Build Defensive Walls",
        message = "Walls can redirect threats! Click once for a single wall, or click and drag to build a chain. Right-click to cancel. Build at least 3 segments!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Pointing,
        waitForAction = true,
        waitForTrigger = "walls_built",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        enableShopButton = true,
        movePanelRight = true,
        restrictShopBuildings = true,
        allowedBuildingNames = new List<string> { "Hay Bale", "HayBale"},
        requiredShopTab = "S",
        shopTabButtonName = "Defense Button"
    });

        // Build Silo
        tutorialSteps.Add(new SimpleTutorialStep
        {
            stepId = "build_silo",
            title = "Build a Silo",
            message = "Place near crops and animal coops — look for the green indicators!",
            peteContext = PeteContext.CornerBuddy,
            peteEmotion = PeteEmotion.Excited,
            waitForAction = true,
            waitForTrigger = "build_silo",
            panelAlpha = 0f,
            disablePanelRaycast = true,
            showGameUI = true,
            enableShopButton = true,
            movePanelRight = true,
            openShopAutomatically = true,  // Auto-open shop for silo building
            highlightShopButton = true,     // Highlight shop button initially
            restrictShopBuildings = true,
            allowedBuildingNames = new List<string> { "Silo" },
            requiredShopTab = "P",
            shopTabButtonName = "Plant Button"
        });

    
        tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "repair_buildings",
        title = "Damage Control",
        message = "Oh no! Your buildings took damage! Click the Repair tab, then repair all your buildings.",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Worried,
        waitForAction = true,
        waitForTrigger = "all_buildings_repaired",
        openShopAutomatically = true,
        highlightShopButton = false,
        highlightUIByName = "Repair",
        highlightRepairButton = true,
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        enableShopButton = true,
        movePanelDown = true,
        damageAllBuildingsOnStart = true
    });
    // Structures built intro
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "structures_built_intro",
        title = "Competent Builder!",
        message = "Buildings done! Now let's use them!",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Celebrating,
        enableShopButton = false,
        showGameUI = true,
        waitForAction = false,
    });

    // Repair buildings step


    // Plant crop step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "plant_first_crop",
        title = "Plant Sunflowers",
        message = "Click your crop plot and plant sunflowers!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Pointing,
        waitForAction = true,
        waitForTrigger = "crop_planted",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        movePanelDown = true,
        highlightStructureType = "crop plot",
        highlightStructureUIButtons = new List<string> { "plantButton", "plantSunflowerButton" },
        enableShopButton = false
    });

    // Harvest crop step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "harvest_first_crop",
        title = "Harvest Time!",
        message = "Sunflowers ready! I just sped it up for you... Click and harvest!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Excited,
        waitForAction = true,
        waitForTrigger = "crop_harvested",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        movePanelRight = true,
        highlightStructureType = "crop plot",
        highlightStructureUIButtons = new List<string> { "harvestButton" },
        enableShopButton = false
    });

    // Buy chickens step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "buy_chickens",
        title = "Buy Chickens",
        message = "Click your coop and buy 5 chickens!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Pointing,
        waitForAction = true,
        waitForTrigger = "chickens_bought",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        movePanelRight = true,
        highlightStructureType = "chicken coop",
        highlightStructureUIButtons = new List<string> { "buyAnimal" },
        enableShopButton = false
    });

    // Feed chickens step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "feed_chickens",
        title = "Feed Chickens",
        message = "Feed them those sunflowers! Click coop, hit Feed!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Excited,
        waitForAction = true,
        waitForTrigger = "chickens_fed",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        movePanelRight = true,
        highlightStructureType = "chicken coop",
        highlightStructureUIButtons = new List<string> { "feedButton" },
        enableShopButton = false
    });

    // Collect eggs step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "collect_eggs",
        title = "Collect Eggs",
        message = "Eggs = money! Click coop and collect!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Celebrating,
        waitForAction = true,
        waitForTrigger = "eggs_collected",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        movePanelRight = true,
        highlightStructureType = "chicken coop",
        highlightStructureUIButtons = new List<string> { "collectButton" },
        enableShopButton = false
    });

    // Recruit soldiers step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "recruit_soldiers",
        title = "Turn Chickens into Soldiers",
        message = "Click barracks and recruit 3 shotgun-wielding chickens!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Excited,
        waitForAction = true,
        waitForTrigger = "soldiers_recruited",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        movePanelRight = true,
        highlightStructureType = "chicken barrack",
        highlightStructureUIButtons = new List<string> { "recruitButton" },
        enableShopButton = false
    });

    // Place flag step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "place_flag",
        title = "Set Defense Point",
        message = "Click barracks, hit 'Place Flag', pick a guard spot!",
        peteContext = PeteContext.CornerBuddy,
        peteEmotion = PeteEmotion.Pointing,
        waitForAction = true,
        waitForTrigger = "flag_placed",
        panelAlpha = 0f,
        disablePanelRaycast = true,
        showGameUI = true,
        movePanelRight = true,
        highlightStructureType = "chicken barrack",
        highlightStructureUIButtons = new List<string> { "placeFlagButton" },
        enableShopButton = false
    });

    // Final complete step
    tutorialSteps.Add(new SimpleTutorialStep
    {
        stepId = "complete",
        title = "Tutorial Done!",
        message = "You're ready!... Probably not, but good enough! Remember, at night time weird things happen...",
        peteContext = PeteContext.UIHelper,
        peteEmotion = PeteEmotion.Celebrating,
        showGameUI = true,
        enableShopButton = true
    });
}

    // Public helpers so other systems can notify the simplified tutorial about built structures
    public void OnCropPlotBuilt() => TriggerAction("build_crop_plot");
    public void OnSiloBuilt() => TriggerAction("build_silo");
    public void OnChickenCoopBuilt() => TriggerAction("build_chicken_coop");
    public void OnChickenBarracksBuilt() => TriggerAction("build_chicken_barracks");
    public void OnWallsBuilt() => TriggerAction("walls_built");
    
    // Public helpers for crop actions
    public void OnCropPlanted() => TriggerAction("crop_planted");
    public void OnCropHarvested() => TriggerAction("crop_harvested");
    
    // Public helpers for chicken actions
    public void OnChickensBought() => TriggerAction("chickens_bought");
    public void OnChickensFed() => TriggerAction("chickens_fed");
    public void OnEggsCollected() => TriggerAction("eggs_collected");
    
    // Public helpers for army/defense actions
    public void OnSoldiersRecruited() => TriggerAction("soldiers_recruited");
    public void OnFlagPlaced() => TriggerAction("flag_placed");
    
    // Public helper for repair actions
    public void OnAllBuildingsRepaired() => TriggerAction("all_buildings_repaired");
    
    // Public API for shop tutorial restrictions
    public bool ShouldRestrictShopBuildings()
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return false;
            
        return tutorialSteps[currentStepIndex].restrictShopBuildings;
    }
    
    public List<string> GetAllowedBuildingNames()
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return new List<string>();
            
        return tutorialSteps[currentStepIndex].allowedBuildingNames ?? new List<string>();
    }
    
    public bool IsBuildingAllowed(string buildingName)
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return true; // No restrictions when tutorial isn't active
            
        SimpleTutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (!currentStep.restrictShopBuildings)
            return true; // No restrictions for this step
            
        if (currentStep.allowedBuildingNames == null || currentStep.allowedBuildingNames.Count == 0)
            return false; // Restrict all if list is empty but restriction is enabled
            
        // Check if building name matches any allowed names (case-insensitive, flexible matching)
        foreach (string allowed in currentStep.allowedBuildingNames)
        {
            if (string.IsNullOrEmpty(allowed)) continue;
            
            // Exact match (case-insensitive)
            if (buildingName.Equals(allowed, System.StringComparison.OrdinalIgnoreCase))
                return true;
                
            // Partial match (contains)
            if (buildingName.IndexOf(allowed, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                allowed.IndexOf(buildingName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
                
            // Remove spaces and check again
            string buildingNoSpaces = buildingName.Replace(" ", "");
            string allowedNoSpaces = allowed.Replace(" ", "");
            
            if (buildingNoSpaces.Equals(allowedNoSpaces, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false; // Building not in allowed list
    }
    
    public bool ShouldHighlightShopButton()
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return false;
            
        return tutorialSteps[currentStepIndex].highlightShopButton;
    }
    
    public bool ShouldHighlightRepairButton()
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return false;
            
        return tutorialSteps[currentStepIndex].highlightRepairButton;
    }
    
    // ===== WORLD STRUCTURE HIGHLIGHTING API =====
    
    // Check if a specific structure type should be highlighted in the current step
    public bool ShouldHighlightStructure(string structureType)
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return false;
            
        SimpleTutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (string.IsNullOrEmpty(currentStep.highlightStructureType))
            return false;
            
        // Flexible matching (case-insensitive, partial match)
        return structureType.IndexOf(currentStep.highlightStructureType, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               currentStep.highlightStructureType.IndexOf(structureType, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
    
    // Get the next UI button that should be highlighted in the current step
    // Returns null if no more buttons to highlight
    public string GetNextUIButtonToHighlight()
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return null;
            
        SimpleTutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (currentStep.highlightStructureUIButtons == null || currentStep.highlightStructureUIButtons.Count == 0)
            return null;
            
        if (currentUIButtonIndex >= currentStep.highlightStructureUIButtons.Count)
            return null; // All buttons have been highlighted
            
        return currentStep.highlightStructureUIButtons[currentUIButtonIndex];
    }
    
    // Called by structure UI when a button is clicked - advances to next button in sequence
    public void OnStructureUIButtonClicked(string buttonName)
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return;
            
        SimpleTutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        if (currentStep.highlightStructureUIButtons == null || currentUIButtonIndex >= currentStep.highlightStructureUIButtons.Count)
            return;
            
        string expectedButton = currentStep.highlightStructureUIButtons[currentUIButtonIndex];
        
        // Check if this is the button we're expecting
        if (buttonName.Equals(expectedButton, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[SimplifiedTutorialManager] Button '{buttonName}' clicked, advancing to next button in sequence");
            currentUIButtonIndex++;
            
            // Clear current highlight
            ClearUIHighlight();
            
            // If there's another button to highlight, wait a moment then highlight it
            if (currentUIButtonIndex < currentStep.highlightStructureUIButtons.Count)
            {
                string nextButton = currentStep.highlightStructureUIButtons[currentUIButtonIndex];
                Debug.Log($"[SimplifiedTutorialManager] Next button to highlight: {nextButton}");
                // The structure UI will call HighlightUIButton on its next update
            }
            else
            {
                Debug.Log($"[SimplifiedTutorialManager] All UI buttons highlighted for this step");
            }
        }
    }
    
    // Called by structure UI panels when they open to highlight the appropriate button
    public void HighlightStructureUIButton(GameObject buttonObject)
    {
        if (buttonObject == null)
        {
            Debug.LogWarning("[SimplifiedTutorialManager] Cannot highlight null button");
            return;
        }
        
    Debug.Log($"[SimplifiedTutorialManager] Highlighting structure UI button: {buttonObject.name}");
    StartDelayedHighlight(buttonObject, 0.2f);
    }
    
    // Helper method to handle shop tab guidance when tutorial restrictions change
    private void RefreshShopIfOpen()
    {
        if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return;
            
        SimpleTutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        // Check if shop is currently open
        if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen())
        {
            Debug.Log("[RefreshShopIfOpen] Shop is open");
            
            // Check if we need to highlight the repair button
            if (currentStep.highlightRepairButton && ShopPanelUI.Instance != null)
            {
                GameObject repairButton = ShopPanelUI.Instance.GetRepairButton();
                if (repairButton != null)
                {
                    Debug.Log("[RefreshShopIfOpen] Highlighting repair button");
                    StartDelayedHighlight(repairButton, 0.3f);
                }
                else
                {
                    Debug.LogWarning("[RefreshShopIfOpen] Repair button not found!");
                }
            }
            // Check if we need to highlight a tab button
            else if (!string.IsNullOrEmpty(currentStep.requiredShopTab) && ShopPanelUI.Instance != null)
            {
                char requiredTab = currentStep.requiredShopTab[0]; // Get first character ('C', 'P', 'A', 'S')
                
                // Check if player is already on the correct tab
                if (ShopPanelUI.Instance.GetCurrentTab() == requiredTab)
                {
                    Debug.Log($"[RefreshShopIfOpen] Player already on correct tab '{requiredTab}', forcing shop repopulate");
                    // Already on correct tab, just force repopulate
                    ShopPanelUI.Instance.PopulateShop(requiredTab);
                }
                else
                {
                    Debug.Log($"[RefreshShopIfOpen] Player on wrong tab, need to guide to tab '{requiredTab}'");
                    // Player is on wrong tab - find and highlight the button that switches to required tab
                    GameObject tabButton = FindShopTabButton(requiredTab);
                    if (tabButton != null)
                    {
                        Debug.Log($"[RefreshShopIfOpen] Found tab button, highlighting it");
                        StartDelayedHighlight(tabButton, 0.1f);
                    }
                    else
                    {
                        Debug.LogWarning($"[RefreshShopIfOpen] Could not find button for tab: {requiredTab}");
                    }
                }
            }
        }
        else
        {
            Debug.Log("[RefreshShopIfOpen] Shop is not open, no refresh needed");
        }
    }
    
    // Find the shop tab button that switches to the specified tab
    private GameObject FindShopTabButton(char targetTab)
    {
        // Use ShopPanelUI's API to get the correct button
        if (ShopPanelUI.Instance != null)
        {
            GameObject tabButton = ShopPanelUI.Instance.GetTabButton(targetTab);
            if (tabButton != null)
            {
                Debug.Log($"[FindShopTabButton] Found button for tab '{targetTab}': {tabButton.name}");
                return tabButton;
            }
            else
            {
                Debug.LogWarning($"[FindShopTabButton] ShopPanelUI.GetTabButton returned null for tab '{targetTab}'. Make sure the button is assigned in the inspector.");
            }
        }
        else
        {
            Debug.LogError("[FindShopTabButton] ShopPanelUI.Instance is null!");
        }
        
        return null;
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
            // Hide skip button until farmhouse is placed
            skipTutorialButton.gameObject.SetActive(false);
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
        
        // Play button click sound
        
        // Cancel any pending auto-advance from previous step
        if (delayedNextStepCoroutine != null)
        {
            StopCoroutine(delayedNextStepCoroutine);
            delayedNextStepCoroutine = null;
        }
        
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
        
        // Reset structure UI button highlighting sequence for new step
        currentUIButtonIndex = 0;
        
        Debug.Log($"Showing tutorial step {currentStepIndex}: {step.title}");
        
        // Control shop button enabled/disabled state
        if (shopButton != null)
        {
            shopButton.interactable = step.enableShopButton;
            Debug.Log($"Shop button interactable set to: {step.enableShopButton}");
        }
        
        // Close shop when Pete starts explaining the UI (after farmhouse is built)
        if (step.stepId == "explain_money" && ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.CloseShop();
            Debug.Log("Closed shop automatically for UI explanation");
        }
        
        // Close shop before planting first crop to declutter UI
        if (step.stepId == "plant_first_crop" && ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.CloseShop();
            Debug.Log("Closed shop automatically before planting first crop");
        }
        
        // Close shop before "Excellent Work!" step to declutter background
        if (step.stepId == "structures_built_intro" && ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.CloseShop();
            Debug.Log("Closed shop automatically before 'Excellent Work!' step");
        }
        
        // Auto-open shop if this step requires it (e.g., crop plot step after UI explanation)
        if (step.openShopAutomatically && ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OpenShop();
            Debug.Log("Opened shop automatically for tutorial step");
        }
        
        // Damage all buildings if this step requires it (for repair tutorial)
        // Use a coroutine with delay to ensure shop is fully open first
        if (step.damageAllBuildingsOnStart)
        {
            StartCoroutine(DamageBuildingsAfterDelay(0.5f));
        }
        
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
            // Check if this trigger was already completed before this step
            if (completedTriggers.Contains(step.waitForTrigger))
            {
                Debug.Log($"[ShowCurrentStep] Trigger '{step.waitForTrigger}' was already completed! Auto-advancing step.");
                // Trigger was already done, auto-advance after a short delay
                StartCoroutine(DelayedNextStep(0.5f));
                return;
            }
            
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
        
        // NEW: Refresh shop if it's open and this step has building restrictions
        if (step.restrictShopBuildings)
        {
            RefreshShopIfOpen();
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
            progressText.text = $"{currentStepIndex + 1} / {tutorialSteps.Count}";
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
        bool shopOpen = ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen();
        Debug.Log($"[UpdatePanelForStep] Called for step: {step.stepId}, panelAlpha: {step.panelAlpha}, movePanelDown: {step.movePanelDown}, movePanelRight: {step.movePanelRight}, movePanelDownRight: {step.movePanelDownRight}, disableRaycast: {step.disablePanelRaycast}, showGameUI: {step.showGameUI}, shopOpen: {shopOpen}");
        
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
            StartDelayedHighlight(step.highlightUIElement, 0.1f);
        }
        else if (!string.IsNullOrEmpty(step.highlightUIByName))
        {
            // Prefer searching under the main game UI canvas so nested elements (e.g., Coinb canvas/GoldPanel)
            // are found reliably. Fall back to global GameObject.Find if canvas is not assigned or search fails.
            GameObject targetByName = null;
            if (gameUICanvas != null)
            {
                Transform found = FindChildRecursive(gameUICanvas.transform, step.highlightUIByName);
                if (found != null)
                    targetByName = found.gameObject;
            }

            if (targetByName == null)
            {
                // Fallback to global search in the scene (may miss inactive objects)
                targetByName = GameObject.Find(step.highlightUIByName);
            }

            if (targetByName != null)
            {
                StartDelayedHighlight(targetByName, 0.1f);
            }
            else
            {
                Debug.LogWarning($"[SimplifiedTutorialManager] Could not find UI element by name: {step.highlightUIByName}");
            }
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
        bool shopIsOpenNow = ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen();

        // Down+slight-right move takes priority when the step explicitly requests it
        // or when the shop is (or will be) open for this step (openShopAutomatically).
        bool shouldApplyDownRight = step.movePanelDownRight || step.openShopAutomatically || (shopIsOpenNow && step.showGameUI && step.enableShopButton);

        if (shouldApplyDownRight)
        {
            // Slight right and down offset suitable when shop UI is visible
            targetPosition = new Vector2(originalContentPosition.x + 360f, originalContentPosition.y - 335f);
        }
        else if (step.movePanelDown)
        {
            targetPosition = new Vector2(originalContentPosition.x, originalContentPosition.y - 335f);
        }
        else if (step.movePanelRight)
        {
            targetPosition = new Vector2(originalContentPosition.x + 520f, originalContentPosition.y + 260);
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
        Debug.Log($"[TriggerAction] Called with trigger: '{triggerName}', tutorialActive: {tutorialActive}, currentStepIndex: {currentStepIndex}");
        
        // Always add to completed triggers, even if tutorial isn't active or waiting for it
        // This prevents getting stuck if player performs action before tutorial asks for it
        if (!completedTriggers.Contains(triggerName))
        {
            completedTriggers.Add(triggerName);
            Debug.Log($"[TriggerAction] Added '{triggerName}' to completed triggers");
        }
        
        // Allow triggers to be processed even if waitingForPlayerAction wasn't explicitly set.
        if (!tutorialActive)
        {
            Debug.Log($"[TriggerAction] Tutorial not active, but trigger '{triggerName}' recorded for later");
            return;
        }
        
        if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
        {
            Debug.Log($"[TriggerAction] Invalid step index {currentStepIndex} (count: {tutorialSteps.Count}), but trigger '{triggerName}' recorded");
            return;
        }

        var currentStep = tutorialSteps[currentStepIndex];
        Debug.Log($"[TriggerAction] Current step: '{currentStep.stepId}', waiting for: '{currentStep.waitForTrigger}'");

        if (currentStep.waitForTrigger == triggerName)
        {
            Debug.Log($"[TriggerAction] Trigger '{triggerName}' MATCHED! Advancing step.");
            
            // Show skip button after farmhouse is built
            if (triggerName == "farmhouse_built" && skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(true);
                Debug.Log("Skip tutorial button now visible after farmhouse placed");
            }
            
            // Mark trigger completed (already done above) and clear any input UI
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
            delayedNextStepCoroutine = StartCoroutine(DelayedNextStep(0.2f));
        }
        else
        {
            Debug.Log($"[TriggerAction] Trigger '{triggerName}' does NOT match current step's waitForTrigger '{currentStep.waitForTrigger}'. Ignoring.");
        }
    }
    
    private IEnumerator DelayedNextStep(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextStep();
    }
    
    private IEnumerator DamageBuildingsAfterDelay(float delay)
    {
        Debug.Log($"[DamageBuildingsAfterDelay] Starting coroutine with {delay}s delay");
        yield return new WaitForSeconds(delay);
        
        Debug.Log("[DamageBuildingsAfterDelay] Delay complete, looking for DamageBuildings script");
        DamageBuildings damageScript = FindFirstObjectByType<DamageBuildings>();
        if (damageScript != null)
        {
            Debug.Log("[DamageBuildingsAfterDelay] Found DamageBuildings script, calling DamageAllBuildings()");
            damageScript.DamageAllBuildings();
            Debug.Log("Damaged all buildings for repair tutorial step");
            
            // Refresh shop to show repair button after damage
            if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen())
            {
                Debug.Log("[DamageBuildingsAfterDelay] Shop is open, waiting 0.2s then refreshing");
                yield return new WaitForSeconds(0.2f); // Wait a bit for damage to register
                RefreshShopIfOpen();
            }
            else
            {
                Debug.LogWarning("[DamageBuildingsAfterDelay] Shop is not open!");
            }
        }
        else
        {
            Debug.LogWarning("DamageBuildings script not found in scene!");
        }
    }
    
    public void SkipTutorial()
    {
        // Play button click sound
        
        StopMumbling(); // Stop Pete's speaking
        EndTutorial();
    }
    
    private void EndTutorial()
    {
        tutorialActive = false;
        waitingForPlayerAction = false;
        
        // Save tutorial completion to PlayerPrefs so it doesn't restart on game load
        PlayerPrefs.SetInt("SimplifiedTutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log("[SimplifiedTutorialManager] Tutorial completed and saved to PlayerPrefs");
        
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
        // Stop any pending delayed highlight so old coroutines don't re-create highlights
        if (delayedHighlightCoroutine != null)
        {
            StopCoroutine(delayedHighlightCoroutine);
            delayedHighlightCoroutine = null;
        }

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

    // Helper to start a delayed highlight and cancel any previous pending one
    private void StartDelayedHighlight(GameObject targetElement, float delay)
    {
        if (delayedHighlightCoroutine != null)
        {
            StopCoroutine(delayedHighlightCoroutine);
            delayedHighlightCoroutine = null;
        }

        delayedHighlightCoroutine = StartCoroutine(DelayedHighlight(targetElement, delay));
    }

    // Recursive search helper to locate a child by name under a given parent transform.
    // This helps find nested UI elements (for example: "Coinb canvas" -> "GoldPanel").
    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null || string.IsNullOrEmpty(targetName))
            return null;

        if (parent.name == targetName)
            return parent;

        foreach (Transform child in parent)
        {
            var found = FindChildRecursive(child, targetName);
            if (found != null)
                return found;
        }

        return null;
    }
    
    // Public methods for other systems to call
    public void OnShopOpened() => TriggerAction("shop_opened");
    public void OnFarmhouseBuilt() => TriggerAction("farmhouse_built");
    
    // Called when player clicks a shop tab button
    public void OnShopTabChanged(char newTab)
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return;
            
        SimpleTutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        // Check if this step requires a specific shop tab
        if (!string.IsNullOrEmpty(currentStep.requiredShopTab))
        {
            // Convert char to string for comparison
            string newTabStr = newTab.ToString();
            
            Debug.Log($"[OnShopTabChanged] Player switched to tab '{newTab}', required: '{currentStep.requiredShopTab}'");
            
            // If player switched to the correct tab, clear the tab button highlight
            if (newTabStr == currentStep.requiredShopTab)
            {
                Debug.Log("[OnShopTabChanged] Correct tab selected! Clearing highlight.");
                ClearUIHighlight();
                
                // Force shop to repopulate with tutorial restrictions
                if (ShopPanelUI.Instance != null)
                {
                    ShopPanelUI.Instance.PopulateShop(newTab);
                }
            }
        }
        
        // Special case: If this is the repair buildings step and player opened Repair tab
        if (currentStep.stepId == "repair_buildings" && currentStep.highlightRepairButton)
        {
            Debug.Log("[OnShopTabChanged] Repair step detected, highlighting repair button");
            ClearUIHighlight();
            
            // Highlight the repair all button after a short delay
            if (ShopPanelUI.Instance != null)
            {
                GameObject repairButton = ShopPanelUI.Instance.GetRepairButton();
                if (repairButton != null)
                {
                    StartDelayedHighlight(repairButton, 0.3f);
                }
            }
        }
    }
    
    // Called when player switches to Repair view in shop
    public void OnRepairViewOpened()
    {
        if (!tutorialActive || currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count)
            return;
            
        SimpleTutorialStep currentStep = tutorialSteps[currentStepIndex];
        
        // If this is the repair buildings step, highlight the repair all button
        if (currentStep.stepId == "repair_buildings" && currentStep.highlightRepairButton)
        {
            Debug.Log("[OnRepairViewOpened] Highlighting repair all button");
            ClearUIHighlight();
            
            // Highlight the repair all button after a short delay
            if (ShopPanelUI.Instance != null)
            {
                GameObject repairButton = ShopPanelUI.Instance.GetRepairButton();
                if (repairButton != null)
                {
                    StartDelayedHighlight(repairButton, 0.5f);
                }
            }
        }
    }
    
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