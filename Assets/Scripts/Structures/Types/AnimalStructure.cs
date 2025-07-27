using UnityEngine;
using System.Collections;

public class AnimalStructure : Structure
{
    // Registration flag to prevent double registration with NightManager
    private bool isRegisteredWithNightManager = false;
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
    [SerializeField] private int animalCount = 0;
    [SerializeField] private int maxAnimalCount = 5;

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
        public int moneyPerProduct = 50;
        public int baseFoodRequired = 2;
        public int costPerAnimal = 50;
        public int boostedProduction = 0;
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
    [SerializeField] public int baseMoneyPerProduct = 50;
    [SerializeField] public int baseProductMultiplier = 1;

    protected override void Start()
    {
        base.Start();
        updateSiloSynergy();

        // Notify barracks to check for this coop
        BarracksStructure.UpdateAllNearbyChickenCoops();

        if (structureData != null && structureData.type != StructureType.Animal)
        {
            }

        isProducing = false;
        productReady = false;
        productionProgress = 0f;
        if (productionSettings == null)
        {
            productionSettings = new AnimalProductionSettings();
        }

        nightManager = NightManager.Instance ?? FindFirstObjectByType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError($"{GetStructureName()} cannot find NightManager!");
        }

        lastCheckedHour = nightManager != null ? nightManager.Hours + (nightManager.Minutes / 60f) : 7f;
        requiredFood = GetRequiredFood();
        if (nightManager != null && !isRegisteredWithNightManager)
        {
            nightManager.RegisterAnimalStructure(this);
            isRegisteredWithNightManager = true;
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
            }
    }
    
    public void DebugProductionSettings()
{
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

        
        ProductionBoosts productionBoosts = FindObjectOfType<ProductionBoosts>();
        int productPrice = 0;
        float boostedAmount = 0f;

        if (animalType == AnimalType.Chicken)
        {
            productPrice = productionBoosts.GetProductPrices()[0];
            boostedAmount = productionBoosts.GetBoostedProducts()[0];
        }
        else if (animalType == AnimalType.Cow)
        {
            productPrice = productionBoosts.GetProductPrices()[1];
            boostedAmount = productionBoosts.GetBoostedProducts()[1];
        }
        else if (animalType == AnimalType.Sheep)
        {
            productPrice = productionBoosts.GetProductPrices()[2];
            boostedAmount = productionBoosts.GetBoostedProducts()[2];
        }
        else if (animalType == AnimalType.Goat)
        {
            productPrice = productionBoosts.GetProductPrices()[3];
            boostedAmount = productionBoosts.GetBoostedProducts()[3];
        }
        else if (animalType == AnimalType.Pig)
        {
            productPrice = productionBoosts.GetProductPrices()[4];
            boostedAmount = productionBoosts.GetBoostedProducts()[4];
        }



        // int totalProducts = productionSettings.productAmount * animalCount;
        int totalProducts = (int)(productPrice * boostedAmount);
        // int totalMoneyEarned = totalProducts * productionSettings.moneyPerProduct;
        int totalMoneyEarned = totalProducts * animalCount;
        // int totalMoneyEarned = totalProducts;

        // Debug.Log($"products: {totalProducts}, product price: {productPrice}, money earned: {totalMoneyEarned}");
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
        OnAnimalCountChanged?.Invoke();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (nightManager != null && isRegisteredWithNightManager)
        {
            nightManager.UnregisterAnimalStructure(this);
            isRegisteredWithNightManager = false;
        }
    }

    //handles stuff for lees food if animals close to silo
    public void updateSiloSynergy()
    {
        SiloStructure[] silos = FindObjectsByType<SiloStructure>(FindObjectsSortMode.None);
        float minGridDistance = float.MaxValue;

        // Get the grid controller (assumes only one in scene)
        GridController gridController = FindFirstObjectByType<GridController>();

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
        foreach (var animal in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
        {
            animal.updateSiloSynergy();
        }
    }

    public void updateAnimalProductionAmount(string animalType, float increasePercent)
    {
        // Use the first letter(s) to match the type
        string thisType = GetAnimalType.ToString();
        bool matches = false;
        bool doubleBoosted = false;

        if (increasePercent == 2f)
        {
            doubleBoosted = true;
        }

        switch (animalType)
        {
            case "Ch":
                matches = thisType.StartsWith("Chicken");
                break;
            case "C":
                matches = thisType.StartsWith("Cow");
                break;
            case "S":
                matches = thisType.StartsWith("Sheep");
                break;
            case "G":
                matches = thisType.StartsWith("Goat");
                break;
            case "P":
                matches = thisType.StartsWith("Pig");
                break;
            default:
                Debug.LogWarning($"Unknown animal type: {animalType}");
                break;
        }

        if (matches)
        {
            // productionSettings.moneyPerProduct = (int)(productionSettings.moneyPerProduct * increasePercent);
            productionSettings.moneyPerProduct = (int)(baseMoneyPerProduct * increasePercent);

            if (doubleBoosted)
            {
                productionSettings.boostedProduction = 2;
                }
            else
            {
                productionSettings.boostedProduction = 1;
            }

            }
    }

    public void resetAnimalProductionAmount()
    {
        // Reset to base value (adjust as needed)
        productionSettings.moneyPerProduct = baseMoneyPerProduct;
        productionSettings.boostedProduction = baseProductMultiplier;
        // productionSettings.productAmount = baseProductAmount;
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

    // public int[] getProductPrices(string[] animals)
    // {
    //     int[] prices = new int[animals.Length];

    //     for (int k = 0; k < animals.Length; k++)
    //     {
    //         // Compare the requested animal type with this structure's animal type
    //         if (GetAnimalType.ToString().Equals(animals[k], System.StringComparison.OrdinalIgnoreCase))
    //         {
    //             prices[k] = productionSettings.moneyPerProduct;
    //             //         }
    //         else
    //         {
    //             // If this structure doesn't match, you might want to return 0 or -1
    //                 //             prices[k] = 0;
    //         }
    //     }

    //     return prices;
    // }

    // public static int[] getProductPrices(string[] animals)
    // {
    //     int[] prices = new int[animals.Length];
    //     AnimalStructure[] allStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);

    //     for (int i = 0; i < animals.Length; i++)
    //     {
    //         foreach (var structure in allStructures)
    //         {
    //             if (structure.GetAnimalType.ToString().Equals(animals[i], System.StringComparison.OrdinalIgnoreCase))
    //             {
    //                 prices[i] = structure.productionSettings.moneyPerProduct;
    //                 break; // Found the structure for this animal type, move to next animal
    //             }
    //         }
    //     }
    //     return prices;
    // }

    // public int[] whichProductsAreBoosted(string[] animals)
    // {
    //     int[] boosted = new int[animals.Length];

    //     for (int k = 0; k < animals.Length; k++)
    //     {
    //         // Compare the requested animal type with this structure's animal type
    //         if (GetAnimalType.ToString().Equals(animals[k], System.StringComparison.OrdinalIgnoreCase))
    //         {
    //             if (productionSettings.boostedProduction == 0)
    //             {
    //                 boosted[k] = 0;
    //             }
    //             else if (productionSettings.boostedProduction == 1)
    //             {
    //                 boosted[k] = 50;
    //             }
    //             else
    //             {
    //                 boosted[k] = 100;
    //             }
    //         }
    //         else
    //         {
    //             // If this structure doesn't match, you might want to return 0 or -1
    //                 //             boosted[k] = 0;
    //         }
    //     }

    //     return boosted;
    // }
    
    public static int[] whichProductsAreBoosted(string[] animals)
{
    int[] boosted = new int[animals.Length];
    AnimalStructure[] allStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);

    for (int i = 0; i < animals.Length; i++)
    {
        foreach (var structure in allStructures)
        {
            if (structure.GetAnimalType.ToString().Equals(animals[i], System.StringComparison.OrdinalIgnoreCase))
            {
                if (structure.productionSettings.boostedProduction == 0)
                {
                    boosted[i] = 0;
                }
                else if (structure.productionSettings.boostedProduction == 1)
                {
                    boosted[i] = 50;
                }
                else
                {
                    boosted[i] = 100;
                }
                break; // Found the structure for this animal type, move to next animal
            }
        }
    }

    Debug.Log($"Boosted products for {string.Join(", ", animals)}: {string.Join(", ", boosted)}");
    return boosted;
}

/// <summary>
    /// Tutorial-only method to instantly complete animal production
    /// </summary>
    public void InstantCompleteProductionForTutorial()
    {
        if (isProducing && !productReady && animalCount > 0)
        {
            productionProgress = productionSettings.productionTime;
            productReady = true;
            isProducing = false;
            Debug.Log($"TUTORIAL: Instantly completed production for {animalType} with {animalCount} animals");
        }
    }
}