using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimalStructure : Structure
{
    private static List<AnimalStructure> allAnimalStructures = new List<AnimalStructure>();
    private bool isRegisteredWithNightManager;
    public enum AnimalType { Chicken, Cow, Sheep, Goat, Pig }

    [Header("Animal Settings")]
    [SerializeField] private AnimalType animalType;
    [SerializeField] private bool isProducing;
    [SerializeField] private bool productReady;
    [SerializeField] private float productionProgress;
    [SerializeField] private AnimalProductionSettings productionSettings;
    [SerializeField] public int animalCount;
    [SerializeField] public int maxAnimalCount = 5;

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip backgroundClip;

    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 3f;
    [SerializeField] private float minVolume = 0.8f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    private Coroutine backgroundCoroutine;

    [System.Serializable]
    public class AnimalProductionSettings
    {
        public float productionTime = 24f;
        public int productAmount = 1;
        public int moneyPerProduct = 50;
        public int baseFoodRequired = 2;
        public int costPerAnimal = 50;
        public int boostedProduction;
    }

    private NightManager nightManager;
    private float lastCheckedHour;
    private string requiredFood;
    private ReadyIndicator readyIndicator;
    private int originalAnimalCountWhenFed; // Track count when production started

    public System.Action OnAnimalCountChanged;

    public bool IsProducing => isProducing;
    public bool ProductReady => productReady;
    public float ProductionProgress => productionProgress;
    public AnimalProductionSettings ProductionSettings => productionSettings;
    public AnimalType GetAnimalType => animalType;
    public int AnimalCount => animalCount;
    public int MaxAnimalCount => maxAnimalCount;
    public string RequiredFood => requiredFood;
    public int OriginalAnimalCountWhenFed => originalAnimalCountWhenFed;
    public bool HasLostAnimalsFromProduction => originalAnimalCountWhenFed > 0 && originalAnimalCountWhenFed != animalCount;

    [Header("Animal Synergies")]
    [SerializeField] public float siloSynergyRange = 15f;
    [SerializeField] public float synergyFoodRequired = 0.8f;
    [SerializeField] public float normalFoodRequired = 1f;
    [SerializeField] public float foodMultiplier = 1f;
    [SerializeField] public int baseMoneyPerProduct = 50;
    [SerializeField] public int baseProductMultiplier = 1;
    private bool activeSynergy = false;

    StructureData data;

    public int foodRequired;

    protected override void Start()
    {
        base.Start();

        data = GetData();


        productionSettings.costPerAnimal = data.costPerAnimal;
        productionSettings.moneyPerProduct = data.moneyPerProduct;
        productionSettings.baseFoodRequired = data.baseFoodRequired;
        synergyFoodRequired = data.foodSynergyMultiplier;

        siloSynergyRange = data.siloSynergyRange;


        // Register with static list for efficient lookups
        allAnimalStructures.Add(this);

        updateSiloSynergy();
        BarracksStructure.UpdateAllNearbyChickenCoops();
        isProducing = false;
        productReady = false;
        productionProgress = 0f;
        productionSettings ??= new AnimalProductionSettings();
        nightManager = NightManager.Instance ?? FindFirstObjectByType<NightManager>();
        lastCheckedHour = nightManager != null ? nightManager.Hours + (nightManager.Minutes / 60f) : 7f;
        requiredFood = GetRequiredFood();
        if (nightManager != null && !isRegisteredWithNightManager)
        {
            nightManager.RegisterAnimalStructure(this);
            isRegisteredWithNightManager = true;
        }
        audioSource = audioSource ?? GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // Initialize ready indicator
        readyIndicator = GetComponent<ReadyIndicator>();
        if (readyIndicator == null)
            readyIndicator = gameObject.AddComponent<ReadyIndicator>();
    }

    private void OnDisable()
    {
        // Unregister from static list when destroyed/disabled
        allAnimalStructures.Remove(this);
    }

    private void Update()
    {
        if (nightManager == null || !isProducing || productReady) return;
        float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
        float hourDelta = currentHour >= lastCheckedHour ? currentHour - lastCheckedHour : (24f - lastCheckedHour) + currentHour;
        productionProgress += hourDelta;
        lastCheckedHour = currentHour;
        if (productionProgress >= productionSettings.productionTime) productionProgress = productionSettings.productionTime;
    }

    public void Feed()
    {
        if (nightManager == null || !nightManager.IsDay || isProducing || productReady || animalCount <= 0) 
        {
            return;
        }
        
        int foodRequired = (int)((productionSettings.baseFoodRequired * animalCount) * foodMultiplier);
        if (foodRequired <= 0)
        {
            foodRequired = 1;
        }

        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredFood, foodRequired))
        {
            InventoryManager.Instance.RemoveItem(requiredFood, foodRequired);
            isProducing = true;
            productReady = false;
            productionProgress = 0f;
            lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
            
            // Track the animal count when production started
            originalAnimalCountWhenFed = animalCount;

            // Hide indicator during production
            if (readyIndicator != null)
                readyIndicator.HideIndicator();

            // Check simplified tutorial for instant production
            if (SimplifiedTutorialManager.Instance != null && 
                SimplifiedTutorialManager.Instance.IsTutorialActive() && 
                animalCount >= 1) // Instant production during tutorial
            {
                StartCoroutine(DelayedInstantCompleteForTutorial());
            }
        }
    }

    public bool canFeed()
    {
        if (nightManager == null || !nightManager.IsDay || isProducing || productReady || animalCount <= 0) return false;
        foodRequired = (int)((productionSettings.baseFoodRequired * animalCount) * foodMultiplier);
        if (foodRequired <= 0)
        {
            foodRequired = 1;
        }

        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredFood, foodRequired))
        {
            return true;
        }
        return false;
    }

    private IEnumerator DelayedInstantCompleteForTutorial()
    {
        yield return new WaitForSeconds(1f);
        
        // Check simplified tutorial instead of old tutorial
        if (SimplifiedTutorialManager.Instance != null && 
            SimplifiedTutorialManager.Instance.IsTutorialActive())
        {
            InstantCompleteProductionForTutorial();
            
            // Brief wait for production completion visual feedback
            yield return new WaitForSeconds(1f);
            
            // Call simplified tutorial helper
            TutorialTriggerHelper.TriggerChickensFed();
        }
    }

    public string GetRequiredFood()
    {
        return animalType switch
        {
            AnimalType.Chicken => "Sunflower",
            AnimalType.Cow or AnimalType.Sheep => "Wheat",
            AnimalType.Goat or AnimalType.Pig => "Carrots",
            _ => "Unknown"
        };
    }

    public void Collect()
    {
        if (!productReady || nightManager == null || !nightManager.IsDay) return;
        ProductionBoosts productionBoosts = FindFirstObjectByType<ProductionBoosts>();
        int productPrice = 0;
        float boostedAmount = 0f;

        // Check if ProductionBoosts is found and properly initialized
        if (productionBoosts == null)
        {
            productPrice = baseMoneyPerProduct; // Use base price, seasonal bonuses won't work
            boostedAmount = 1f; // Default multiplier
        }
        else
        {
            int[] prices = productionBoosts.GetProductPrices();
            float[] boosts = productionBoosts.GetBoostedProducts();

            if (animalType == AnimalType.Chicken)
            {
                // Use BASE price from ProductionBoosts (not the modified productionSettings price)
                if (prices != null && prices.Length > 0)
                    productPrice = prices[0]; // This is the base price from StructureData
                else
                {
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 0)
                    boostedAmount = boosts[0];
                else
                {
                    boostedAmount = 1f;
                }
            }
            else if (animalType == AnimalType.Cow)
            {
                // Use BASE price from ProductionBoosts (not the modified productionSettings price)
                if (prices != null && prices.Length > 1)
                    productPrice = prices[1]; // This is the base price from StructureData
                else
                {
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 1)
                    boostedAmount = boosts[1];
                else
                {
                    boostedAmount = 1f;
                }
            }
            else if (animalType == AnimalType.Sheep)
            {
                // Use BASE price from ProductionBoosts (not the modified productionSettings price)
                if (prices != null && prices.Length > 2)
                    productPrice = prices[2]; // This is the base price from StructureData
                else
                {
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 2)
                    boostedAmount = boosts[2];
                else
                {
                    boostedAmount = 1f;
                }
            }
            else if (animalType == AnimalType.Goat)
            {
                // Use BASE price from ProductionBoosts (not the modified productionSettings price)
                if (prices != null && prices.Length > 3)
                    productPrice = prices[3]; // This is the base price from StructureData
                else
                {
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 3)
                    boostedAmount = boosts[3];
                else
                {
                    boostedAmount = 1f;
                }
            }
            else if (animalType == AnimalType.Pig)
            {
                // Use BASE price from ProductionBoosts (not the modified productionSettings price)
                if (prices != null && prices.Length > 4)
                    productPrice = prices[4]; // This is the base price from StructureData
                else
                {
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 4)
                    boostedAmount = boosts[4];
                else
                {
                    boostedAmount = 1f;
                }
            }
        }

        // Calculate production based on CURRENT animal count with accurate bonus breakdown
        int baseProductPerAnimal = productPrice; // Base amount without any boosts
        int baseMoneyEarned = baseProductPerAnimal * animalCount;
        
        // Calculate production boost bonus (from ProductionBoosts system)
        int productionBonus = 0;
        if (boostedAmount > 1f)
        {
            int boostedProductPerAnimal = (int)(productPrice * boostedAmount);
            int boostedMoneyEarned = boostedProductPerAnimal * animalCount;
            productionBonus = boostedMoneyEarned - baseMoneyEarned;
        }
        
        // Note: Synergy affects food consumption (20% less food needed), not money collection
        // Note: Seasonal bonuses would come from the ProductionBoosts system as boostedAmount
        
        // Total calculation
        int totalMoneyEarned = baseMoneyEarned + productionBonus;

        // Debug logging for bonus calculation
        Debug.Log($"[AnimalStructure] Bonus Debug: productPrice={productPrice}, boostedAmount={boostedAmount}, animalCount={animalCount}, animalType={animalType}");
        Debug.Log($"[AnimalStructure] Bonus Debug: baseMoneyEarned={baseMoneyEarned}, productionBonus={productionBonus}, totalMoneyEarned={totalMoneyEarned}");

        // Enhanced logging for production collection
        if (originalAnimalCountWhenFed != animalCount)
        {
            int lostProduction = (int)(productPrice * boostedAmount) * (originalAnimalCountWhenFed - animalCount);
        }

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(totalMoneyEarned, transform.position);
            
            // Show floating money text with production bonus only (synergy affects food, not money)
            if (productionBonus > 0)
            {
                ReadyIndicator.ShowFloatingNumberWithBonus(transform.position, baseMoneyEarned, productionBonus, Color.yellow);
            }
            else
            {
                ReadyIndicator.ShowFloatingNumber(transform.position, totalMoneyEarned, Color.yellow);
            }
            
            // NEW: Show notification for animal collection with boost info
            if (NotificationManager.Instance != null)
            {
                string animalName = animalType.ToString();
                if (string.IsNullOrEmpty(animalName) || animalName == "None")
                {
                    animalName = "Animal"; // Fallback for uninitialized types
                }
                
                // Handle plurals properly
                string displayName = animalCount == 1 ? animalName : animalName + "s";
                string baseMessage = $"${totalMoneyEarned} from {animalCount} {displayName}";
                
                // DISABLED: Per-collection notifications - keeping production boosts only in NightManager
                // Check for production boost
                // if (boostedAmount > 1f)
                // {
                //     int bonusPercent = Mathf.RoundToInt((boostedAmount - 1f) * 100f);
                //     baseMessage += $" • +{bonusPercent}% season bonus!";
                //     NotificationManager.ShowAchievement($"{displayName} Boosted!", baseMessage);
                // }
                // // Check for silo synergy (reduced food cost)
                // else if (foodMultiplier < normalFoodRequired)
                // {
                //     int savings = Mathf.RoundToInt((1f - (foodMultiplier / normalFoodRequired)) * 100f);
                //     baseMessage += $" • {savings}% less feed used!";
                //     // Shorter duration for routine successes
                //     NotificationManager.ShowSuccess($"{displayName} Collected!", baseMessage, 2.5f);
                // }
                // else
                // {
                //     // NEW: Show error when no boosts are active - show earnings and missed potential
                //     int missedSavings = Mathf.RoundToInt((1f - (synergyFoodRequired / normalFoodRequired)) * 100f);
                //     NotificationManager.ShowError($"{displayName} No Boosts!", $"${totalMoneyEarned} earned • Missing {missedSavings}% feed savings!");
                // }
            }
            
            // Trigger simplified tutorial for egg collection
            if (SimplifiedTutorialManager.Instance != null && 
                SimplifiedTutorialManager.Instance.IsTutorialActive())
            {
                TutorialTriggerHelper.TriggerEggsCollected();
            }
        }
        else
        {
            // MoneyManager is null - cannot add money
        }
        productReady = false;
        isProducing = false;
        productionProgress = 0f;
        originalAnimalCountWhenFed = 0; // Reset tracking

        // Hide ready indicator after collection
        if (readyIndicator != null)
            readyIndicator.HideIndicator();
        
        lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
    }

    public void BuyAnimals(int amount)
    {
        if (nightManager == null || !nightManager.IsDay || animalCount >= maxAnimalCount) return;
        if (nightManager.getIsPaused()) return;
        
        // Tutorial restriction: prevent buying more than 5 animals during buy_chickens step
        if (SimplifiedTutorialManager.Instance != null && SimplifiedTutorialManager.Instance.IsTutorialActive())
        {
            // During buy_chickens step, limit to 5 animals max
            if (animalCount + amount > 5)
            {
                amount = 5 - animalCount;
                if (amount <= 0)
                {
                    return;
                }
            }
        }
        
        int animalsToBuy = Mathf.Min(amount, maxAnimalCount - animalCount);
        int totalCost = animalsToBuy * productionSettings.costPerAnimal;
        if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(totalCost))
        {
            AddAnimals(animalsToBuy);
            
            // Trigger simplified tutorial when 5 chickens bought
            if (SimplifiedTutorialManager.Instance != null && 
                SimplifiedTutorialManager.Instance.IsTutorialActive() && 
                animalCount >= 5)
            {
                TutorialTriggerHelper.TriggerChickensBought();
            }
        }
    }

    public void OnMoved()
    {
        CivilianSpawner spawner = GetComponentInChildren<CivilianSpawner>();
        if (spawner != null)
        {
            spawner.RespawnAnimals();
        }
    }

    public void OnNewDay()
    {
        if (isProducing && animalCount > 0)
        {
            productReady = true;
            isProducing = false;
            productionProgress = productionSettings.productionTime;

            // Show ready indicator when production is complete
            if (readyIndicator != null)
                readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Collect);
        }
        
        // Safety check: Reset any inappropriate instant production states on new day
        // if tutorial is not active (prevents persistence across days)
        if (SimplifiedTutorialManager.Instance == null || !SimplifiedTutorialManager.Instance.IsTutorialActive())
        {
            ResetInstantProductionState();
        }
    }
    
    private int GetCollectAmount()
    {
        // Calculate the money that would be earned from collection
        // This mirrors the logic in Collect() method
        ProductionBoosts productionBoosts = FindFirstObjectByType<ProductionBoosts>();
        int productPrice = baseMoneyPerProduct;
        float boostedAmount = 1f;

        // Check if ProductionBoosts is found and get the appropriate boost
        if (productionBoosts != null)
        {
            int[] prices = productionBoosts.GetProductPrices();
            float[] boosts = productionBoosts.GetBoostedProducts();
            
            int animalIndex = (int)animalType;
            if (prices != null && prices.Length > animalIndex)
                productPrice = prices[animalIndex];
            
            if (boosts != null && boosts.Length > animalIndex)
                boostedAmount = boosts[animalIndex];
        }

        int productPerAnimal = (int)(productPrice * boostedAmount);
        return productPerAnimal * animalCount;
    }

    public bool CanRecruit(int amount)
    {
        return animalCount >= amount;
    }

    // Overloaded method that provides recruitment impact information
    public bool CanRecruit(int amount, out string impactWarning)
    {
        impactWarning = "";
        
        if (animalCount < amount)
        {
            impactWarning = $"Cannot recruit {amount} animals - only have {animalCount}";
            return false;
        }
        
        if ((isProducing || productReady) && originalAnimalCountWhenFed > 0)
        {
            int remainingAfterRecruitment = animalCount - amount;
            if (remainingAfterRecruitment <= 0)
            {
                impactWarning = $"⚠️ Warning: Recruiting all {amount} animals will cancel ongoing production!";
            }
            else
            {
                impactWarning = $"⚠️ Warning: Recruiting {amount} animals will reduce production output from {originalAnimalCountWhenFed} to {remainingAfterRecruitment} animals.";
            }
        }
        
        return true;
    }

    public void RecruitAnimals(int amount)
    {
        if (CanRecruit(amount))
        {
            int previousCount = animalCount;
            animalCount -= amount;

            CivilianSpawner spawner = GetComponentInChildren<CivilianSpawner>();
            if (spawner != null)
                spawner.DespawnAnimals(animalCount);
            
            // Only reset production state if ALL animals are recruited away
            if (animalCount <= 0)
            {
                isProducing = false;
                productReady = false;
                productionProgress = 0f;
                originalAnimalCountWhenFed = 0; // Reset tracking
                
                if (readyIndicator != null)
                    readyIndicator.HideIndicator();
            }
            // If some animals remain and production is active, warn about reduced output
            else if (isProducing || productReady)
            {
                // Production continues but with reduced animal count for final calculation
            }
            
            OnAnimalCountChanged?.Invoke();
        }
    }

    public void AddAnimals(int amount)
    {
        if (amount <= 0) return;
        int actualAmountToAdd = Mathf.Min(amount, maxAnimalCount - animalCount);
        if (actualAmountToAdd <= 0) return;
        animalCount += actualAmountToAdd;
        OnAnimalCountChanged?.Invoke();
    }

    public void SetAnimalCount(int count)
    {
        int previousCount = animalCount;
        animalCount = Mathf.Clamp(count, 0, maxAnimalCount);
        
        // Only reset production state if no animals remain
        if (animalCount <= 0)
        {
            isProducing = false;
            productReady = false;
            productionProgress = 0f;
            originalAnimalCountWhenFed = 0; // Reset tracking
            
            if (readyIndicator != null)
                readyIndicator.HideIndicator();
        }
        // If count changed during production, log the impact
        else if ((isProducing || productReady) && previousCount != animalCount)
        {
            // Animal count changed during production - output will reflect current count
        }
        
        OnAnimalCountChanged?.Invoke();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clean up background coroutine to prevent memory leaks
        if (backgroundCoroutine != null)
        {
            StopCoroutine(backgroundCoroutine);
            backgroundCoroutine = null;
        }

        if (nightManager != null && isRegisteredWithNightManager)
        {
            nightManager.UnregisterAnimalStructure(this);
            isRegisteredWithNightManager = false;
        }
    }

    public void updateSiloSynergy()
    {
        SiloStructure[] silos = FindObjectsByType<SiloStructure>(FindObjectsSortMode.None);
        float minGridDistance = float.MaxValue;
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            foodMultiplier = normalFoodRequired;
            return;
        }

        Vector2Int animalCell = gridController.WorldToGridCoords(transform.position);

        foreach (SiloStructure silo in silos)
        {
            Vector2Int siloCell = gridController.WorldToGridCoords(silo.transform.position);
            float gridDistance = Vector2Int.Distance(animalCell, siloCell);
            minGridDistance = Mathf.Min(minGridDistance, gridDistance);
        }
        
        float oldMultiplier = foodMultiplier;
        if(minGridDistance <= siloSynergyRange)
        {
            activeSynergy = true;
        }
        else
        {
            activeSynergy = false;
        }

        foodMultiplier = minGridDistance <= siloSynergyRange ? synergyFoodRequired : normalFoodRequired;
        
        // REMOVED: Synergy notifications are now shown in collection notifications instead
        // This avoids temporary/spam notifications
    }

    public bool isSynergyActive()
    {
        return activeSynergy;
    }

    public static void UpdateAllAnimalSynergies()
    {
        // Use static list instead of expensive FindObjectsByType
        foreach (var animal in allAnimalStructures)
            animal.updateSiloSynergy();
    }

    public void updateAnimalProductionAmount(string animalType, float increasePercent)
    {
        string thisType = GetAnimalType.ToString();
        bool matches = animalType switch
        {
            "Ch" => thisType.StartsWith("Chicken"),
            "C" => thisType.StartsWith("Cow"),
            "S" => thisType.StartsWith("Sheep"),
            "G" => thisType.StartsWith("Goat"),
            "P" => thisType.StartsWith("Pig"),
            _ => false
        };
        if (matches)
        {
            productionSettings.moneyPerProduct = (int)(baseMoneyPerProduct * increasePercent);
            productionSettings.boostedProduction = increasePercent == 2f ? 2 : 1;
        }
    }

    public void resetAnimalProductionAmount()
    {
        productionSettings.moneyPerProduct = baseMoneyPerProduct;
        productionSettings.boostedProduction = baseProductMultiplier;
    }

    public void PlayBackgroundNoise()
    {
        if (backgroundClip != null && audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        {
            StopBackgroundNoise(); // stop any existing loop
            backgroundCoroutine = StartCoroutine(PlayBackgroundClipRandomly(backgroundClip));
        }
    }

    private IEnumerator PlayBackgroundClipRandomly(AudioClip clip)
    {
        while (true)
        {
            // Check if audio source is still valid and enabled before playing
            if (audioSource == null || !audioSource.enabled || !audioSource.gameObject.activeInHierarchy)
            {
                yield break; // Exit the coroutine if audio source is disabled
            }
            
            float targetVolume = Random.Range(minVolume, maxVolume);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.clip = clip;
            audioSource.volume = 0f;
            audioSource.Play();

            // Fade in
            float fadeInTime = 0.5f;
            for (float t = 0; t < fadeInTime; t += Time.deltaTime)
            {
                // Check if audio source is still valid during fade
                if (audioSource == null || !audioSource.enabled || !audioSource.gameObject.activeInHierarchy)
                {
                    yield break; // Exit if audio source becomes invalid
                }
                audioSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeInTime);
                yield return null;
            }
            audioSource.volume = targetVolume;

            // Wait for clip duration minus fade in/out
            yield return new WaitForSeconds(clip.length - fadeInTime);

            // Fade out
            float fadeOutTime = 0.5f;
            for (float t = 0; t < fadeOutTime; t += Time.deltaTime)
            {
                // Check if audio source is still valid during fade out
                if (audioSource == null || !audioSource.enabled || !audioSource.gameObject.activeInHierarchy)
                {
                    yield break; // Exit if audio source becomes invalid
                }
                audioSource.volume = Mathf.Lerp(targetVolume, 0f, t / fadeOutTime);
                yield return null;
            }
            
            // Final check before stopping
            if (audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
            {
                audioSource.Stop();
            }

            // Random delay before next loop
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }

    public void StopBackgroundNoise()
    {
        if (backgroundCoroutine != null)
        {
            StopCoroutine(backgroundCoroutine);
            backgroundCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }


    public static int[] whichProductsAreBoosted(string[] animals)
    {
        int[] boosted = new int[animals.Length];
        AnimalStructure[] allStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        for (int i = 0; i < animals.Length; i++)
            foreach (var structure in allStructures)
                if (structure.GetAnimalType.ToString().Equals(animals[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    boosted[i] = structure.productionSettings.boostedProduction switch
                    {
                        0 => 0,
                        1 => 50,
                        _ => 100
                    };
                    break;
                }
        return boosted;
    }

    public void InstantCompleteProductionForTutorial()
    {
        // Only allow instant production during active tutorial
        if (SimplifiedTutorialManager.Instance == null || !SimplifiedTutorialManager.Instance.IsTutorialActive() || !isProducing || animalCount <= 0) return;
        
        isProducing = false;
        productReady = true;
        productionProgress = 1f;
        OnAnimalCountChanged?.Invoke();
        
        // Show ready indicator when production is complete
        if (readyIndicator != null)
            readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Collect);
    }

    // Separate method for cheat manager that bypasses tutorial checks
    public void InstantCompleteProductionCheat()
    {
        if (!isProducing || animalCount <= 0) return;
        
        isProducing = false;
        productReady = true;
        productionProgress = 1f;
        OnAnimalCountChanged?.Invoke();
        
        // Show ready indicator when production is complete
        if (readyIndicator != null)
            readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Collect);
    }

    // Method to reset any inappropriate instant production states after tutorial ends
    public void ResetInstantProductionState()
    {
        if (SimplifiedTutorialManager.Instance == null || !SimplifiedTutorialManager.Instance.IsTutorialActive())
        {
            // If tutorial is not active, ensure no instant production persists
            // Keep normal production but reset any inappropriate ready states
            if (productReady && productionProgress < 1f && isProducing)
            {
                // This indicates an inappropriate instant production state
                productReady = false;
                productionProgress = 0f;
            }
        }
    }

    // Static method to reset all animals' instant production states (call when tutorial ends)
    public static void ResetAllInstantProductionStates()
    {
        AnimalStructure[] allAnimals = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        foreach (var animal in allAnimals)
        {
            animal.ResetInstantProductionState();
        }
    }

    // Get information about recruitment impact on production
    public string GetProductionImpactInfo()
    {
        if (!isProducing && !productReady)
            return "No active production";
            
        if (originalAnimalCountWhenFed == 0)
            return "Production tracking not available";
            
        if (originalAnimalCountWhenFed == animalCount)
            return $"Production will yield from all {animalCount} animals";
            
        int lostAnimals = originalAnimalCountWhenFed - animalCount;
        if (animalCount > 0)
            return $"⚠️ Production reduced: Started with {originalAnimalCountWhenFed} animals, now have {animalCount}. Lost production from {lostAnimals} recruited animals.";
        else
            return $"❌ Production lost: All {originalAnimalCountWhenFed} animals were recruited away.";
    }

    public void SetProductionState(bool isProducing, bool productReady, float productionProgress/*, float lastCheckedHour*/)
    {
        this.isProducing = isProducing;
        this.productReady = productReady;
        this.productionProgress = productionProgress;
        // Optionally:
        // this.lastCheckedHour = lastCheckedHour;
        OnAnimalCountChanged?.Invoke();
    }
    
    // Helper method to delay tutorial triggers and prevent race conditions (kept for other uses)
    private IEnumerator DelayedTutorialTrigger(TutorialTrigger trigger, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.Trigger(trigger);
        }
    }

    private float plantedAtHour = -1f; 
    private float cropGrowthProgress = 0f; 
    private bool wasGrowing = false;

    public void TrackGrowth(NightManager nightManager)
    {
        if (!isProducing) 
        {
            cropGrowthProgress = productReady ? 1f : 0f;
            plantedAtHour = -1f;
            wasGrowing = false;
            return;
        }

        float currentHour = nightManager.Hours + nightManager.Minutes / 60f;

        // Detect newly planted crop
        if (!wasGrowing)
        {
            plantedAtHour = currentHour;
            cropGrowthProgress = 0f;
        }

        wasGrowing = true;

        // Calculate hours elapsed
        float hoursElapsed = currentHour >= plantedAtHour 
            ? currentHour - plantedAtHour 
            : (24f - plantedAtHour) + currentHour;

        // Crops grow until 5 AM next day
        float totalHoursNeeded = (24f - plantedAtHour) + 5f; 

        cropGrowthProgress = Mathf.Clamp01(hoursElapsed / totalHoursNeeded);

        // Automatically mark ready if fully grown
        if (cropGrowthProgress >= 1f)
        {
            productReady = true;
            isProducing = false;
            cropGrowthProgress = 1f;
            
            // Show ready indicator when production is complete
            if (readyIndicator != null)
                readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Collect);
        }
    }

    public float GetGrowthProgress() => cropGrowthProgress; // 0..1

}