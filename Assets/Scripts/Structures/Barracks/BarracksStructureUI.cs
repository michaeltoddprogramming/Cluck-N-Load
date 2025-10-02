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
    [SerializeField] private int recruitAmount = 1;
    [SerializeField] private GameObject flagPlacementIndicator;
    [SerializeField] private GameObject flagGhostPrefab; // Ghost prefab for flag placement preview
    [SerializeField] private Material flagGhostMaterial; // Optional: Material to apply to ghost flag
    [SerializeField] private Button addAnimal;
    [SerializeField] private Button minusAnimal;
    [SerializeField] private TextMeshProUGUI animalCountText;
    [SerializeField] private TextMeshProUGUI costText;

    [Header("UI Control")]
    [SerializeField] private CanvasGroup uiCanvasGroup;

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
    private bool lastPauseState = false; // Track pause state changes

    // Public property to check if this barracks is currently placing a flag
    public bool IsPlacingFlag => isPlacingFlag;

    [SerializeField] public Image animalIcon1;
    [SerializeField] public Image animalIcon2;
    [SerializeField] public Sprite cowIcon;
    [SerializeField] public Sprite chickenIcon;
    [SerializeField] public Sprite goatIcon;
    [SerializeField] public Sprite pigIcon;
    [SerializeField] public Sprite sheepIcon;

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

        setUpStats();

        if (recruitButton != null)
        {
            recruitButton.onClick.RemoveAllListeners();
            recruitButton.onClick.AddListener(() =>
            {
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
            placeFlagButton.onClick.AddListener(StartFlagPlacement);
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

    private void Update()
    {
        // Check for pause state changes and update UI immediately
        NightManager nightManager = NightManager.Instance;
        if (nightManager != null)
        {
            bool currentPauseState = nightManager.getIsPaused();
            if (currentPauseState != lastPauseState)
            {
                lastPauseState = currentPauseState;
                Debug.Log($"[BarracksStructureUI] Pause state changed to: {currentPauseState}");
                UpdateUI(); // Update immediately when pause state changes
            }
        }
        
        if (Time.time - lastUIUpdate > UI_UPDATE_INTERVAL)
        {
            UpdateUI();
            lastUIUpdate = Time.time;
        }

        // Handle flag placement input
        if (isPlacingFlag)
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
                // Optional: Add visual feedback, e.g., flash the button red
                if (placeFlagButton != null)
                {
                    var buttonImage = placeFlagButton.GetComponent<Image>();
                    if (buttonImage != null) buttonImage.color = Color.red;
                    LeanTween.color(buttonImage.rectTransform, Color.white, 1f);  // Reset after 1 second
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
            // placeFlagButton.GetComponentInChildren<TextMeshProUGUI>().text = "Placing...";
        }
        if (flagPlacementIndicator != null) flagPlacementIndicator.SetActive(true);
        
        // Create flag ghost for preview
        CreateFlagGhost();
    }

    private void HandleFlagPlacementInput()
    {
        // Check for night start cancellation for sheep
        if (barracksStructure.GetAnimalType() == "Sheep")
        {
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && !nightManager.IsDay)
            {
                Debug.Log("Cancelling flag placement - night started for sheep");
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
            Debug.Log("Left mouse button pressed during flag placement");
            PlaceFlagAtMousePosition();
        }
        else if (Input.GetMouseButtonDown(1)) // Right click to cancel
        {
            Debug.Log("Right mouse button pressed during flag placement");
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
            if (isPlacingFlag)
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
            ColorBlock colors = placeFlagButton.colors;
            colors.normalColor = hasArmy ? new Color(0.8f, 1f, 0.8f) : Color.grey;
            placeFlagButton.colors = colors;
        }

        if (setFlagColorButton != null && !isPlacingFlag)
        {
            setFlagColorButton.interactable = true;
            Image buttonImage = setFlagColorButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(barracksStructure.GetFlagColor, Color.white, 0.7f);
            }
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
                        Debug.Log($"Tutorial: Add restricted - can only recruit {maxCanRecruit} more animals. Currently selected: {newAnimalCount}");
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
                        Debug.Log($"Tutorial: Recruit restricted - already own 3 army animals. Current owned: {currentArmyCount}");
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
            // Play error sound for insufficient funds
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
            }
        }

        // Add day/night check for sheep flag placement
        if (barracksStructure.GetAnimalType() == "Sheep")
        {
            bool isDay = nightManager != null ? nightManager.IsDay : true;
            Debug.Log($"[Sheep Barracks UI] IsDay: {isDay}, HasArmy: {hasArmy}");  // Debug log to check values

            if (placeFlagButton != null)
            {
                placeFlagButton.interactable = isDay && hasArmy && !isPaused;
                Debug.Log($"[Sheep Barracks UI] PlaceFlagButton interactable: {placeFlagButton.interactable}");  // Debug log for button state

                if (isPaused && statusText != null)
                {
                    updateStatusText("Cannot place flags while game is paused");
                    // Play error sound for paused game action
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayErrorSound();
                    }
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
                    updateStatusText("Ready to place sheep flags");
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
    }

    private void updateStatusText(string message)
    {
        // Update status text
        if (statusText != null)
        {
            string animalStatus = "";

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
        BarracksStructureUI[] allBarracks = FindObjectsOfType<BarracksStructureUI>();
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
        // Use CanvasGroup to restore UI visibility and interaction
        if (uiCanvasGroup != null)
        {
            LeanTween.alphaCanvas(uiCanvasGroup, 1f, 0.2f).setEase(LeanTweenType.easeOutQuad);
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }
    }
}