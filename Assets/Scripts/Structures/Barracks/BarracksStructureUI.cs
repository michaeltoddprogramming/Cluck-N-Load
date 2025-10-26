using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BarracksStructureUI : BaseStructureUI
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI armyCountText;
    [SerializeField] private Button recruitButton;
    [SerializeField] private Button placeFlagButton;
    [SerializeField] private Button setFlagColorButton;
    [SerializeField] private GameObject flagPlacementIndicator;
    [SerializeField] private GameObject flagGhostPrefab; // Ghost prefab for flag placement preview
    [SerializeField] private Material flagGhostMaterial; // Optional: Material to apply to ghost flag
    [SerializeField] private Button addAnimal;
    [SerializeField] private Button minusAnimal;
    [SerializeField] private TextMeshProUGUI animalCountText;
    [SerializeField] private TextMeshProUGUI costText;

    [Header("UI Control")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    
    // Optimization: Only run Update() when UI is visible
    private bool isUIVisible = false;

    [Header("Recruitment Warning")]
    [SerializeField] private GameObject recruitmentWarningPanel;
    [SerializeField] private TextMeshProUGUI recruitWarningText;
    [SerializeField] private Button confirmRecruitButton;
    [SerializeField] private Button cancelRecruitButton;

    private BarracksStructure barracksStructure;
    private bool isBarracksStructure = false;
    private bool isPlacingFlag = false;
    private GameObject currentFlagGhost; // Current ghost flag instance during placement
    private int newAnimalCount = 0;
    private int animalCount = 0;
    private int maxAnimalCount = 0;
    private System.Action pendingRecruitAction;
    private string lastHighlightedButton = null; // Track which button we're currently highlighting
    // Note: lastPauseState is inherited from BaseStructureUI

    // Public property to check if this barracks is currently placing a flag
    public bool IsPlacingFlag => isPlacingFlag;

    [SerializeField] public Image animalIcon1;
    [SerializeField] public Image animalIcon2;
    [SerializeField] public Sprite cowIcon;
    [SerializeField] public Sprite chickenIcon;
    [SerializeField] public Sprite goatIcon;
    [SerializeField] public Sprite pigIcon;
    [SerializeField] public Sprite sheepIcon;

    [Header("Sheep Flag Management")]
    [SerializeField] private Button manageSheepFlagsButton;
    [SerializeField] private GameObject sheepFlagManagementPanel;
    [SerializeField] private Transform sheepFlagListParent;
    [SerializeField] private GameObject sheepFlagItemPrefab;
    [SerializeField] private Button closeSheepFlagPanelButton;
    [SerializeField] private AudioSource buttonClick;
    [SerializeField] private AudioSource closeClicked;
    
    private bool isManagingSheepFlags = false;
    private int selectedSheepFlagIndex = -1;
    private bool isMovingSheepFlag = false;

    [Header("Army indicator")]
    // [SerializeField] public Image animalIcon3;
    [SerializeField] private Slider armyBarSlider;
    [SerializeField] private Image armyBarFill;

    [Header("Civilian indicator")]
    [SerializeField] public Image animalIcon3;
    [SerializeField] private Slider civilianBarSlider;
    [SerializeField] private Image civilianBarFill;
    [SerializeField] private TextMeshProUGUI civilianText;

    [Header("Stats Display")]
    [SerializeField] public GameObject[] healthAmount;
    [SerializeField] public GameObject[] damageAmount;
    [SerializeField] public GameObject[] rangeAmount;
    [SerializeField] public GameObject[] speedAmount;

    // private BarracksStructure barrackStructure;

    // public UIHoverManager hoverManager;
    [SerializeField] public GameObject civilianSection;

    [Header("Synergy indicator")]
    [SerializeField] private Image synergyIndicator;
    [SerializeField] private Sprite chickenSynergyGood;
    [SerializeField] private Sprite chickenSynergyBad;
    [SerializeField] private Sprite pigSynergyGood;
    [SerializeField] private Sprite pigSynergyBad;
    [SerializeField] private Sprite cowSynergyGood;
    [SerializeField] private Sprite cowSynergyBad;
    [SerializeField] private Sprite goatSynergyGood;
    [SerializeField] private Sprite goatSynergyBad;
    [SerializeField] private Sprite sheepSynergyGood;
    [SerializeField] private Sprite sheepSynergyBad;

    private new void Start()
    {
        base.Start();
        // hoverManager = FindObjectOfType<UIHoverManager>();
    }

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);
        barracksStructure = structure as BarracksStructure;
        isBarracksStructure = barracksStructure != null;

        // Auto-find CanvasGroup if not assigned
        if (uiCanvasGroup == null)
        {
            uiCanvasGroup = GetComponent<CanvasGroup>();
            if (uiCanvasGroup == null)
            {
                Debug.LogWarning($"BarracksStructureUI on {gameObject.name}: No CanvasGroup found. UI hiding during flag placement will not work properly. Please add a CanvasGroup component or assign the uiCanvasGroup field.");
            }
        }

        if (!isBarracksStructure)
        {
            HideBarracksUI();
            return;
        }

        barracksStructure.OnArmyChanged += UpdateUI;
        barracksStructure.OnArmyChanged += () => {
            RefreshSheepFlagListIfOpen();
        }; // Auto-update sheep flag panel

        // Trigger tutorial when barracks UI is opened for the first time
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            TutorialManager.Instance.Trigger(TutorialTrigger.BarracksUIOpened);
        }

        setUpStats();

        if (recruitButton != null)
        {
            recruitButton.onClick.RemoveAllListeners();
            recruitButton.onClick.AddListener(() =>
            {
                if (SimplifiedTutorialManager.Instance != null)
                    SimplifiedTutorialManager.Instance.OnStructureUIButtonClicked("recruitButton");
                RecruitAnimalsWithWarningCheck();
                UpdateUI();
            });
        }
        else
        {
            Debug.LogError("Recruit Button is not assigned in BarracksStructureUI!");
        }

        if (placeFlagButton != null)
        {
            placeFlagButton.onClick.RemoveAllListeners();
            placeFlagButton.onClick.AddListener(() =>
            {
                if (SimplifiedTutorialManager.Instance != null)
                    SimplifiedTutorialManager.Instance.OnStructureUIButtonClicked("placeFlagButton");
                StartFlagPlacement();
            });
        }

        if (setFlagColorButton != null)
        {
            setFlagColorButton.onClick.RemoveAllListeners();
            setFlagColorButton.onClick.AddListener(() =>
            {
                Color[] presetColors = { Color.red, Color.blue, Color.green, Color.yellow, Color.white };
                Color currentColor = barracksStructure.GetFlagColor;
                int currentIndex = System.Array.IndexOf(presetColors, currentColor);
                Color newColor = presetColors[(currentIndex + 1) % presetColors.Length];
                barracksStructure.SetFlagColor(newColor);
                UpdateUI();
            });
        }

        // Setup sheep flag management buttons
        if (manageSheepFlagsButton != null)
        {
            manageSheepFlagsButton.onClick.RemoveAllListeners();
            manageSheepFlagsButton.onClick.AddListener(OpenSheepFlagManagement);
        }

        if (closeSheepFlagPanelButton != null)
        {
            closeSheepFlagPanelButton.onClick.RemoveAllListeners();
            closeSheepFlagPanelButton.onClick.AddListener(CloseSheepFlagManagement);
        }

        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(false);
        }

        UpdateUI();

        barracksStructure.playBackgroundSound();

        SetupButtonListeners();

        animalCount = barracksStructure.GetAnimalCount();
        maxAnimalCount = barracksStructure.GetMaxAnimalCount();
    }

    private float lastUIUpdate;
    private const float UI_UPDATE_INTERVAL = 0.5f; // Update UI twice per second

    protected override void Update()
    {
        base.Update();
        
        // Tutorial button highlighting
        HighlightTutorialButton();
        
        // OPTIMIZATION: Skip Update() if UI is not visible (hidden barracks don't need updates)
        // EXCEPTION: Keep running if placing flags, managing sheep flags, or moving sheep flags (need input handling)
        if (!isUIVisible && !isPlacingFlag && !isMovingSheepFlag && !isManagingSheepFlags)
            return;
        
        // Call base update to handle move button logic
        
        // Check for pause state changes and update UI immediately
        NightManager nightManager = NightManager.Instance;
        if (nightManager != null)
        {
            bool currentPauseState = nightManager.getIsPaused();
            if (currentPauseState != lastPauseState)
            {
                lastPauseState = currentPauseState;
                UpdateUI(); // Update immediately when pause state changes
            }
        }
        
        if (Time.time - lastUIUpdate > UI_UPDATE_INTERVAL)
        {
            UpdateUI();
            
            // Refresh sheep flag panel more frequently if it's open
            if (isManagingSheepFlags && sheepFlagManagementPanel != null && sheepFlagManagementPanel.activeInHierarchy)
            {
                RefreshSheepFlagList();
            }
            
            lastUIUpdate = Time.time;
        }

        // Handle sheep flag movement input first (takes priority)
        if (isMovingSheepFlag)
        {
            HandleSheepFlagMovementInput();
            UpdateSheepFlagMovementIndicator();
        }
        // Handle regular flag placement input only if not moving sheep flags
        else if (isPlacingFlag)
        {
            HandleFlagPlacementInput();
            UpdateFlagPlacementIndicator();
        }

        // Additional safeguard for sheep flag button at night
        if (barracksStructure != null && barracksStructure.GetAnimalType() == "Sheep")
        {
            if (nightManager != null && !nightManager.IsDay && placeFlagButton != null)
            {
                placeFlagButton.interactable = false;
                Debug.LogWarning("[Sheep Barracks UI] Force-disabled placeFlagButton at night");
            }
        }
    }

    // Removed Update() method for better performance - using event-driven updates instead

    private void SetupButtonListeners()
    {
        if (addAnimal != null)
        {
            addAnimal.onClick.RemoveAllListeners();
            addAnimal.onClick.AddListener(() =>
            {
                // Check if action is allowed before proceeding
                if ((newAnimalCount + animalCount) >= maxAnimalCount)
                {
                    // Play error sound for max capacity reached
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayErrorSound();
                    }
                    updateStatusText("Maximum army capacity reached!");
                    return;
                }
                
                if (!MoneyManager.Instance.CanAfford((newAnimalCount + 1) * barracksStructure.GetAnimalRecruitPrice()))
                {
                    // Play insufficient funds sound
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayInsufficientFundsSound();
                    }
                    updateStatusText("Cannot afford more animals!");
                    // NEW: Show notification for insufficient funds
                    if (NotificationManager.Instance != null)
                    {
                        int cost = (newAnimalCount + 1) * barracksStructure.GetAnimalRecruitPrice();
                        NotificationManager.ShowError("Can't Recruit!", $"Need ${cost} to recruit more animals");
                    }
                    return;
                }
                
                if (!barracksStructure.CanRecruit(newAnimalCount + 1))
                {
                    // Play error sound for recruitment limitations
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayErrorSound();
                    }
                    updateStatusText("Cannot recruit more animals!");
                    return;
                }
                
                // animalStructure.Feed();
                animalChange(0);
                UpdateUI();
            });
        }
        else
        {
        }
        if (minusAnimal != null)
        {
            minusAnimal.onClick.RemoveAllListeners();
            minusAnimal.onClick.AddListener(() =>
            {
                // Check if action is allowed before proceeding
                if (newAnimalCount <= 0)
                {
                    // Play error sound for no animals to remove
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayErrorSound();
                    }
                    return;
                }
                
                // animalStructure.Feed();
                animalChange(1);
                UpdateUI();
            });
        }
        else
        {
        }
        // if (recruitButton != null)
        // {
        //     recruitButton.onClick.RemoveAllListeners();
        //     recruitButton.onClick.AddListener(() =>
        //     {
        //         // animalStructure.Feed();
        //         UpdateUI();
        //     });
        // }
        // else
        // {
        // }
    }

    private void StartFlagPlacement()
    {
        if (!isBarracksStructure || barracksStructure.ArmyAnimalCount <= 0) return;

        // Check if it's sheep and if it's nighttime - prevent flag placement
        if (barracksStructure.GetAnimalType() == "Sheep")
        {
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && !nightManager.IsDay)
            {
                updateStatusText("Sheep flags can only be placed during the day");
                // Play error sound
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayErrorSound();
                }
                // Optional: Add audio feedback
                if (placeFlagButton != null)
                {
                    placeFlagButton.interactable = false;
                }
                return; // Exit early, don't start flag placement
            }
        }

        isPlacingFlag = true;
        
        // Hide UI during flag placement
        HideUI();
        
        if (recruitButton != null) recruitButton.interactable = false;
        if (setFlagColorButton != null) setFlagColorButton.interactable = false;
        if (placeFlagButton != null)
        {
            placeFlagButton.interactable = false;
            var buttonText = placeFlagButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                // buttonText.text = "Placing...";
            }
        }
        if (flagPlacementIndicator != null) flagPlacementIndicator.SetActive(true);
        
        // Create flag ghost for preview
        CreateFlagGhost();
    }

    private void HandleFlagPlacementInput()
    {
        // If we're moving a sheep flag, don't handle regular flag placement
        if (isMovingSheepFlag)
        {
            return;
        }

        // Check for night start cancellation for sheep
        if (barracksStructure.GetAnimalType() == "Sheep")
        {
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && !nightManager.IsDay)
            {
                // Play error sound for night cancellation
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayErrorSound();
                }
                CancelFlagPlacement("Night started - sheep flag placement cancelled");
                return;
            }
        }

        // Handle mouse input
        if (Input.GetMouseButtonDown(0)) // Left click to place flag
        {
            PlaceFlagAtMousePosition();
        }
        else if (Input.GetMouseButtonDown(1)) // Right click to cancel
        {
            CancelFlagPlacement("Flag placement cancelled");
        }
    }

    private void UpdateFlagPlacementIndicator()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");
        
        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            // Update flag placement indicator if it exists
            if (flagPlacementIndicator != null)
            {
                Vector3 position = hit.point;
                position.y += 0.1f;
                flagPlacementIndicator.transform.position = position;
            }
            
            // Update flag ghost position
            UpdateFlagGhostPosition(hit.point);
        }
        else
        {
            // Hide ghost when raycast doesn't hit anything
            if (currentFlagGhost != null && currentFlagGhost.activeSelf)
            {
                currentFlagGhost.SetActive(false);
            }
        }
    }

    private void PlaceFlagAtMousePosition()
    {
        // If we're moving a sheep flag, handle it separately
        if (isMovingSheepFlag)
        {
            MoveSheepFlagToMousePosition();
            return;
        }

        // Make sure we don't place on UI elements
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // Play error sound for trying to place on UI
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            return; // Don't place on UI
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            // Final check before placing the flag for sheep
            if (barracksStructure.GetAnimalType() == "Sheep")
            {
                NightManager nightManager = NightManager.Instance;
                if (nightManager != null && !nightManager.IsDay)
                {
                    // Play error sound for attempted night placement
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayErrorSound();
                    }
                    CancelFlagPlacement("Cannot place sheep flags at night");
                    return;
                }
            }

            barracksStructure.PlaceFlag(hit.point);
            EndFlagPlacement("Flag placed successfully!");
        }
        else
        {
            // Play error sound for invalid placement location
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
        }
    }

    private void CancelFlagPlacement(string message = "Flag placement cancelled")
    {
        EndFlagPlacement(message);
    }

    private void EndFlagPlacement(string message = "")
    {
        // Don't end flag placement if we're moving a sheep flag
        if (isMovingSheepFlag)
        {
            return;
        }

        isPlacingFlag = false;
        
        // Show UI when ending flag placement
        ShowUI();
        
        if (flagPlacementIndicator != null)
        {
            flagPlacementIndicator.SetActive(false);
        }
        
        // Destroy flag ghost
        DestroyFlagGhost();
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (!isBarracksStructure || barracksStructure == null) return;

        updateSynergyIndicator();

        animalCountText.text = $"{newAnimalCount}";

        animalCount = barracksStructure.GetAnimalCount();
        maxAnimalCount = barracksStructure.GetMaxAnimalCount();

        bool canRecruit = barracksStructure.CanRecruit(newAnimalCount);
        bool hasArmy = barracksStructure.ArmyAnimalCount > 0;
        
        // Check if game is paused
        NightManager nightManager = NightManager.Instance;
        bool isPaused = nightManager != null && nightManager.getIsPaused();

        updateStatusBars();

        // --- CIVILIAN BAR ---
        // if (civilianBarSlider != null && civilianText != null)
        // {
        //     int civilianCount = barracksStructure.GetAvailableCivilians();   // You need this method in BarracksStructure
        //     int civilianMax = barracksStructure.GetMaxCivilians(); // Or reuse existing maxAnimalCount if same

        //     // civilianBarSlider.maxValue = civilianMax;
        //     // civilianBarSlider.value = civilianCount;

        //     civilianText.text = $"{civilianCount}/{civilianMax}";

        //     // Optional: change fill color based on % full
        //     float fillPercent = (float)civilianCount / Mathf.Max(1, civilianMax);
        //     civilianBarSlider.value = fillPercent;
        //     // if (civilianBarFill != null)
        //     // {
        //     //     if (fillPercent < 0.33f) civilianBarFill.color = Color.red;
        //     //     else if (fillPercent < 0.66f) civilianBarFill.color = Color.yellow;
        //     //     else civilianBarFill.color = Color.green;
        //     // }
        // }



        if (statusText != null)
        {
            if (isMovingSheepFlag)
            {
                // Don't override the sheep flag movement status text
                // Keep the existing message
            }
            else if (isPlacingFlag)
            {
                statusText.text = "Click anywhere to place flag";
            }
            else if (barracksStructure.GetTargetStructure != null)
            {
                AnimalStructure target = barracksStructure.GetTargetStructure;
                statusText.text = $"Animals: {target.AnimalCount}/{target.MaxAnimalCount}\n" +
                                  $"Army: {barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
                statusText.color = Color.white;
            }
            else
            {
                statusText.text = $"No {barracksStructure.GetAnimalType()}s nearby!";
                statusText.color = Color.yellow;
            }
        }

        if (armyCountText != null)
        {
            armyCountText.text = $"{barracksStructure.ArmyAnimalCount}/{barracksStructure.MaxArmyAnimals}";
            // armyCountText.color = hasArmy ? Color.green : Color.white;
        }

        if (recruitButton != null && !isPlacingFlag)
        {
            recruitButton.interactable = canRecruit && !isPaused;
            // TextMeshProUGUI buttonText = recruitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (costText != null)
            {
                int cost = barracksStructure.GetRecruitmentCost() * newAnimalCount;
                costText.text = cost.ToString();
            }
        }

        if (placeFlagButton != null && !isPlacingFlag)
        {
            placeFlagButton.interactable = hasArmy && !isPaused;
        }

        if (setFlagColorButton != null && !isPlacingFlag)
        {
            setFlagColorButton.interactable = true;
        }

        if (addAnimal != null)
        {
            // ADD button should disable when clicking it would make the TOTAL SELECTED exceed 3
            bool tutorialAddRestriction = false;
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                if (!TutorialManager.Instance.GetCompletedStepIds().Contains("recruit_soldiers"))
                {
                    int currentArmyCount = barracksStructure.ArmyAnimalCount;
                    // Disable ADD button when total selected would exceed what we can recruit (3 - current owned)
                    int maxCanRecruit = 3 - currentArmyCount;
                    if (newAnimalCount >= maxCanRecruit)
                    {
                        tutorialAddRestriction = true;
                    }
                }
            }
            
            if ((newAnimalCount + animalCount) < maxAnimalCount && MoneyManager.Instance.CanAfford(newAnimalCount + 1 * barracksStructure.GetAnimalRecruitPrice()) && barracksStructure.CanRecruit(newAnimalCount + 1) && !isPaused && !tutorialAddRestriction)
            {
                addAnimal.interactable = true;
            }
            else
            {
                addAnimal.interactable = false;
            }
        }

        if (minusAnimal != null)
        {
            if (newAnimalCount > 0 && !isPaused)
            {
                minusAnimal.interactable = true;
            }
            else
            {
                minusAnimal.interactable = false;
            }
        }

        if (recruitButton != null)
        {
            // Tutorial logic: disable RECRUIT button only when we already OWN 3 army animals
            bool tutorialRecruitRestriction = false;
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
            {
                if (!TutorialManager.Instance.GetCompletedStepIds().Contains("recruit_soldiers"))
                {
                    int currentArmyCount = barracksStructure.ArmyAnimalCount;
                    // During recruit_soldiers tutorial step, disable RECRUIT button only when we already OWN 3 animals
                    if (currentArmyCount >= 3)
                    {
                        tutorialRecruitRestriction = true;
                    }
                }
            }
            
            if (newAnimalCount > 0 && MoneyManager.Instance.CanAfford(newAnimalCount * barracksStructure.GetAnimalRecruitPrice()) && !isPaused && !tutorialRecruitRestriction)
            {
                recruitButton.interactable = true;
            }
            else
            {
                recruitButton.interactable = false;
            }
        }

        if (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(newAnimalCount * barracksStructure.GetAnimalRecruitPrice()))
        {
            updateStatusText($"Cannot afford {maxAnimalCount} many animals!");
            // Don't play error sound here - this is just UI state update, not user action
        }

        // Add day/night check for sheep flag placement
        if (barracksStructure.GetAnimalType() == "Sheep")
        {
            bool isDay = nightManager != null ? nightManager.IsDay : true;

            if (placeFlagButton != null)
            {
                bool canPlaceFlags = isDay && hasArmy;
                
                // For sheep, also check if more flags can be placed
                if (barracksStructure.GetAnimalType() == "Sheep" && canPlaceFlags)
                {
                    canPlaceFlags = barracksStructure.CanPlaceMoreSheepFlags();
                }
                
                placeFlagButton.interactable = canPlaceFlags && !isPaused;

                if (isPaused && statusText != null)
                {
                    updateStatusText("Cannot place flags while game is paused");
                    // Don't play error sound here - this is just UI state update, not user action
                }
                else if (!isDay && statusText != null)
                {
                    updateStatusText("Sheep flags can only be placed during the day");
                }
                else if (isDay && statusText != null && !hasArmy)
                {
                    updateStatusText("No sheep army to place flags for");
                }
                else if (isDay && hasArmy && statusText != null)
                {
                    // Check if all sheep have individual flags
                    int sheepCount = barracksStructure.GetMaxSheepFlags();
                    int flagCount = barracksStructure.GetSheepFlagCount();
                    
                    if (flagCount >= sheepCount && sheepCount > 0)
                    {
                        updateStatusText($"All {sheepCount} sheep have individual flags placed!");
                    }
                    else if (flagCount > 0)
                    {
                        updateStatusText($"Ready to place sheep flags ({flagCount}/{sheepCount} placed)");
                    }
                    else
                    {
                        updateStatusText("Ready to place sheep flags");
                    }
                }
            }
        }
        else
        {
            // For non-sheep animals, flags can be placed anytime (existing behavior)
            if (placeFlagButton != null)
            {
                placeFlagButton.interactable = hasArmy && !isPaused;
            }
        }

        if (animalIcon1 != null)
        {
            if (barracksStructure.GetAnimalType() == "Cow")
            {
                animalIcon1.sprite = cowIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Chicken")
            {
                animalIcon1.sprite = chickenIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Goat")
            {
                animalIcon1.sprite = goatIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Pig")
            {
                animalIcon1.sprite = pigIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Sheep")
            {
                animalIcon1.sprite = sheepIcon;
            }
        }

        if (animalIcon3 != null)
        {
            if (barracksStructure.GetAnimalType() == "Cow")
            {
                animalIcon3.sprite = cowIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Chicken")
            {
                animalIcon3.sprite = chickenIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Goat")
            {
                animalIcon3.sprite = goatIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Pig")
            {
                animalIcon3.sprite = pigIcon;
            }
            else if (barracksStructure.GetAnimalType() == "Sheep")
            {
                animalIcon3.sprite = sheepIcon;
            }
        }

        // if (animalIcon2 != null)
        // {
        //     if (barracksStructure.GetAnimalType() == "Cow")
        //     {
        //         animalIcon2.sprite = cowIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Chicken")
        //     {
        //         animalIcon2.sprite = chickenIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Goat")
        //     {
        //         animalIcon2.sprite = goatIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Pig")
        //     {
        //         animalIcon2.sprite = pigIcon;
        //     }
        //     else if (barracksStructure.GetAnimalType() == "Sheep")
        //     {
        //         animalIcon2.sprite = sheepIcon;
        //     }
        // }

        // Update sheep flag management button visibility and state
        if (manageSheepFlagsButton != null)
        {
            bool isSheepBarracks = barracksStructure.GetAnimalType() == "Sheep";
            manageSheepFlagsButton.gameObject.SetActive(isSheepBarracks);
            
            if (isSheepBarracks)
            {
                bool hasFlags = barracksStructure.GetSheepFlagCount() > 0;
                manageSheepFlagsButton.interactable = hasFlags && !isPaused;
                
                // Update button text to show flag count
                TextMeshProUGUI buttonText = manageSheepFlagsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    int flagCount = barracksStructure.GetSheepFlagCount();
                    int maxFlags = barracksStructure.GetMaxSheepFlags();
                    buttonText.text = $"MANAGE FLAGS\\n({flagCount}/{maxFlags})";
                }
            }
        }

        // Hide sheep flag management panel if not a sheep barracks
        if (sheepFlagManagementPanel != null && barracksStructure.GetAnimalType() != "Sheep")
        {
            sheepFlagManagementPanel.SetActive(false);
            isManagingSheepFlags = false;
        }
    }

    private void updateStatusText(string message)
    {
        // Don't override status text if we're moving a sheep flag
        if (isMovingSheepFlag)
        {
            return;
        }
        
        // Update status text
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = Color.yellow;
        }
    }

    private void HideBarracksUI()
    {

        if (statusText != null)
        {
            statusText.text = "Not a barracks structure";
            statusText.color = Color.yellow;
        }
        if (armyCountText != null) armyCountText.gameObject.SetActive(false);
        if (recruitButton != null) recruitButton.gameObject.SetActive(false);
        if (placeFlagButton != null) placeFlagButton.gameObject.SetActive(false);
        if (setFlagColorButton != null) setFlagColorButton.gameObject.SetActive(false);
        if (flagPlacementIndicator != null) flagPlacementIndicator.SetActive(false);
        if (manageSheepFlagsButton != null) manageSheepFlagsButton.gameObject.SetActive(false);
        if (sheepFlagManagementPanel != null) sheepFlagManagementPanel.SetActive(false);

    }

    protected override void OnDestroy()
    {
        if (isBarracksStructure && barracksStructure != null)
        {
            barracksStructure.OnArmyChanged -= UpdateUI;
            barracksStructure.stopBackgroundSound();
        }

        // Clean up flag ghost if it exists
        DestroyFlagGhost();

        // Call base OnDestroy
        base.OnDestroy();
    }

    private void animalChange(int flag)
    {
        if (flag == 0)
        {
            newAnimalCount += 1;
        }
        else if (flag == 1 && newAnimalCount > 0)
        {
            newAnimalCount -= 1;
        }
    }

    // private void BuyAnimals()
    // {
    //     if (newAnimalCount > 0)
    //     {
    //         animalStructure.AddAnimals(newAnimalCount);
    //         newAnimalCount = 0;
    //     }
    // }

    private void recruitAnimals()
    {
        if (newAnimalCount > 0)
        {
            barracksStructure.RecruitAnimals(newAnimalCount);
            newAnimalCount = 0;
        }
    }

    // Enhanced recruitment with production impact warning
    public void RecruitAnimalsWithWarningCheck()
    {
        if (newAnimalCount <= 0) 
        {
            // Play error sound for no animals to recruit
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            return;
        }
        
        // Check if we can afford the recruitment
        if (!MoneyManager.Instance.CanAfford(newAnimalCount * barracksStructure.GetAnimalRecruitPrice()))
        {
            // Play insufficient funds sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
            }
            updateStatusText("Cannot afford recruitment!");
            // NEW: Show notification for insufficient funds
            if (NotificationManager.Instance != null)
            {
                int cost = newAnimalCount * barracksStructure.GetAnimalRecruitPrice();
                NotificationManager.ShowError("Can't Recruit Army!", $"Need ${cost} for recruitment");
            }
            return;
        }
        
        // Check if recruitment would impact production
        AnimalStructure targetAnimal = barracksStructure.GetTargetStructure;
        if (targetAnimal != null && targetAnimal.CanRecruit(newAnimalCount, out string impactWarning))
        {
            if (!string.IsNullOrEmpty(impactWarning))
            {
                ShowRecruitmentWarningPanel(impactWarning, () => recruitAnimals());
            }
            else
            {
                recruitAnimals(); // No warning needed
            }
        }
        else
        {
            // Play error sound for recruitment failure
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            updateStatusText("Cannot recruit animals at this time!");
        }
    }

    // Recruitment warning panel system
    private void ShowRecruitmentWarningPanel(string warningMessage, System.Action onConfirm)
    {
        if (recruitmentWarningPanel == null) return;
        
        pendingRecruitAction = onConfirm;
        recruitWarningText.text = warningMessage;
        recruitmentWarningPanel.SetActive(true);
        
        // Setup button listeners
        confirmRecruitButton?.onClick.RemoveAllListeners();
        cancelRecruitButton?.onClick.RemoveAllListeners();
        
        confirmRecruitButton?.onClick.AddListener(ConfirmRecruitment);
        cancelRecruitButton?.onClick.AddListener(CancelRecruitment);
    }
    
    private void ConfirmRecruitment()
    {
        pendingRecruitAction?.Invoke();
        HideRecruitmentWarningPanel();
    }
    
    private void CancelRecruitment()
    {
        pendingRecruitAction = null;
        HideRecruitmentWarningPanel();
    }
    
    private void HideRecruitmentWarningPanel()
    {
        if (recruitmentWarningPanel != null)
            recruitmentWarningPanel.SetActive(false);
    }

    public void setUpStats()
    {
        hideAllStatAmounts();
        if (barracksStructure.GetAnimalType() == "Chicken")
        {
            healthAmount[0].SetActive(true);

            damageAmount[0].SetActive(true);

            rangeAmount[0].SetActive(true);

            speedAmount[0].SetActive(true);
            speedAmount[1].SetActive(true);
            speedAmount[2].SetActive(true);
            speedAmount[3].SetActive(true);
            speedAmount[4].SetActive(true);
        }
        else if (barracksStructure.GetAnimalType() == "Cow")
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);
            healthAmount[3].SetActive(true);
            healthAmount[4].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            // damageAmount[2].SetActive(true);
            // damageAmount[3].SetActive(true);

            rangeAmount[0].SetActive(true);
            rangeAmount[1].SetActive(true);
            rangeAmount[2].SetActive(true);
            // rangeAmount[3].SetActive(true);

            speedAmount[0].SetActive(true);
            speedAmount[1].SetActive(true);
            // speedAmount[2].SetActive(true);
        }
        else if (barracksStructure.GetAnimalType() == "Pig")
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            damageAmount[2].SetActive(true);

            rangeAmount[0].SetActive(true);
            rangeAmount[1].SetActive(true);

            speedAmount[0].SetActive(true);
            speedAmount[1].SetActive(true);
            speedAmount[2].SetActive(true);
            // speedAmount[3].SetActive(true);
        }
        else if (barracksStructure.GetAnimalType() == "Sheep")
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);
            healthAmount[3].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            damageAmount[2].SetActive(true);
            damageAmount[3].SetActive(true);
            damageAmount[4].SetActive(true);

            rangeAmount[0].SetActive(true);
            // rangeAmount[1].SetActive(true);

            speedAmount[0].SetActive(true);
            // speedAmount[1].SetActive(true);
        }
        else if (barracksStructure.GetAnimalType() == "Goat")
        {
            healthAmount[0].SetActive(true);
            healthAmount[1].SetActive(true);
            healthAmount[2].SetActive(true);
            healthAmount[3].SetActive(true);

            damageAmount[0].SetActive(true);
            damageAmount[1].SetActive(true);
            damageAmount[2].SetActive(true);
            damageAmount[3].SetActive(true);
            // damageAmount[4].SetActive(true);

            rangeAmount[0].SetActive(true);
            rangeAmount[1].SetActive(true);
            rangeAmount[2].SetActive(true);
            rangeAmount[3].SetActive(true);
            rangeAmount[4].SetActive(true);

            speedAmount[0].SetActive(true);
        }
    }

    public void hideAllStatAmounts()
    {
        for (int k = 0; k < healthAmount.Length; k++)
        {
            healthAmount[k].SetActive(false);
        }
        for (int k = 0; k < damageAmount.Length; k++)
        {
            damageAmount[k].SetActive(false);
        }
        for (int k = 0; k < rangeAmount.Length; k++)
        {
            rangeAmount[k].SetActive(false);
        }
        for (int k = 0; k < speedAmount.Length; k++)
        {
            speedAmount[k].SetActive(false);
        }
    }

    public void updateStatusBars()
    {
        if (barracksStructure == null || armyBarSlider == null || civilianBarSlider == null) return;

        int armyCount = barracksStructure.ArmyAnimalCount;
        int armyMax = barracksStructure.MaxArmyAnimals;

        float fillPercent = armyMax > 0 ? (float)armyCount / armyMax : 0f;
        armyBarSlider.value = fillPercent;

        int civilianCount = barracksStructure.GetAvailableCivilians();
        int civilianMax = barracksStructure.GetMaxCivilians();

        float fillPercent2 = civilianMax > 0 ? (float)civilianCount / civilianMax : 0f;
        civilianBarSlider.value = fillPercent2;
        civilianText.text = $"{civilianCount}/{civilianMax}";
    }

    // Static method to check if any barracks is placing a flag
    public static bool IsAnyBarracksPlacingFlag()
    {
        BarracksStructureUI[] allBarracks = FindObjectsByType<BarracksStructureUI>(FindObjectsSortMode.None);
        foreach (var barracks in allBarracks)
        {
            if (barracks.isPlacingFlag)
            {
                return true;
            }
        }
        return false;
    }

    // Building/Upgrade error sound methods
    private void PlayInsufficientFundsForBuildingSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayInsufficientFundsSound();
        }
    }
    
    private void PlayBuildingErrorSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayErrorSound();
        }
    }
    
    // Method to handle any building-related insufficient funds errors
    public void HandleBuildingInsufficientFunds(string buildingAction, int cost)
    {
        PlayInsufficientFundsForBuildingSound();
        updateStatusText($"Cannot afford {buildingAction}! Cost: {cost}");
    }
    
    // Method to handle general building errors
    public void HandleBuildingError(string errorMessage)
    {
        PlayBuildingErrorSound();
        updateStatusText(errorMessage);
    }
    
    // Method to check and handle barracks upgrade costs (for future upgrades)
    public bool TryUpgradeBarracks(int upgradeCost)
    {
        if (MoneyManager.Instance == null) return false;
        
        if (!MoneyManager.Instance.CanAfford(upgradeCost))
        {
            HandleBuildingInsufficientFunds("barracks upgrade", upgradeCost);
            return false;
        }
        
        if (MoneyManager.Instance.SpendMoney(upgradeCost))
        {
            updateStatusText("Barracks upgraded successfully!");
            return true;
        }
        else
        {
            HandleBuildingError("Failed to upgrade barracks!");
            return false;
        }
    }
    
    // Method to check and handle building additional structures costs
    public bool TryBuildAdditionalStructure(string structureName, int buildCost)
    {
        if (MoneyManager.Instance == null) return false;
        
        if (!MoneyManager.Instance.CanAfford(buildCost))
        {
            HandleBuildingInsufficientFunds($"building {structureName}", buildCost);
            return false;
        }
        
        // Note: Actual building logic would be handled elsewhere
        // This just handles the cost checking and error sounds
        return true;
    }

    // Flag ghost management methods
    private void CreateFlagGhost()
    {
        // Clean up any existing ghost first
        DestroyFlagGhost();
        
        // Create ghost if prefab is assigned
        if (flagGhostPrefab != null)
        {
            currentFlagGhost = Instantiate(flagGhostPrefab);
            
            // Apply ghost material if specified
            if (flagGhostMaterial != null)
            {
                Renderer[] renderers = currentFlagGhost.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = flagGhostMaterial;
                    }
                    renderer.materials = materials;
                }
            }
            else
            {
                // If no ghost material specified, make it semi-transparent
                Renderer[] renderers = currentFlagGhost.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material mat = new Material(materials[i]);
                        Color color = mat.color;
                        color.a = 0.5f; // Make it semi-transparent
                        mat.color = color;
                        
                        // Try to enable transparency if the shader supports it
                        if (mat.HasProperty("_Mode"))
                        {
                            mat.SetFloat("_Mode", 3); // Transparent mode
                            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            mat.SetInt("_ZWrite", 0);
                            mat.DisableKeyword("_ALPHATEST_ON");
                            mat.EnableKeyword("_ALPHABLEND_ON");
                            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            mat.renderQueue = 3000;
                        }
                        
                        materials[i] = mat;
                    }
                    renderer.materials = materials;
                }
            }
            
            // Disable colliders on ghost
            Collider[] colliders = currentFlagGhost.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Start with ghost hidden until we have a valid position
            currentFlagGhost.SetActive(false);
        }
    }
    
    private void UpdateFlagGhostPosition(Vector3 worldPosition)
    {
        if (currentFlagGhost != null)
        {
            // Activate ghost if not already active
            if (!currentFlagGhost.activeSelf)
            {
                currentFlagGhost.SetActive(true);
            }
            
            // Position the ghost at the world position
            currentFlagGhost.transform.position = worldPosition;
        }
    }
    
    private void DestroyFlagGhost()
    {
        if (currentFlagGhost != null)
        {
            Destroy(currentFlagGhost);
            currentFlagGhost = null;
        }
    }

    private void HideUI()
    {
        // OPTIMIZATION: Mark UI as hidden to skip Update() loop
        isUIVisible = false;
        
        // Use CanvasGroup to fade out and disable interaction without stopping Update method
        if (uiCanvasGroup != null)
        {
            LeanTween.alphaCanvas(uiCanvasGroup, 0f, 0.2f).setEase(LeanTweenType.easeOutQuad);
            uiCanvasGroup.interactable = false; // Disable clicks immediately
            uiCanvasGroup.blocksRaycasts = false; // Allow clicks to pass through immediately
        }
    }
    
    private void ShowUI()
    {
        // OPTIMIZATION: Mark UI as visible to enable Update() loop
        isUIVisible = true;
        
        // Use CanvasGroup to restore UI visibility and interaction
        if (uiCanvasGroup != null)
        {
            LeanTween.alphaCanvas(uiCanvasGroup, 1f, 0.2f).setEase(LeanTweenType.easeOutQuad);
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }
    }

    // ===== SHEEP FLAG MANAGEMENT METHODS =====
    
    private string GetCustomSheepName(int index)
    {
        string[] sheepNames = { "Larry", "Garry", "Barry", "Jarry", "Harry" };
        if (index >= 0 && index < sheepNames.Length)
        {
            return sheepNames[index];
        }
        // Fallback for more than 5 sheep
        return $"Sheep {index + 1}";
    }
    
    private void OpenSheepFlagManagement()
    {
        if (barracksStructure == null || barracksStructure.GetAnimalType() != "Sheep")
            return;

        try
        {
            isManagingSheepFlags = true;
            
            if (sheepFlagManagementPanel != null)
            {
                sheepFlagManagementPanel.SetActive(true);
                
                // Force immediate refresh
                RefreshSheepFlagList();
                
                // Also wait a frame and refresh again to be extra sure
                StartCoroutine(DelayedRefreshSheepFlagList());
            }
            else
            {
                Debug.LogWarning("Sheep Flag Management Panel is not assigned in the inspector");
                updateStatusText("Sheep flag management panel not configured");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error opening sheep flag management: {e.Message}");
            isManagingSheepFlags = false;
        }
    }

    private System.Collections.IEnumerator DelayedRefreshSheepFlagList()
    {
        yield return null; // Wait one frame
        
        // Ensure the list parent has proper layout components for scrolling
        if (sheepFlagListParent != null)
        {
            // Hide scrollbars if they exist
            ScrollRect scrollRect = sheepFlagListParent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                // Hide vertical scrollbar
                if (scrollRect.verticalScrollbar != null)
                {
                    scrollRect.verticalScrollbar.gameObject.SetActive(false);
                }
                
                // Hide horizontal scrollbar
                if (scrollRect.horizontalScrollbar != null)
                {
                    scrollRect.horizontalScrollbar.gameObject.SetActive(false);
                }
                
                // Keep scrolling enabled but hide scrollbars
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }
            
            // Add VerticalLayoutGroup if it doesn't exist
            VerticalLayoutGroup layoutGroup = sheepFlagListParent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = sheepFlagListParent.gameObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.spacing = 0f; // No spacing between items
                layoutGroup.padding = new RectOffset(0, 0, 0, 0); // No padding
            }
            else
            {
                // Update existing layout group with no spacing
                layoutGroup.spacing = 0f;
                layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            }
            
            // Add ContentSizeFitter if it doesn't exist
            ContentSizeFitter contentSizeFitter = sheepFlagListParent.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = sheepFlagListParent.gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }
        
        RefreshSheepFlagList();
    }

    private void CloseSheepFlagManagement()
    {
        isManagingSheepFlags = false;
        isMovingSheepFlag = false;
        selectedSheepFlagIndex = -1;
        
        if (sheepFlagManagementPanel != null)
        {
            sheepFlagManagementPanel.SetActive(false);
            closeClicked.Play();
        }
    }

    private void RefreshSheepFlagListIfOpen()
    {
        // Only refresh if the panel is currently open
        if (isManagingSheepFlags && sheepFlagManagementPanel != null && sheepFlagManagementPanel.activeInHierarchy)
        {
            RefreshSheepFlagList();
        }
    }
    
    private void RefreshSheepFlagList()
    {
        if (sheepFlagListParent == null || barracksStructure == null)
            return;

        // Clear existing list items safely
        for (int i = sheepFlagListParent.childCount - 1; i >= 0; i--)
        {
            Transform child = sheepFlagListParent.GetChild(i);
            if (child != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Get sheep flag info
        var flagInfoList = barracksStructure.GetSheepFlagInfo();
        
        // Create list items for each sheep flag
        for (int i = 0; i < flagInfoList.Count; i++)
        {
            CreateSheepFlagListItem(flagInfoList[i], i);
        }

        // Show message if no flags placed yet
        if (flagInfoList.Count == 0)
        {
            // No message shown when no flags exist - panel will be empty
        }
        else
        {
            // Adjust content size after creating all items
            StartCoroutine(AdjustContentSizeAfterFrame());
        }
    }
    
    private System.Collections.IEnumerator AdjustContentSizeAfterFrame()
    {
        yield return null; // Wait one frame for all items to be created
        
        if (sheepFlagListParent != null)
        {
            RectTransform parentRect = sheepFlagListParent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // Calculate total height needed
                float totalHeight = 0f;
                float itemHeight = 55f; // Default item height + spacing
                int itemCount = sheepFlagListParent.childCount;
                
                // Try to get actual height from first item
                if (itemCount > 0)
                {
                    RectTransform firstItemRect = sheepFlagListParent.GetChild(0).GetComponent<RectTransform>();
                    if (firstItemRect != null && firstItemRect.sizeDelta.y > 0)
                    {
                        itemHeight = firstItemRect.sizeDelta.y + 5f; // Add spacing
                    }
                }
                
                totalHeight = itemCount * itemHeight;
                
                // Set the content size to fit all items
                parentRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, totalHeight);
            }
        }
    }

    private void CreateNoFlagsMessage()
    {
        if (sheepFlagListParent == null) return;

        try
        {
            GameObject noFlagsMessage = new GameObject("NoFlagsMessage");
            noFlagsMessage.transform.SetParent(sheepFlagListParent, false);
            
            RectTransform rectTransform = noFlagsMessage.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 60);
            
            TextMeshProUGUI textComponent = noFlagsMessage.AddComponent<TextMeshProUGUI>();
            textComponent.text = "No individual sheep flags placed yet.\nUse 'Place Flag' to create individual positions for each sheep.";
            textComponent.fontSize = 14;
            textComponent.color = Color.gray;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // Set proper anchoring
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(0, -30);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating no flags message: {e.Message}");
        }
    }

    private void CreateSheepFlagListItem(SheepFlagInfo flagInfo, int index)
    {
        if (sheepFlagListParent == null) return;

        try
        {
            GameObject listItem;
            
            if (sheepFlagItemPrefab != null)
            {
                listItem = Instantiate(sheepFlagItemPrefab, sheepFlagListParent, false);
                
                // The VerticalLayoutGroup will handle positioning, so we just need to ensure proper size
                RectTransform rectTransform = listItem.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    rectTransform = listItem.AddComponent<RectTransform>();
                }
                
                // Ensure it has a proper height if not set (VerticalLayoutGroup will position it)
                if (rectTransform.sizeDelta.y <= 0)
                {
                    rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 50f);
                }
                
                Debug.Log($"[SHEEP FLAG] Created prefab item {index} with size {rectTransform.sizeDelta}");
                
                // Find and setup components in the prefab
                // Look for text component to display sheep name
                TextMeshProUGUI[] texts = listItem.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0)
                {
                    texts[0].text = GetCustomSheepName(index); // Use custom sheep names
                }
                
                // Find and setup buttons in the prefab
                Button[] buttons = listItem.GetComponentsInChildren<Button>();
                
                // Setup Move button (look for button with "Move" in name or first button)
                Button moveButton = null;
                Button deleteButton = null;
                
                foreach (Button btn in buttons)
                {
                    if (btn.name.ToLower().Contains("move"))
                    {
                        moveButton = btn;
                    }
                    else if (btn.name.ToLower().Contains("delete") || btn.name.ToLower().Contains("remove"))
                    {
                        deleteButton = btn;
                    }
                }
                
                // If we couldn't find named buttons, use first two buttons
                if (moveButton == null && buttons.Length > 0) moveButton = buttons[0];
                if (deleteButton == null && buttons.Length > 1) deleteButton = buttons[1];
                
                // Connect button functionality
                if (moveButton != null)
                {
                    moveButton.onClick.RemoveAllListeners();
                    moveButton.onClick.AddListener(() => {buttonClick.Play(); StartMovingSheepFlag(index);});
                    Debug.Log($"[SHEEP FLAG] Connected move button for item {index}");
                }
                
                if (deleteButton != null)
                {
                    deleteButton.onClick.RemoveAllListeners();
                    deleteButton.onClick.AddListener(() => {closeClicked.Play(); DeleteSheepFlag(index);});
                    Debug.Log($"[SHEEP FLAG] Connected delete button for item {index}");
                }
            }
            else
            {
                // Create a simple list item if no prefab is provided (fallback)
                listItem = new GameObject($"SheepFlag_{index}");
                listItem.transform.SetParent(sheepFlagListParent, false);
                
                // Add components for basic functionality
                RectTransform rectTransform = listItem.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(0, 40); // Use full width
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.anchoredPosition = new Vector2(0, -index * 45 - 22);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                
                Image background = listItem.AddComponent<Image>();
                background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                // Add sheep name text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(listItem.transform, false);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = GetCustomSheepName(index);
                text.fontSize = 12;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Left;
                
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(0.6f, 1);
                textRect.offsetMin = new Vector2(5, 0);
                textRect.offsetMax = new Vector2(-5, 0);
                
                // Add move button
                CreateListItemButton(listItem, "Move", new Vector2(0.6f, 0), new Vector2(0.8f, 1), 
                    () => StartMovingSheepFlag(index), Color.cyan);
                
                // Add delete button  
                CreateListItemButton(listItem, "Remove", new Vector2(0.8f, 0), new Vector2(1f, 1), 
                    () => DeleteSheepFlag(index), Color.red);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating sheep flag list item {index}: {e.Message}");
        }
    }

    private void CreateListItemButton(GameObject parent, string buttonText, Vector2 anchorMin, Vector2 anchorMax, 
        System.Action onClickAction, Color textColor)
    {
        GameObject buttonObj = new GameObject($"{buttonText}Button");
        buttonObj.transform.SetParent(parent.transform, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = new Vector2(2, 2);
        buttonRect.offsetMax = new Vector2(-2, -2);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => onClickAction?.Invoke());
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TextMeshProUGUI buttonTextComponent = textObj.AddComponent<TextMeshProUGUI>();
        buttonTextComponent.text = buttonText;
        buttonTextComponent.fontSize = 10;
        buttonTextComponent.color = textColor;
        buttonTextComponent.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void SelectSheepFlag(int flagIndex)
    {
        selectedSheepFlagIndex = flagIndex;
        Debug.Log($"Selected sheep flag {flagIndex}");
        
        // Highlight the selected flag in the list (visual feedback)
        RefreshSheepFlagList();
    }

    private void StartMovingSheepFlag(int flagIndex)
    {
        Debug.Log($"[SHEEP FLAG] StartMovingSheepFlag called for index {flagIndex}");
        
        if (barracksStructure.GetAnimalType() != "Sheep")
        {
            Debug.LogWarning("[SHEEP FLAG] Not a sheep barracks, aborting");
            return;
        }

        // Check if it's daytime
        NightManager nightManager = NightManager.Instance;
        if (nightManager != null && !nightManager.IsDay)
        {
            Debug.LogWarning("[SHEEP FLAG] Cannot move sheep flags at night");
            updateStatusText("Cannot move sheep flags at night");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            return;
        }

        Debug.Log($"[SHEEP FLAG] Setting up movement for sheep {flagIndex}");
        selectedSheepFlagIndex = flagIndex;
        isMovingSheepFlag = true;
        isPlacingFlag = true; // Set this so Update() handles the input properly
        
        Debug.Log($"[SHEEP FLAG] State set - isMovingSheepFlag: {isMovingSheepFlag}, isPlacingFlag: {isPlacingFlag}");
        
        // Just hide the management panel without resetting state
        if (sheepFlagManagementPanel != null)
        {
            sheepFlagManagementPanel.SetActive(false);
            HideUI();
            // StructureUIManager.Instance?.HideStructureUI();
        }
        
        // Show flag ghost for the specific sheep flag being moved
        CreateFlagGhost();
        
        // Set persistent status message that won't be overridden
        if (statusText != null)
        {
            statusText.text = $"Moving {GetCustomSheepName(flagIndex)} flag - Click to place, Right-click to cancel";
            statusText.color = Color.cyan;
            Debug.Log($"[SHEEP FLAG] Status text set to: {statusText.text}");
        }
        else
        {
            Debug.LogError("[SHEEP FLAG] statusText is null!");
        }
    }

    private void HandleSheepFlagMovementInput()
    {
        if (selectedSheepFlagIndex < 0)
        {
            Debug.LogWarning("[SHEEP FLAG] HandleSheepFlagMovementInput - Invalid flag index, ending movement");
            isMovingSheepFlag = false;
            isPlacingFlag = false;
            return;
        }

        // Handle mouse input for moving the flag
        if (Input.GetMouseButtonDown(0)) // Left click to place
        {
            Debug.Log($"[SHEEP FLAG] Left click detected - Moving sheep flag {selectedSheepFlagIndex}");
            MoveSheepFlagToMousePosition();
        }
        else if (Input.GetMouseButtonDown(1)) // Right click to cancel
        {
            Debug.Log($"[SHEEP FLAG] Right click detected - Cancelling sheep flag {selectedSheepFlagIndex} movement");
            CancelSheepFlagMovement();
        }
    }

    private void MoveSheepFlagToMousePosition()
    {
        Debug.Log($"[SHEEP FLAG] MoveSheepFlagToMousePosition called for flag {selectedSheepFlagIndex}");
        
        // Make sure we don't place on UI elements
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[SHEEP FLAG] Clicked on UI element, ignoring");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            Debug.Log($"[SHEEP FLAG] Raycast hit at position {hit.point}, calling MoveSheepFlag");
            bool success = barracksStructure.MoveSheepFlag(selectedSheepFlagIndex, hit.point);
            
            Debug.Log($"[SHEEP FLAG] MoveSheepFlag result: {success}");
            
            if (success)
            {
                if (statusText != null)
                {
                    statusText.text = $"{GetCustomSheepName(selectedSheepFlagIndex)} flag moved successfully!";
                    statusText.color = Color.green;
                }
                // Play the flag placement sound
                if (barracksStructure != null)
                {
                    // Trigger the flag placement sounds from BarracksStructure
                    var flagPlaceSound = barracksStructure.GetComponent<AudioSource>();
                    if (flagPlaceSound != null)
                    {
                        flagPlaceSound.Play();
                    }
                }
                
                // Refresh the flag list immediately after moving
                RefreshSheepFlagListIfOpen();
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "Failed to move sheep flag";
                    statusText.color = Color.red;
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayErrorSound();
                }
            }
            
            EndSheepFlagMovement();
        }
        else
        {
            Debug.Log("[SHEEP FLAG] Raycast missed ground");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
        }
    }

    private void CancelSheepFlagMovement()
    {
        Debug.Log("Sheep flag movement cancelled by user");
        if (statusText != null)
        {
            statusText.text = "Sheep flag movement cancelled";
            statusText.color = Color.yellow;
        }
        EndSheepFlagMovement();
    }

    private void EndSheepFlagMovement()
    {
        Debug.Log($"[SHEEP FLAG] EndSheepFlagMovement called - cleaning up state");
        Debug.Log($"[SHEEP FLAG] Previous state - isMovingSheepFlag: {isMovingSheepFlag}, isPlacingFlag: {isPlacingFlag}, selectedSheepFlagIndex: {selectedSheepFlagIndex}");
        
        isMovingSheepFlag = false;
        isPlacingFlag = false; // Clear the placement flag
        selectedSheepFlagIndex = -1;
        
        Debug.Log($"[SHEEP FLAG] State reset - isMovingSheepFlag: {isMovingSheepFlag}, isPlacingFlag: {isPlacingFlag}, selectedSheepFlagIndex: {selectedSheepFlagIndex}");
        
        // Clean up flag ghost
        DestroyFlagGhost();
        
        // Reopen the management panel properly
        if (sheepFlagManagementPanel != null)
        {
            Debug.Log("[SHEEP FLAG] Reopening sheep flag management panel");
            sheepFlagManagementPanel.SetActive(true);
            ShowUI();

            RefreshSheepFlagList(); // Always refresh when reopening
        }
        
        // Reset status text (will be overridden by normal UI updates since isMovingSheepFlag is now false)
        Debug.Log("[SHEEP FLAG] Movement ended, allowing normal UI updates");
    }

    private void UpdateSheepFlagMovementIndicator()
    {
        if (currentFlagGhost == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask groundLayer = LayerMask.GetMask("Ground", "Default");

        if (Physics.Raycast(ray, out hit, 1000f, groundLayer))
        {
            currentFlagGhost.transform.position = hit.point;
            currentFlagGhost.SetActive(true);
        }
        else
        {
            currentFlagGhost.SetActive(false);
        }
    }

    private void DeleteSheepFlag(int flagIndex)
    {
        bool success = barracksStructure.RemoveSheepFlag(flagIndex);
        
        if (success)
        {
            updateStatusText($"{GetCustomSheepName(flagIndex)} flag removed (sheep returns to main position)");
            RefreshSheepFlagList(); // Immediate refresh after deletion
        }
        else
        {
            updateStatusText("Failed to remove sheep flag");
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayErrorSound();
            }
        }
    }

    public void OnButtonHoverEnter(GameObject button)
    {
        // Debug.Log("Hover enter on button: " + button.name);
        if(hoverManager != null)
        {
            // TextMeshProUGUI feedText = feedButton.GetComponentInChildren<TextMeshProUGUI>();

            //when the new count is 0
            if(button == recruitButton.gameObject && newAnimalCount == 0)
            {
                hoverManager.ShowHover(recruitButton, "Recruiting air?", "Zero doesn’t count!", true, new Vector2(-200, 0), animalCountText.gameObject);
            }
            //when they cant afford more
            else if(button == addAnimal.gameObject && (newAnimalCount + animalCount) < maxAnimalCount && MoneyManager.Instance.CanAfford(newAnimalCount + 1 * barracksStructure.GetAnimalRecruitPrice()) && barracksStructure.CanRecruit(newAnimalCount + 1))
            {
                hoverManager.ShowHover(addAnimal, "Broke!", $"You can only afford {newAnimalCount}.", true, new Vector2(200, 0), costText.gameObject);
            }
            //when there isnt more space
            else if(button == addAnimal.gameObject && (newAnimalCount + animalCount) >= maxAnimalCount)
            {
                hoverManager.ShowHover(addAnimal, "Overcrowded!", "No more room for more animals.", true, new Vector2(200, 0), animalCountText.gameObject);
            }
            //where there are no civilian animals
            else if(button == addAnimal.gameObject && !barracksStructure.CanRecruit(newAnimalCount + 1))
            {
                hoverManager.ShowHover(addAnimal, "No civilians!", "Buy more civilians to recruit!", true, new Vector2(200, 0), civilianSection);
            }
            //when they want less than 0 new animals
            else if(button == minusAnimal.gameObject && newAnimalCount <= 0)
            {
                hoverManager.ShowHover(minusAnimal, "Recruiting air?", "Must recruit at least 1 animal", true, new Vector2(-200, 0), animalCountText.gameObject);
            }
            // when they can not place flag cause they have zero animals
            else if(button == placeFlagButton.gameObject && newAnimalCount <= 0)
            {
                hoverManager.ShowHover(placeFlagButton, "No Army!", "Cant place flag with no army!", true, new Vector2(-200, 0), animalCountText.gameObject);
            }
            //when the synergy indicator is active
            else if(button == synergyIndicator.gameObject && barracksStructure.isSynergyActive())
            {
                hoverManager.ShowHoverOnGameObject(synergyIndicator.gameObject, "Nice & Cheap!", "Perfect distance from pen! Recruiting costs less!", true, new Vector2(200, 0));
            }
            //when the synergy indicator is not active
            else if(button == synergyIndicator.gameObject && !barracksStructure.isSynergyActive())
            {
                if(barracksStructure.IsToFar())
                {
                    hoverManager.ShowHoverOnGameObject(synergyIndicator.gameObject, "Too Far!", "Too far from pen - No discount!", true, new Vector2(200, 0));
                }
                else
                {
                    hoverManager.ShowHoverOnGameObject(synergyIndicator.gameObject, "Too Close!", "Too close to pen - No discount!", true, new Vector2(200, 0));
                }
            }

        }
    }

    public void OnButtonHoverExit()
    {
        if(hoverManager != null)
        {
            hoverManager.HideHover();
        }
    }

    public void OnButtonClick(GameObject button)
    {
        if(button == recruitButton.gameObject && newAnimalCount == 0)
        {
            hoverManager.PlayErrorFeedback(false, recruitButton);
        }
        //when they cant afford more
        else if(button == addAnimal.gameObject && !MoneyManager.Instance.CanAfford(newAnimalCount + 1 * barracksStructure.GetAnimalRecruitPrice()))
        {
            hoverManager.PlayErrorFeedback(true, addAnimal);
        }
        //when there isnt more space
        else if(button == addAnimal.gameObject && (newAnimalCount + animalCount) <= maxAnimalCount)
        {
            hoverManager.PlayErrorFeedback(false, addAnimal);
        }
        //when they want less than 0 new animals
        else if(button == minusAnimal.gameObject && newAnimalCount <= 0)
        {
            hoverManager.PlayErrorFeedback(false, minusAnimal);
        }
        // when they can not place flag cause they have zero animals
        else if(button == placeFlagButton.gameObject && newAnimalCount <= 0)
        {
            hoverManager.PlayErrorFeedback(false, placeFlagButton);
        }  
    }

    private void updateSynergyIndicator()
    {
        if (barracksStructure == null || synergyIndicator == null) return;

        // Check if this structure currently has an active synergy
        if (barracksStructure.isSynergyActive())
        {
            // Show the indicator
            // synergyIndicator.gameObject.SetActive(true);

            // Set the sprite based on food type
            if (barracksStructure.GetAnimalType() == "Cow")
            {
                synergyIndicator.sprite = cowSynergyGood;
            }
            else if (barracksStructure.GetAnimalType() == "Chicken")
            {
                synergyIndicator.sprite = chickenSynergyGood;
            }
            else if (barracksStructure.GetAnimalType() == "Goat")
            {
                synergyIndicator.sprite = goatSynergyGood;
            }
            else if (barracksStructure.GetAnimalType() == "Pig")
            {
                synergyIndicator.sprite = pigSynergyGood;
            }
            else if (barracksStructure.GetAnimalType() == "Sheep")
            {
                synergyIndicator.sprite = sheepSynergyGood;
            }
        }
        else
        {
            if (barracksStructure.GetAnimalType() == "Cow")
            {
                synergyIndicator.sprite = cowSynergyBad;
            }
            else if (barracksStructure.GetAnimalType() == "Chicken")
            {
                synergyIndicator.sprite = chickenSynergyBad;
            }
            else if (barracksStructure.GetAnimalType() == "Goat")
            {
                synergyIndicator.sprite = goatSynergyBad;
            }
            else if (barracksStructure.GetAnimalType() == "Pig")
            {
                synergyIndicator.sprite = pigSynergyBad;
            }
            else if (barracksStructure.GetAnimalType() == "Sheep")
            {
                synergyIndicator.sprite = sheepSynergyBad;
            }
            // Hide the indicator if no synergy is active
            // synergyIndicator.gameObject.SetActive(false);
        }
    }

    private void HighlightTutorialButton()
    {
        if (SimplifiedTutorialManager.Instance == null) return;

        string nextButtonName = SimplifiedTutorialManager.Instance.GetNextUIButtonToHighlight();
        
        // Only update highlighting if the button has changed
        if (nextButtonName == lastHighlightedButton) return;
        
        lastHighlightedButton = nextButtonName;
        
        if (string.IsNullOrEmpty(nextButtonName)) return;

        GameObject buttonToHighlight = null;
        string lowerButtonName = nextButtonName.ToLower();

        // Match button names
        if (lowerButtonName.Contains("recruit"))
        {
            buttonToHighlight = recruitButton?.gameObject;
        }
        else if (lowerButtonName.Contains("flag") && lowerButtonName.Contains("place"))
        {
            buttonToHighlight = placeFlagButton?.gameObject;
        }

        if (buttonToHighlight != null)
        {
            SimplifiedTutorialManager.Instance.HighlightStructureUIButton(buttonToHighlight);
        }
    }
}
