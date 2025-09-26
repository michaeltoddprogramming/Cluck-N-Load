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
    [SerializeField] private int animalCount;
    [SerializeField] private int maxAnimalCount = 5;

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

    public System.Action OnAnimalCountChanged;

    public bool IsProducing => isProducing;
    public bool ProductReady => productReady;
    public float ProductionProgress => productionProgress;
    public AnimalProductionSettings ProductionSettings => productionSettings;
    public AnimalType GetAnimalType => animalType;
    public int AnimalCount => animalCount;
    public int MaxAnimalCount => maxAnimalCount;
    public string RequiredFood => requiredFood;

    [Header("Animal Synergies")]
    [SerializeField] public float siloSynergyRange = 15f;
    [SerializeField] public float synergyFoodRequired = 0.8f;
    [SerializeField] public float normalFoodRequired = 1f;
    [SerializeField] public float foodMultiplier = 1f;
    [SerializeField] public int baseMoneyPerProduct = 50;
    [SerializeField] public int baseProductMultiplier = 1;

    StructureData data;

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
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredFood, foodRequired))
        {
            InventoryManager.Instance.RemoveItem(requiredFood, foodRequired);
            isProducing = true;
            productReady = false;
            productionProgress = 0f;
            lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);

            // Hide indicator during production
            if (readyIndicator != null)
                readyIndicator.HideIndicator();

            if (TutorialManager.Instance != null && animalCount >= 3) StartCoroutine(DelayedInstantCompleteForTutorial());
        }
    }

    public bool canFeed()
    {
        if (nightManager == null || !nightManager.IsDay || isProducing || productReady || animalCount <= 0) return false;
        int foodRequired = (int)((productionSettings.baseFoodRequired * animalCount) * foodMultiplier);
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredFood, foodRequired))
        {
            return true;
        }
        return false;
    }

    private IEnumerator DelayedInstantCompleteForTutorial()
    {
        yield return new WaitForSeconds(2f);
        // Only allow instant complete if tutorial step is not completed
        if (TutorialManager.Instance != null && !TutorialManager.Instance.GetCompletedStepIds().Contains("feed_chickens"))
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

        if (animalType == AnimalType.Chicken)
        {
            // productPrice = productionSettings.moneyPerProduct;
            productPrice = productionBoosts.GetProductPrices()[0];
            boostedAmount = productionBoosts.GetBoostedProducts()[0];
        }
        else if (animalType == AnimalType.Cow)
        {
            // productPrice = productionSettings.moneyPerProduct;
            productPrice = productionBoosts.GetProductPrices()[1];
            boostedAmount = productionBoosts.GetBoostedProducts()[1];
        }
        else if (animalType == AnimalType.Sheep)
        {
            // productPrice = productionSettings.moneyPerProduct;
            productPrice = productionBoosts.GetProductPrices()[2];
            boostedAmount = productionBoosts.GetBoostedProducts()[2];
        }
        else if (animalType == AnimalType.Goat)
        {
            // productPrice = productionSettings.moneyPerProduct;
            productPrice = productionBoosts.GetProductPrices()[3];
            boostedAmount = productionBoosts.GetBoostedProducts()[3];
        }
        else if (animalType == AnimalType.Pig)
        {
            // productPrice = productionSettings.moneyPerProduct;
            productPrice = productionBoosts.GetProductPrices()[4];
            boostedAmount = productionBoosts.GetBoostedProducts()[4];
        }

        int totalProducts = (int)(productPrice * boostedAmount);
        int totalMoneyEarned = totalProducts * animalCount;

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(totalMoneyEarned, transform.position);
            TutorialManager.Instance?.Trigger(TutorialTrigger.CollectedFirstProducts);
        }
        productReady = false;
        isProducing = false;
        productionProgress = 0f;

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
    }

    public bool CanRecruit(int amount)
    {
        // Debug.Log("Here is the amoun of animals: " + animalCount + " and max animals: " + maxAnimalCount + " and amount to recruit: " + amount);

        return animalCount >= amount;
    }

    public void RecruitAnimals(int amount)
    {
        if (CanRecruit(amount))
        {
            animalCount -= amount;
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
        animalCount = Mathf.Clamp(count, 0, maxAnimalCount);
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
        if (backgroundClip != null && audioSource != null)
        {
            StopBackgroundNoise(); // stop any existing loop
            backgroundCoroutine = StartCoroutine(PlayBackgroundClipRandomly(backgroundClip));
        }
    }

    private IEnumerator PlayBackgroundClipRandomly(AudioClip clip)
    {
        while (true)
        {
            float targetVolume = Random.Range(minVolume, maxVolume);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.clip = clip;
            audioSource.volume = 0f;
            audioSource.Play();

            // Fade in
            float fadeInTime = 0.5f;
            for (float t = 0; t < fadeInTime; t += Time.deltaTime)
            {
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
                audioSource.volume = Mathf.Lerp(targetVolume, 0f, t / fadeOutTime);
                yield return null;
            }
            audioSource.Stop();

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
        if (TutorialManager.Instance == null || !isProducing || animalCount <= 0) return;
        isProducing = false;
        productReady = true;
        productionProgress = 1f;
        OnAnimalCountChanged?.Invoke();
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



}