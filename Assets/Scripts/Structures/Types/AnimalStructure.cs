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
        // Debug.Log("this is the amount of civilian animals we have: " + animalCount + "---------------------------------");
        if (nightManager == null || !isProducing || productReady) return;
        float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
        float hourDelta = currentHour >= lastCheckedHour ? currentHour - lastCheckedHour : (24f - lastCheckedHour) + currentHour;
        productionProgress += hourDelta;
        lastCheckedHour = currentHour;
        if (productionProgress >= productionSettings.productionTime) productionProgress = productionSettings.productionTime;
    }

    public void Feed()
    {
        if (nightManager == null || !nightManager.IsDay || isProducing || productReady || animalCount <= 0) return;
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
            Debug.Log($"[{animalType}] Started production with {originalAnimalCountWhenFed} animals");

            // Hide indicator during production
            if (readyIndicator != null)
                readyIndicator.HideIndicator();

            if (TutorialManager.Instance != null && 
                TutorialManager.Instance.IsTutorialActive() && 
                animalCount >= 3) 
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
        yield return new WaitForSeconds(2f);
        // Only allow instant complete if tutorial is active and feed_chickens step is not completed
        if (TutorialManager.Instance != null && 
            TutorialManager.Instance.IsTutorialActive() && 
            !TutorialManager.Instance.GetCompletedStepIds().Contains("feed_chickens"))
        {
            InstantCompleteProductionForTutorial();
            TutorialManager.Instance.Trigger(TutorialTrigger.FedFirstAnimals);
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
        ProductionBoosts productionBoosts = FindObjectOfType<ProductionBoosts>();
        int productPrice = 0;
        float boostedAmount = 0f;

        // Check if ProductionBoosts is found and properly initialized
        if (productionBoosts == null)
        {
            Debug.LogError("ProductionBoosts not found in scene! Using fallback values.");
            productPrice = baseMoneyPerProduct; // Use base price, seasonal bonuses won't work
            boostedAmount = 1f; // Default multiplier
        }
        else
        {
            int[] prices = productionBoosts.GetProductPrices();
            float[] boosts = productionBoosts.GetBoostedProducts();
            
            Debug.Log($"ProductionBoosts - Prices array length: {prices?.Length}, Boosts array length: {boosts?.Length}");

            if (animalType == AnimalType.Chicken)
            {
                // Use BASE price from ProductionBoosts (not the modified productionSettings price)
                if (prices != null && prices.Length > 0)
                    productPrice = prices[0]; // This is the base price from StructureData
                else
                {
                    Debug.LogWarning("ProductionBoosts prices array is empty! Using fallback base price.");
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 0)
                    boostedAmount = boosts[0];
                else
                {
                    Debug.LogWarning("ProductionBoosts boosts array is empty! Using default multiplier.");
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
                    Debug.LogWarning("ProductionBoosts prices array is too short for Cow! Using fallback base price.");
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 1)
                    boostedAmount = boosts[1];
                else
                {
                    Debug.LogWarning("ProductionBoosts boosts array is too short for Cow! Using default multiplier.");
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
                    Debug.LogWarning("ProductionBoosts prices array is too short for Sheep! Using fallback base price.");
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 2)
                    boostedAmount = boosts[2];
                else
                {
                    Debug.LogWarning("ProductionBoosts boosts array is too short for Sheep! Using default multiplier.");
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
                    Debug.LogWarning("ProductionBoosts prices array is too short for Goat! Using fallback base price.");
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 3)
                    boostedAmount = boosts[3];
                else
                {
                    Debug.LogWarning("ProductionBoosts boosts array is too short for Goat! Using default multiplier.");
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
                    Debug.LogWarning("ProductionBoosts prices array is too short for Pig! Using fallback base price.");
                    productPrice = baseMoneyPerProduct; // Use base price, not modified price
                }
                    
                if (boosts != null && boosts.Length > 4)
                    boostedAmount = boosts[4];
                else
                {
                    Debug.LogWarning("ProductionBoosts boosts array is too short for Pig! Using default multiplier.");
                    boostedAmount = 1f;
                }
            }
        }

        // Calculate production based on CURRENT animal count (not when fed)
        int productPerAnimal = (int)(productPrice * boostedAmount);
        int totalMoneyEarned = productPerAnimal * animalCount;

        // Debug collection calculation
        Debug.Log($"[{animalType}] Collection Debug - productPrice: {productPrice}, boostedAmount: {boostedAmount}, productPerAnimal: {productPerAnimal}, animalCount: {animalCount}, totalMoneyEarned: {totalMoneyEarned}");

        // Enhanced logging for production collection
        if (originalAnimalCountWhenFed != animalCount)
        {
            int lostProduction = productPerAnimal * (originalAnimalCountWhenFed - animalCount);
            Debug.Log($"[{animalType}] Collection - Originally fed {originalAnimalCountWhenFed} animals, now have {animalCount}");
            Debug.Log($"[{animalType}] Collection - Lost ${lostProduction} due to {originalAnimalCountWhenFed - animalCount} recruited animals");
            Debug.Log($"[{animalType}] Collection - Earning ${totalMoneyEarned} from {animalCount} remaining animals");
        }
        else
        {
            Debug.Log($"[{animalType}] Collection - Earning ${totalMoneyEarned} from {animalCount} animals (no animals recruited during production)");
        }

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(totalMoneyEarned, transform.position);
            TutorialManager.Instance?.Trigger(TutorialTrigger.CollectedFirstProducts);
        }
        else
        {
            Debug.LogError("MoneyManager.Instance is null when trying to collect money!");
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
        int animalsToBuy = Mathf.Min(amount, maxAnimalCount - animalCount);
        int totalCost = animalsToBuy * productionSettings.costPerAnimal;
        if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(totalCost))
        {
            AddAnimals(animalsToBuy);
            if (TutorialManager.Instance != null && animalCount >= 3) TutorialManager.Instance.Trigger(TutorialTrigger.BoughtFirstAnimals);
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
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
        {
            ResetInstantProductionState();
        }
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
            
            // Only reset production state if ALL animals are recruited away
            if (animalCount <= 0)
            {
                isProducing = false;
                productReady = false;
                productionProgress = 0f;
                originalAnimalCountWhenFed = 0; // Reset tracking
                
                if (readyIndicator != null)
                    readyIndicator.HideIndicator();
                    
                Debug.Log($"[{animalType}] All animals recruited - production stopped");
            }
            // If some animals remain and production is active, warn about reduced output
            else if (isProducing || productReady)
            {
                Debug.Log($"[{animalType}] Recruited {amount} animals during production. Production will yield from {animalCount} animals instead of {previousCount}");
                // Production continues but with reduced animal count for final calculation
            }
            
            OnAnimalCountChanged?.Invoke();
        }
    }

    public void AddAnimals(int amount)
    {
        if (amount <= 0) return;
        int actualAmountToAdd = Mathf.Min(amount, maxAnimalCount - animalCount);
        // int newCount = Mathf.Clamp(animalCount + amount, 0, maxAnimalCount);
        Debug.Log($"Adding {amount} animals. Before: {animalCount}, After: {animalCount + actualAmountToAdd}");
        if (actualAmountToAdd <= 0) return;
        // if (newCount == animalCount) return;
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
                
            Debug.Log($"[{animalType}] Animal count set to 0 - production stopped");
        }
        // If count changed during production, log the impact
        else if ((isProducing || productReady) && previousCount != animalCount)
        {
            Debug.Log($"[{animalType}] Animal count changed from {previousCount} to {animalCount} during production. Output will reflect current count.");
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
        foodMultiplier = minGridDistance <= siloSynergyRange ? synergyFoodRequired : normalFoodRequired;
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
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive() || !isProducing || animalCount <= 0) return;
        
        isProducing = false;
        productReady = true;
        productionProgress = 1f;
        OnAnimalCountChanged?.Invoke();
    }

    // Separate method for cheat manager that bypasses tutorial checks
    public void InstantCompleteProductionCheat()
    {
        if (!isProducing || animalCount <= 0) return;
        
        isProducing = false;
        productReady = true;
        productionProgress = 1f;
        OnAnimalCountChanged?.Invoke();
    }

    // Method to reset any inappropriate instant production states after tutorial ends
    public void ResetInstantProductionState()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
        {
            // If tutorial is not active, ensure no instant production persists
            // Keep normal production but reset any inappropriate ready states
            if (productReady && productionProgress < 1f && isProducing)
            {
                // This indicates an inappropriate instant production state
                productReady = false;
                productionProgress = 0f;
                Debug.Log($"Reset inappropriate instant production state on {animalType}");
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
        Debug.Log("Reset instant production states for all animals after tutorial ended");
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
    
    // Helper method to delay tutorial triggers and prevent race conditions
    private IEnumerator DelayedTutorialTrigger(TutorialTrigger trigger, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.Trigger(trigger);
        }
    }
}