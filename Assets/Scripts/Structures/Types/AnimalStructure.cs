using UnityEngine;
using System.Collections;

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
    [SerializeField] private float productionProgress;
    [SerializeField] private AnimalProductionSettings productionSettings;
    [SerializeField] private int animalCount = 5;
    [SerializeField] private int maxAnimalCount = 10;

    [Header("SFX")]
    [Tooltip("Background sound for the animal structure.")]
    // [SerializeField] public AudioClip backgroundNoise;
    // [SerializeField] public AudioSource backgroundNoise;
    // [SerializeField] public AudioSource backgroundNoise;
    [SerializeField] private AudioSource audioSource; // Assign in Inspector
    [SerializeField] private AudioClip backgroundClip; // Assign in Inspector
    private Coroutine soundCoroutine;

    public class AnimalProductionSettings
    {
        public float productionTime = 24f;
        public int productAmount = 1;
        public int moneyPerProduct = 10;
        public int baseFoodRequired = 2;
        public int costPerAnimal = 50;
    }

    private NightManager nightManager;
    private float lastCheckedHour;
    private string requiredFood;

    public System.Action OnAnimalCountChanged;

    public bool IsProducing => isProducing;
    public bool ProductReady => productReady;
    public float ProductionProgress => productionProgress;
    public AnimalProductionSettings ProductionSettings => productionSettings;
    public AnimalType GetAnimalType => animalType;
    public int AnimalCount => animalCount;
    public int MaxAnimalCount => maxAnimalCount;
    public string RequiredFood => requiredFood;

    //synergies
    [Header("Animal Settings")]
    [SerializeField] private float siloSynergyRange = 15f; // blocks
    [SerializeField] private float synergyFoodRequired = 0.8f; // food per animal when in range
    [SerializeField] private float normalFoodRequired = 1f; // food per animal when not in range
    [SerializeField] private float foodMultiplier = 1f; // food per animal when not in range
    [SerializeField] public int baseMoneyPerProduct = 10;
    [SerializeField] public int baseProductAmount = 1;


    protected override void Start()
    {
        base.Start();
        updateSiloSynergy();

        // Notify barracks to check for this coop
        BarracksStructure.UpdateAllNearbyChickenCoops();

        if (structureData != null && structureData.type != StructureType.Animal)
        {
            Debug.LogWarning($"{gameObject.name} has AnimalStructure script but StructureData.type is {structureData.type}, expected Animal.");
        }

        isProducing = false;
        productReady = false;
        productionProgress = 0f;
        if (productionSettings == null)
        {
            productionSettings = new AnimalProductionSettings();
        }

        nightManager = NightManager.Instance ?? FindObjectOfType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError($"{GetStructureName()} cannot find NightManager!");
        }

        lastCheckedHour = nightManager != null ? nightManager.Hours + (nightManager.Minutes / 60f) : 7f;
        requiredFood = GetRequiredFood();
        Debug.Log($"{GetStructureName()} initialized with {animalCount}/{maxAnimalCount} {animalType}s, requiredFood={requiredFood}, lastCheckedHour={lastCheckedHour}");

        if (nightManager != null)
        {
            nightManager.RegisterAnimalStructure(this);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning($"{gameObject.name}: AudioSource was missing, so one was added at runtime.-------------------------------------");
            }
        }
    }

    private void Update()
    {
        if (nightManager == null || !isProducing || productReady) return;

        float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
        float hourDelta = currentHour >= lastCheckedHour ? currentHour - lastCheckedHour : (24f - lastCheckedHour) + currentHour;
        productionProgress += hourDelta;
        lastCheckedHour = currentHour;

        if (productionProgress >= productionSettings.productionTime)
        {
            productionProgress = productionSettings.productionTime;
            Debug.Log($"{GetStructureName()} production progress complete, awaiting new day (05:00).");
        }
    }

    public void Feed()
    {
        if (nightManager == null || !nightManager.IsDay)
        {
            Debug.LogWarning($"{GetStructureName()} cannot feed: Feeding only allowed during the day!");
            return;
        }

        if (!isProducing && !productReady && animalCount > 0)
        {
            // int foodRequired = productionSettings.baseFoodRequired * animalCount * foodMultiplier;
            // int foodRequired = Mathf.RoundToInt((productionSettings.baseFoodRequired * animalCount) * foodMultiplier);
            int foodRequired = (int)((productionSettings.baseFoodRequired * animalCount) * foodMultiplier);
            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredFood, foodRequired))
            {
                InventoryManager.Instance.RemoveItem(requiredFood, foodRequired);
                isProducing = true;
                productionProgress = 0f;
                lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
                Debug.Log($"{GetStructureName()} fed with {foodRequired} {requiredFood} for {animalCount} {animalType}s.");
            }
            else
            {
                Debug.LogWarning($"{GetStructureName()} cannot feed: Need {foodRequired} {requiredFood}.");
            }
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot feed: Producing={isProducing}, ProductReady={productReady}, Animals={animalCount}.");
        }
    }

    private string GetRequiredFood()
    {
        switch (animalType)
        {
            case AnimalType.Chicken: return "Sunflower";
            case AnimalType.Cow: return "Wheat";
            case AnimalType.Sheep:
            case AnimalType.Goat:
            case AnimalType.Pig: return "Carrots";
            default: return "Unknown";
        }
    }

    public void Collect()
    {
        if (!productReady || nightManager == null || !nightManager.IsDay)
        {
            Debug.LogWarning($"{GetStructureName()} cannot collect: ProductReady={productReady}, IsDay={nightManager?.IsDay}.");
            return;
        }

        int totalProducts = productionSettings.productAmount * animalCount;
        int totalMoneyEarned = totalProducts * productionSettings.moneyPerProduct;
        Debug.Log($"{GetStructureName()} collecting {totalProducts} products from {animalCount} {animalType}s, earning {totalMoneyEarned} gold.");

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(totalMoneyEarned);
        }
        else
        {
            Debug.LogError($"{GetStructureName()} cannot add money: MoneyManager not found!");
        }

        productReady = false;
        isProducing = false;
        productionProgress = 0f;
        lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
    }

    public void BuyAnimals(int amount)
    {
        if (nightManager == null || !nightManager.IsDay)
        {
            Debug.LogWarning($"{GetStructureName()} cannot buy animals: Buying only allowed during the day!");
            return;
        }

        if (animalCount >= maxAnimalCount)
        {
            Debug.LogWarning($"{GetStructureName()} cannot buy animals: Max animal count reached ({animalCount}/{maxAnimalCount}).");
            return;
        }

        int availableSlots = maxAnimalCount - animalCount;
        int animalsToBuy = Mathf.Min(amount, availableSlots);
        int totalCost = animalsToBuy * productionSettings.costPerAnimal;

        if (MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(totalCost))
        {
            if (MoneyManager.Instance.SpendMoney(totalCost))
            {
                AddAnimals(animalsToBuy);
                Debug.Log($"{GetStructureName()} bought {animalsToBuy} {animalType}s for {totalCost} gold. Now: {animalCount}/{maxAnimalCount}.");
            }
            else
            {
                Debug.LogWarning($"{GetStructureName()} cannot buy {animalsToBuy} {animalType}s: SpendMoney failed.");
            }
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot buy {animalsToBuy} {animalType}s: Need {totalCost} gold.");
        }
    }

    public void OnNewDay()
    {
        if (isProducing && animalCount > 0)
        {
            productReady = true;
            isProducing = false;
            productionProgress = productionSettings.productionTime;
            Debug.Log($"{GetStructureName()} products ready to collect at 05:00 for {animalCount} {animalType}s.");
        }
    }

    public bool CanRecruit(int amount)
    {
        return animalCount >= amount;
    }

    public void RecruitAnimals(int amount)
    {
        if (CanRecruit(amount))
        {
            animalCount -= amount;
            Debug.Log($"{GetStructureName()} recruited {amount} {animalType}s. Remaining: {animalCount}/{maxAnimalCount}");
            OnAnimalCountChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot recruit: Need {amount}, have {animalCount} {animalType}s.");
        }
    }

    public void AddAnimals(int amount)
    {
        animalCount = Mathf.Min(animalCount + amount, maxAnimalCount);
        Debug.Log($"{GetStructureName()} added {amount} {animalType}s. Now: {animalCount}/{maxAnimalCount}");
        OnAnimalCountChanged?.Invoke();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (nightManager != null)
        {
            nightManager.UnregisterAnimalStructure(this);
            Debug.Log($"{GetStructureName()} unregistered from NightManager");
        }
    }

    //handles stuff for lees food if animals close to silo
    public void updateSiloSynergy()
    {
        SiloStructure[] silos = FindObjectsOfType<SiloStructure>();
        float minGridDistance = float.MaxValue;

        // Get the grid controller (assumes only one in scene)
        GridController gridController = FindObjectOfType<GridController>();

        if (gridController == null)
        {
            Debug.LogWarning("No GridController found for AnimalStructure synergy check.");
            foodMultiplier = normalFoodRequired;
            return;
        }

        Vector2Int animalCell = gridController.WorldToGridCoords(transform.position);

        foreach (SiloStructure silo in silos)
        {
            Vector2Int siloCell = gridController.WorldToGridCoords(silo.transform.position);
            float gridDistance = Vector2Int.Distance(animalCell, siloCell);

            if (gridDistance < minGridDistance)
            {
                minGridDistance = gridDistance;
            }
        }

        if (minGridDistance <= siloSynergyRange)
        {
            foodMultiplier = synergyFoodRequired;
        }
        else
        {
            foodMultiplier = normalFoodRequired;
        }
    }

    public static void UpdateAllAnimalSynergies()
    {
        foreach (var animal in FindObjectsOfType<AnimalStructure>())
        {
            animal.updateSiloSynergy();
        }
    }

    public void updateAnimalProductionAmount(string animalType, float increasePercent)
    {
        // Use the first letter(s) to match the type
        string thisType = GetAnimalType.ToString();
        bool matches = false;


        switch (animalType)
        {
            case "Ch":
                matches = thisType.StartsWith("Chicken");
                Debug.Log($"The animal product increase is for Chicken amount increased is {increasePercent}");
                break;
            case "C":
                matches = thisType.StartsWith("Cow");
                Debug.Log($"The animal product increase is for Cow amount increased is {increasePercent}");
                break;
            case "S":
                matches = thisType.StartsWith("Sheep");
                Debug.Log($"The animal product increase is for Sheep amount increased is {increasePercent}");
                break;
            case "G":
                matches = thisType.StartsWith("Goat");
                Debug.Log($"The animal product increase is for Goat amount increased is {increasePercent}");
                break;
            case "P":
                matches = thisType.StartsWith("Pig");
                Debug.Log($"The animal product increase is for Pig amount increased is {increasePercent}");
                break;
            default:
                Debug.LogWarning($"Unknown animal type: {animalType}");
                break;
        }

        if (matches)
        {
            productionSettings.moneyPerProduct = (int)(productionSettings.moneyPerProduct * increasePercent);
        }

    }

    public void resetAnimalProductionAmount()
    {
        // Reset to base value (adjust as needed)
        productionSettings.moneyPerProduct = baseMoneyPerProduct;
        productionSettings.productAmount = baseProductAmount;
    }

    public void PlayBackgroundNoise()
    {
        if (audioSource != null && backgroundClip != null)
        {
            audioSource.clip = backgroundClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    

    public void StopBackgroundNoise()
    {
        if (audioSource != null && backgroundClip != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}