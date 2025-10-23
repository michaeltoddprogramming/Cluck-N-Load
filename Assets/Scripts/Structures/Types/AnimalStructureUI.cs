using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimalStructureUI : BaseStructureUI
{
    [SerializeField] private Button feedButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button buyAnimal;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Button addAnimal;
    [SerializeField] private Button removeAnimal;
    [SerializeField] private TextMeshProUGUI animalAmountText;
    [SerializeField] private TextMeshProUGUI newAnimalAmount;
    [SerializeField] public Image animalIcon1;
    // [SerializeField] public Image animalIcon2;
    [SerializeField] public Sprite cowIcon;
    [SerializeField] public Sprite chickenIcon;
    [SerializeField] public Sprite goatIcon;
    [SerializeField] public Sprite pigIcon;
    [SerializeField] public Sprite sheepIcon;
    [SerializeField] public AudioClip clickSound;
    [SerializeField] public TextMeshProUGUI costText;

    [Header("Production Amount")]
    [SerializeField] public Image animalIcon3;
    [SerializeField] public TextMeshProUGUI coinPerAnimalText;
    [SerializeField] public TextMeshProUGUI totalCoinsText;
    private CivilianSpawner civilianSpawner;

    private int newAnimalCount;
    private AnimalStructure animalStructure;
    private bool isAnimalStructure;
    private new NightManager nightManager;
    private bool lastPauseState = false; // Track pause state changes

    private float lastUIUpdate;
    private const float UI_UPDATE_INTERVAL = 0.5f; // Update UI twice per second

    [Header("Civilian indicator")]
    [SerializeField] private Slider civilianBarSlider;
    [SerializeField] private Image civilianBarFill;

    [Header("Food indicator")]
    [SerializeField] private Image animalFood;
    [SerializeField] private Sprite sunflowerSeed;
    [SerializeField] private Sprite wheat;
    [SerializeField] private Sprite carrot;
    [SerializeField] private TextMeshProUGUI foodNeededText;

    [Header("Production Impact Warning")]
    [SerializeField] private GameObject productionWarningPanel;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private Button confirmActionButton;
    [SerializeField] private Button cancelActionButton;
    
    private System.Action pendingAction;




    // void Start()
    // {
    //     // Instantiate health bar if prefab is assigned
    //     if (healthBarPrefab != null && healthBarInstance == null)
    //     {
    //         healthBarInstance = Instantiate(healthBarPrefab, transform);

    //         // Position the health bar above the structure based on its height
    //         var rect = healthBarInstance.GetComponent<RectTransform>();
    //         // if (rect != null)
    //         // {
    //         // float structureHeight = GetStructureHeight();
    //         // rect.localPosition = new Vector3(0, structureHeight + 1.5f, 0); // Add 1.5f buffer above structure
    //         // }
    //         healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
    //         healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
    //         // healthBarCanvasGroup = healthBarInstance.GetComponentInChildren<CanvasGroup>();
    //         healthBarInstance.SetActive(false); // Start hidden

    //         // Set initial visibility based on current health and time of day
    //         UpdateHealthBar();
    //     }
    // }

    protected override void Update()
    {
        // Call base update to handle move button logic
        base.Update();
        
        // Check for pause state changes and update UI immediately
        if (nightManager != null)
        {
            bool currentPauseState = nightManager.getIsPaused();
            if (currentPauseState != lastPauseState)
            {
                lastPauseState = currentPauseState;
                Debug.Log($"[AnimalStructureUI] Pause state changed to: {currentPauseState}");
                UpdateUI(); // Update immediately when pause state changes
                return;
            }
        }
        
        // Regular UI updates
        if (Time.time - lastUIUpdate > UI_UPDATE_INTERVAL)
        {
            UpdateUI();
            lastUIUpdate = Time.time;
        }
    }

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);
        isAnimalStructure = structure is AnimalStructure;
        if (isAnimalStructure)
        {
            animalStructure = (AnimalStructure)structure;
            animalStructure.OnAnimalCountChanged += UpdateUI;
            animalStructure.PlayBackgroundNoise();
        }
        nightManager = NightManager.Instance ?? FindFirstObjectByType<NightManager>();
        if (!isAnimalStructure)
        {
            HideAnimalSpecificUI();
            return;
        }

        if (animalStructure.RequiredFood == "Sunflower")
        {
            animalFood.sprite = sunflowerSeed;
        }
        else if (animalStructure.RequiredFood == "Wheat")
        {
            animalFood.sprite = wheat;
        }
        else
        {
            animalFood.sprite = carrot;
        }


        SetupButtonListeners();
        UpdateUI();
    }

    private void SetupButtonListeners()
    {
        feedButton?.onClick.AddListener(() => { animalStructure.Feed(); UpdateUI(); });
        collectButton?.onClick.AddListener(() => { animalStructure.Collect(); UpdateUI(); });
        addAnimal?.onClick.AddListener(() => { animalChange(0); UpdateUI(); });
        removeAnimal?.onClick.AddListener(() => { animalChange(1); UpdateUI(); });
        buyAnimal?.onClick.AddListener(() => { BuyAnimalsWithWarningCheck(); UpdateUI(); });
    }

    private void UpdateUI()
    {
        if (!isAnimalStructure || animalStructure == null || nightManager == null)
        {
            HideAnimalSpecificUI();
            return;
        }

        // UpdateHealthBar();
        updateStatusBar();

        foodNeededText.text = $"{animalStructure.foodRequired}";

        bool isProducing = animalStructure.IsProducing;
        bool productReady = animalStructure.ProductReady;
        int animalCount = animalStructure.AnimalCount;
        int maxAnimalCount = animalStructure.MaxAnimalCount;
        bool isPaused = nightManager.getIsPaused();
        Debug.Log($"[AnimalStructureUI] UpdateUI called - isPaused: {isPaused}");
        bool canFeed = nightManager.IsDay && !isProducing && !productReady && animalCount > 0 && animalStructure.canFeed() && !isPaused;
        bool canCollect = productReady && nightManager.IsDay && !isPaused;
        // Explicitly prevent buying if already producing (fixes "can't buy if already fed and is producing")
        bool canBuy = nightManager.IsDay && animalCount < maxAnimalCount && !isProducing && !isPaused;  // Added !isProducing and !isPaused checks

        animalAmountText.text = $"{animalCount}/{maxAnimalCount}";
        newAnimalAmount.text = $"{newAnimalCount}";

        coinPerAnimalText.text = $"{animalStructure.ProductionSettings.moneyPerProduct}";
        totalCoinsText.text = $"{animalCount * animalStructure.ProductionSettings.moneyPerProduct}";
        SetAnimalIcons();

        feedButton.interactable = canFeed;
        collectButton.interactable = canCollect;

        // Tutorial logic: disable buy button only when we already OWN the exact amount
        bool tutorialBuyRestriction = false;
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            if (!TutorialManager.Instance.GetCompletedStepIds().Contains("buy_chickens"))
            {
                // During buy_chickens tutorial step, disable BUY button only when we already OWN 5 animals
                if (animalCount >= 5)
                {
                    tutorialBuyRestriction = true;
                    Debug.Log($"Tutorial: Buy restricted - already own 5 animals. Current owned: {animalCount}");
                }
            }
        }

        if (buyAnimal != null)
            buyAnimal.interactable = newAnimalCount > 0 && canBuy && !tutorialBuyRestriction;

        // ADD button should disable when clicking it would make the TOTAL SELECTED exceed 5
        bool tutorialAddRestriction = false;
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            if (!TutorialManager.Instance.GetCompletedStepIds().Contains("buy_chickens"))
            {
                // Disable ADD button when total selected would exceed what we can buy (5 - current owned)
                int maxCanBuy = 5 - animalCount;
                if (newAnimalCount >= maxCanBuy)
                {
                    tutorialAddRestriction = true;
                    Debug.Log($"Tutorial: Add restricted - can only buy {maxCanBuy} more animals. Currently selected: {newAnimalCount}");
                }
            }
        }

        addAnimal.interactable = (newAnimalCount + animalCount) < maxAnimalCount && canBuy && MoneyManager.Instance != null && MoneyManager.Instance.CanAfford((newAnimalCount + 1) * animalStructure.ProductionSettings.costPerAnimal) && !tutorialAddRestriction;
        removeAnimal.interactable = newAnimalCount > 0 && !isPaused;

        costText.text = (newAnimalCount * animalStructure.ProductionSettings.costPerAnimal).ToString();

        if (feedButton != null)
        {
            var buttonImage = feedButton.GetComponent<Image>();
            TextMeshProUGUI feedText = feedButton.GetComponentInChildren<TextMeshProUGUI>();

            bool needsFeedAndCantFeed = !canFeed && animalCount > 0 && !isProducing && !productReady &&
                InventoryManager.Instance != null &&
                !InventoryManager.Instance.HasItem(animalStructure.RequiredFood,
                    (int)((animalStructure.ProductionSettings.baseFoodRequired * animalCount) * animalStructure.foodMultiplier));

            if (buttonImage != null)
            {
                // Grey out the button when feed needed but can't feed
                Color greyColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // Medium grey
                buttonImage.color = needsFeedAndCantFeed ? greyColor : Color.white;
            }

            // Change notification text to yellow when feed needed
            if (feedText != null)
            {
                feedText.color = needsFeedAndCantFeed ? Color.yellow : Color.white;
            }
        }

        UpdateStatusText(isProducing, productReady, animalCount);
    }

    private void SetAnimalIcons()
    {
        Sprite icon = animalStructure.GetAnimalType switch
        {
            AnimalStructure.AnimalType.Cow => cowIcon,
            AnimalStructure.AnimalType.Chicken => chickenIcon,
            AnimalStructure.AnimalType.Goat => goatIcon,
            AnimalStructure.AnimalType.Pig => pigIcon,
            AnimalStructure.AnimalType.Sheep => sheepIcon,
            _ => null
        };
        if (animalIcon1 != null) animalIcon1.sprite = icon;
        if (animalIcon3 != null) animalIcon3.sprite = icon;
        // if (animalIcon2 != null) animalIcon2.sprite = icon;
    }

    private void UpdateStatusText(bool isProducing, bool productReady, int animalCount)
    {
        if (statusText != null)
        {
            if (productReady)
            {
                string baseText = "Ready to collect!";
                // Add production impact info if relevant
                if (animalStructure.HasLostAnimalsFromProduction)
                {
                    int lost = animalStructure.OriginalAnimalCountWhenFed - animalCount;
                    baseText += $"\n⚠️ Production reduced: {lost} animals were recruited during production";
                }
                statusText.text = baseText;
                statusText.color = Color.green;
            }
            else if (isProducing)
            {
                string baseText = "Producing...";
                // Add production impact info if relevant
                if (animalStructure.HasLostAnimalsFromProduction)
                {
                    int original = animalStructure.OriginalAnimalCountWhenFed;
                    baseText += $"\n⚠️ Started with {original}, now have {animalCount}";
                }
                statusText.text = baseText;
                statusText.color = Color.white;
            }
            else if (animalCount <= 0)
            {
                statusText.text = "No animals!";
                statusText.color = Color.yellow;
            }
            else if (!nightManager.IsDay)
            {
                statusText.text = "Cannot feed at night";
                statusText.color = Color.yellow;
            }
            else if (!animalStructure.canFeed())
            {
                // Enhanced message: Show required food details
                int requiredFood = (int)((animalStructure.ProductionSettings.baseFoodRequired * animalCount) * animalStructure.foodMultiplier);
                int availableFood = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemCount(animalStructure.RequiredFood) : 0;
                statusText.text = $"Not enough {animalStructure.RequiredFood} to feed! Need {requiredFood}, have {availableFood}.";
                statusText.color = Color.yellow;  // Changed from red to yellow
            }
            else
            {
                statusText.text = "Needs feeding";
                statusText.color = Color.white;
            }
        }
    }

    private void HideAnimalSpecificUI()
    {
        feedButton.interactable = false;
        collectButton.interactable = false;
        buyAnimal.interactable = false;
        addAnimal.interactable = false;
        removeAnimal.interactable = false;
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (statusText != null)
        {
            statusText.text = "Not an animal structure";
            statusText.color = Color.yellow;
        }
    }

    protected override void OnDestroy()
    {
        if (isAnimalStructure && animalStructure != null)
        {
            animalStructure.OnAnimalCountChanged -= UpdateUI;
            animalStructure.StopBackgroundNoise();
        }

        
        base.OnDestroy();
    }

    private void animalChange(int flag)
    {
        if (flag == 0) newAnimalCount++;
        else if (flag == 1 && newAnimalCount > 0) newAnimalCount--;
    }

    private void BuyAnimals()
    {
        if (newAnimalCount > 0 && MoneyManager.Instance != null)
        {
            CivilianSpawner spawner = animalStructure.GetComponentInChildren<CivilianSpawner>();
            if (spawner != null)
                spawner.SpawnAnimals(newAnimalCount + animalStructure.AnimalCount);
            animalStructure.BuyAnimals(newAnimalCount);
            newAnimalCount = 0;
        }
    }

    public void updateStatusBar()
    {
        if (animalStructure == null || civilianBarSlider == null) return;

        int civilianCount = animalStructure.animalCount;
        int civilianMax = animalStructure.maxAnimalCount;

        float fillPercent2 = civilianMax > 0 ? (float)civilianCount / civilianMax : 0f;
        civilianBarSlider.value = fillPercent2;
        // civilianText.text = $"{civilianCount}/{civilianMax}";
    }

    // Production impact warning system
    private void ShowProductionWarningPanel(string warningMessage, System.Action onConfirm)
    {
        if (productionWarningPanel == null) return;
        
        pendingAction = onConfirm;
        warningText.text = warningMessage;
        productionWarningPanel.SetActive(true);
        
        // Setup button listeners
        confirmActionButton?.onClick.RemoveAllListeners();
        cancelActionButton?.onClick.RemoveAllListeners();
        
        confirmActionButton?.onClick.AddListener(ConfirmAction);
        cancelActionButton?.onClick.AddListener(CancelAction);
    }
    
    private void ConfirmAction()
    {
        pendingAction?.Invoke();
        HideProductionWarningPanel();
    }
    
    private void CancelAction()
    {
        pendingAction = null;
        HideProductionWarningPanel();
    }
    
    private void HideProductionWarningPanel()
    {
        if (productionWarningPanel != null)
            productionWarningPanel.SetActive(false);
    }
    
    // Enhanced BuyAnimals with production impact check
    private void BuyAnimalsWithWarningCheck()
    {
        if (newAnimalCount <= 0 || MoneyManager.Instance == null) return;
        
        // Check if this action would affect ongoing production
        string impactInfo = animalStructure.GetProductionImpactInfo();
        if (animalStructure.HasLostAnimalsFromProduction)
        {
            string warningMessage = $"Production Status:\n{impactInfo}\n\nAdding {newAnimalCount} animals will not affect current production output.\n\nProceed?";
            ShowProductionWarningPanel(warningMessage, () => BuyAnimals());
        }
        else
        {
            BuyAnimals(); // No warning needed
        }
    }
}