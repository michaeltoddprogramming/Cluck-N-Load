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
    [SerializeField] public Image animalIcon2;
    [SerializeField] public Sprite cowIcon;
    [SerializeField] public Sprite chickenIcon;
    [SerializeField] public Sprite goatIcon;
    [SerializeField] public Sprite pigIcon;
    [SerializeField] public Sprite sheepIcon;
    [SerializeField] public AudioClip clickSound;
    [SerializeField] public TextMeshProUGUI costText;
    private CivilianSpawner civilianSpawner;

    private int newAnimalCount;
    private AnimalStructure animalStructure;
    private bool isAnimalStructure;
    private NightManager nightManager;

    private float lastUIUpdate;
    private const float UI_UPDATE_INTERVAL = 0.5f; // Update UI twice per second

    private void Update()
    {
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
        SetupButtonListeners();
        UpdateUI();
    }

    private void SetupButtonListeners()
    {
        feedButton?.onClick.AddListener(() => { animalStructure.Feed(); UpdateUI(); });
        collectButton?.onClick.AddListener(() => { animalStructure.Collect(); UpdateUI(); });
        addAnimal?.onClick.AddListener(() => { animalChange(0); UpdateUI(); });
        removeAnimal?.onClick.AddListener(() => { animalChange(1); UpdateUI(); });
        buyAnimal?.onClick.AddListener(() => { BuyAnimals(); UpdateUI(); });
    }

    private void UpdateUI()
    {
        if (!isAnimalStructure || animalStructure == null || nightManager == null)
        {
            HideAnimalSpecificUI();
            return;
        }
        bool isProducing = animalStructure.IsProducing;
        bool productReady = animalStructure.ProductReady;
        int animalCount = animalStructure.AnimalCount;
        int maxAnimalCount = animalStructure.MaxAnimalCount;
        bool canFeed = nightManager.IsDay && !isProducing && !productReady && animalCount > 0 && animalStructure.canFeed();
        bool canCollect = productReady && nightManager.IsDay;
        // Explicitly prevent buying if already producing (fixes "can't buy if already fed and is producing")
        bool canBuy = nightManager.IsDay && animalCount < maxAnimalCount && !isProducing;  // Added !isProducing check

        animalAmountText.text = $"{animalCount}/{maxAnimalCount}";
        newAnimalAmount.text = $"{newAnimalCount}";
        SetAnimalIcons();

        feedButton.interactable = canFeed;
        collectButton.interactable = canCollect;

        if (buyAnimal != null)
            buyAnimal.interactable = newAnimalCount > 0 && canBuy;  // Use updated canBuy

        addAnimal.interactable = (newAnimalCount + animalCount) < maxAnimalCount && canBuy && MoneyManager.Instance != null && MoneyManager.Instance.CanAfford((newAnimalCount + 1) * animalStructure.ProductionSettings.costPerAnimal);
        removeAnimal.interactable = newAnimalCount > 0;

        costText.text = (newAnimalCount * animalStructure.ProductionSettings.costPerAnimal).ToString();

        // Enhanced feedback for insufficient feed (add visual indicator)
        if (!canFeed && animalCount > 0 && !isProducing && !productReady)
        {
            // Add a red tint to the feed button or an icon
            if (feedButton != null)
            {
                var buttonImage = feedButton.GetComponent<Image>();
                if (buttonImage != null) buttonImage.color = Color.red;  // Visual indicator for insufficient feed
            }
        }
        else
        {
            // Reset color if conditions are met
            if (feedButton != null)
            {
                var buttonImage = feedButton.GetComponent<Image>();
                if (buttonImage != null) buttonImage.color = Color.white;
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
        // if (animalIcon2 != null) animalIcon2.sprite = icon;
    }

    private void UpdateStatusText(bool isProducing, bool productReady, int animalCount)
    {
        if (statusText != null)
        {
            if (productReady)
            {
                statusText.text = "Ready to collect!";
                statusText.color = Color.green;
            }
            else if (isProducing)
            {
                statusText.text = "Producing...";
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
                statusText.color = Color.red;  // More prominent color for insufficient feed
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
            animalStructure.BuyAnimals(newAnimalCount);
            newAnimalCount = 0;
            CivilianSpawner spawner = animalStructure.GetComponentInChildren<CivilianSpawner>();
            if (spawner != null)
                spawner.SpawnAnimals(newAnimalCount);
        }
    }

    // public void SpawnAnimals(int userPurchaseCount)
    // {
    //     int desiredSpawnCount = Mathf.CeilToInt(userPurchaseCount / 2f);

    //     while (spawnedAnimals.Count < desiredSpawnCount)
    //     {
    //         SpawnSingleAnimal();
    //     }
    // }
}