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

    [System.Serializable]
    public class AnimalProductionSettings
    {
        public float productionTime = 24f;
        public int productAmount = 1;
        public int moneyPerProduct = 10;
        public int baseFoodRequired = 2;
        public float foodMultiplier = 1f;
    }

    private NightManager nightManager;
    private float lastCheckedHour;

    public bool IsProducing => isProducing;
    public bool ProductReady => productReady;
    public bool ProductionFinished => productionFinished;
    public float ProductionProgress => productionProgress;
    public AnimalProductionSettings ProductionSettings => productionSettings;
    public AnimalType GetAnimalType => animalType;

    protected override void Start()
    {
        base.Start();
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

        if (structureData != null && structureData.type != StructureType.AnimalPlot)
        {
            Debug.LogWarning($"{gameObject.name} has AnimalStructure script but StructureData.type is {structureData.type}, expected AnimalPlot.");
        }

        if (nightManager != null)
        {
            nightManager.RegisterAnimalStructure(this);
        }

        UpdateSynergies();
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
            int foodRequired = Mathf.RoundToInt(productionSettings.baseFoodRequired * productionSettings.foodMultiplier);

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

            int totalMoneyEarned = productionSettings.productAmount * productionSettings.moneyPerProduct;
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

    public void UpdateSynergies()
    {
        SiloStructure[] silos = FindObjectsOfType<SiloStructure>();
        float minDistance = float.MaxValue;
        foreach (SiloStructure silo in silos)
        {
            float distance = Vector3.Distance(transform.position, silo.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        float maxDistance = 10f;
        productionSettings.foodMultiplier = minDistance <= maxDistance ? 0.5f : 1f;
        Debug.Log($"{GetStructureName()} synergy updated: minDistance to silo={minDistance}, foodMultiplier={productionSettings.foodMultiplier}");
    }

    private void OnDestroy()
    {
        if (nightManager != null)
        {
            nightManager.UnregisterAnimalStructure(this);
        }
    }
}