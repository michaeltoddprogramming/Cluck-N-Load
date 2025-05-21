using UnityEngine;

public class AnimalStructure : Structure
{
    public enum AnimalType
    {
        Chicken,
        Cow,
        Sheep,
        Goat,
        Pig
    }

    [Header("Animal Settings")]
    [SerializeField] private AnimalType animalType;
    [SerializeField] private bool isProducing;
    [SerializeField] private bool productReady;
    [SerializeField] private bool productionFinished;
    [SerializeField] private float productionProgress;
    [SerializeField] private AnimalProductionSettings productionSettings;
    [SerializeField] private int animalCount = 5;
    [SerializeField] private int maxAnimalCount = 10;

    [System.Serializable]
    public class AnimalProductionSettings
    {
        public float productionTime = 24f;
        public int productAmount = 1;
        public int moneyPerProduct = 10;
        public int baseFoodRequired = 2;
        // Removed foodMultiplier since we're not using synergies
    }

    private NightManager nightManager;
    private float lastCheckedHour;

    // Public getters for UI and Barracks
    public bool IsProducing => isProducing;
    public bool ProductReady => productReady;
    public bool ProductionFinished => productionFinished;
    public float ProductionProgress => productionProgress;
    public AnimalProductionSettings ProductionSettings => productionSettings;
    public AnimalType GetAnimalType => animalType;
    public int AnimalCount => animalCount;
    public int MaxAnimalCount => maxAnimalCount;

    protected override void Start()
    {
        base.Start();

        if (structureData != null && structureData.type != StructureType.Animal)
        {
            Debug.LogWarning($"{gameObject.name} has AnimalStructure script but StructureData.type is {structureData.type}, expected Animal.");
        }

        // Removed redundant check for StructureType.AnimalPlot since it's already checked above
        // Make sure animalCount is initialized correctly
            Debug.Log($"{GetStructureName()} initialized with {animalCount} {animalType}s");
        isProducing = false;
        productReady = false;
        productionFinished = false;
        productionProgress = 0f;
        if (productionSettings == null)
        {
            productionSettings = new AnimalProductionSettings();
        }

        nightManager = FindObjectOfType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError("NightManager not found in the scene! AnimalStructure requires NightManager to function.");
        }

        lastCheckedHour = nightManager != null ? nightManager.Hours + (nightManager.Minutes / 60f) : 0f;

        if (nightManager != null)
        {
            nightManager.RegisterAnimalStructure(this);
        }

        // Removed UpdateSynergies call
    }

    private void Update()
    {
        if (nightManager == null || !isProducing || productReady || productionFinished) return;

        float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
        float hourDelta;
        if (currentHour < lastCheckedHour)
        {
            hourDelta = (24f - lastCheckedHour) + currentHour;
        }
        else
        {
            hourDelta = currentHour - lastCheckedHour;
        }

        productionProgress += hourDelta;
        lastCheckedHour = currentHour;

        if (productionProgress >= productionSettings.productionTime)
        {
            productionFinished = true;
            isProducing = false;
            productionProgress = productionSettings.productionTime;
            Debug.Log($"{GetStructureName()} has finished producing, waiting for the next day to make products available.");
        }
    }

    public void Feed()
    {
        if (!isProducing && !productReady)
        {
            string requiredFood = GetRequiredFood();
            int foodRequired = productionSettings.baseFoodRequired; // Removed foodMultiplier

            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredFood, foodRequired))
            {
                InventoryManager.Instance.RemoveItem(requiredFood, foodRequired);
                Debug.Log($"{GetStructureName()} is being fed... (Used {foodRequired} {requiredFood})");
                isProducing = true;
                productionFinished = false;
                productionProgress = 0f;
                if (nightManager != null)
                {
                    lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
                }
            }
            else
            {
                Debug.LogWarning($"{GetStructureName()} cannot be fed: Not enough {requiredFood} (need {foodRequired})!");
            }
        }
    }

    private string GetRequiredFood()
    {
        switch (animalType)
        {
            case AnimalType.Chicken:
                return "Sunflower";
            case AnimalType.Cow:
                return "Wheat";
            case AnimalType.Sheep:
            case AnimalType.Goat:
            case AnimalType.Pig:
                return "Carrots";
            default:
                return "Unknown";
        }
    }

    public void Collect()
    {
        Debug.Log($"{GetStructureName()} Collect called: productReady={productReady}, isProducing={isProducing}, productionFinished={productionFinished}");

        if (productReady)
        {
            Debug.Log($"{GetStructureName()} is collecting {productionSettings.productAmount} products...");

            int totalMoneyEarned = productionSettings.productAmount * productionSettings.moneyPerProduct; // Removed globalMoneyMultiplier
            Debug.Log($"Money calculation: productAmount={productionSettings.productAmount}, moneyPerProduct={productionSettings.moneyPerProduct}, totalMoneyEarned={totalMoneyEarned}");

            if (MoneyManager.Instance == null)
            {
                Debug.LogError("MoneyManager not found in the scene!");
            }
            else
            {
                MoneyManager.Instance.AddMoney(totalMoneyEarned);
                Debug.Log($"Earned {totalMoneyEarned} {MoneyManager.Instance.GetCurrencyName()} from collecting {productionSettings.productAmount} products!");
            }

            productReady = false;
            isProducing = false;
            productionFinished = false;
            productionProgress = 0f;
            if (nightManager != null)
            {
                lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
            }
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot collect: productReady is false!");
        }
    }

    public void OnNewDay()
    {
        if (productionFinished && !productReady)
        {
            productReady = true;
            productionFinished = false;
            Debug.Log($"{GetStructureName()} products are now available to collect at the start of a new day!");
        }
    }

    // Methods for Barracks recruitment
    public bool CanRecruit(int amount)
    {
        return animalCount >= amount;
    }

    public void RecruitAnimals(int amount)
    {
        if (CanRecruit(amount))
        {
            animalCount -= amount;
            Debug.Log($"{GetStructureName()} recruited {amount} {animalType}s. Remaining: {animalCount}");
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} does not have enough {animalType}s to recruit. Current count: {animalCount}");
        }
    }

    public void AddAnimals(int amount)
    {
        animalCount = Mathf.Min(animalCount + amount, maxAnimalCount);
        Debug.Log($"{GetStructureName()} added {amount} {animalType}s. New count: {animalCount}");
    }

    private void OnDestroy()
    {
        if (nightManager != null)
        {
            nightManager.UnregisterAnimalStructure(this);
        }
    }

    // Removed UpdateSynergies method
}