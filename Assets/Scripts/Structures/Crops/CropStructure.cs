using UnityEngine;
using System.Collections;

public class CropStructure : Structure
{
    public enum CropType { None, Sunflower, Wheat, Carrots }

    [Header("Crop Settings")]
    [SerializeField] private CropType currentCropType = CropType.None;
    [SerializeField] private bool isGrowing;
    [SerializeField] private bool cropReady;
    [SerializeField] private NightManager nightManager;

    [Header("Crop Prefabs")]
    [SerializeField] private GameObject sunflowerPrefab1;
    [SerializeField] private GameObject sunflowerPrefab2;
    [SerializeField] private GameObject sunflowerPrefab3;
    [SerializeField] private GameObject wheatPrefab1;
    [SerializeField] private GameObject wheatPrefab2;
    [SerializeField] private GameObject wheatPrefab3;
    [SerializeField] private GameObject carrotsPrefab1;
    [SerializeField] private GameObject carrotsPrefab2;
    [SerializeField] private GameObject carrotsPrefab3;

    public int sunflowerTotal;
    public int wheatTotal;
    public int carrotTotal;

    private GameObject currentCropInstance;
    private float productionMultiplier = 1f;
    private ReadyIndicator readyIndicator;

    [Header("Mechanic variations")]
    [SerializeField] private float cropHarvestMultiplier = 1.5f;
    [SerializeField] private float baseCropHarvestAmount = 10f;

    // Add these missing fields
    [SerializeField] private float multiplierRange = 10f;
    [SerializeField] private float cropHarvestMultiplierIncrease = 1.5f;

    // Add these public properties to fix the accessibility issue
    public float BaseCropHarvestAmount => baseCropHarvestAmount;
    public float CropHarvestMultiplier => cropHarvestMultiplier;

    public delegate void CropHarvestedHandler(CropType cropType, int amount);
    public event CropHarvestedHandler OnCropHarvested;

    public bool IsGrowing => isGrowing;
    public bool CropReady => cropReady;
    public float ProductionMultiplier => productionMultiplier;
    public CropType CurrentCropType => currentCropType;
    StructureData data;

    protected override void Start()
    {
        base.Start();
        data = GetData();
        multiplierRange = data.cropSiloSynergyRange;
        if (currentCropType == CropType.Carrots)
        {
            baseCropHarvestAmount = data.carrotsBaseHarvestAmount;
        }
        else if (currentCropType == CropType.Sunflower)
        {
            baseCropHarvestAmount = data.sunflowerBaseHarvestAmount;
        }
        else
        {
            baseCropHarvestAmount = data.wheatBaseHarvestAmount;
        }

        cropHarvestMultiplierIncrease = data.cropSynergyMultiplier;

        UpdateSiloSynergy();
        nightManager = nightManager ?? NightManager.Instance ?? FindFirstObjectByType<NightManager>();

        // Initialize ready indicator
        readyIndicator = GetComponent<ReadyIndicator>();
        if (readyIndicator == null)
            readyIndicator = gameObject.AddComponent<ReadyIndicator>();
    }

    public void Plant(CropType cropType)
    {
        if (cropType == CropType.None || cropReady) return;
        currentCropType = cropType;
        updateCropAmounts();
        isGrowing = true;

        // Hide indicator during growing
        if (readyIndicator != null)
            readyIndicator.HideIndicator();

        UpdateCropVisual(cropType, 0);
        
        // Check for instant growth BEFORE triggering (so step isn't completed yet)
        bool shouldInstantGrow = false;
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            // Check if we haven't completed the plant step yet
            if (!TutorialManager.Instance.GetCompletedStepIds().Contains("plant_first_crop"))
            {
                shouldInstantGrow = true;
            }
        }
        
        // Trigger tutorial event
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.Trigger(TutorialTrigger.PlantedCrop);
        }
        
        // Apply instant growth if needed
        if (shouldInstantGrow)
        {
            // Instantly grow the crop for tutorial
            cropReady = true;
            isGrowing = false;
            UpdateCropVisual(cropType, 2); // Show fully grown crop
            
            // Show ready indicator
            if (readyIndicator != null)
                readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Harvest);
        }
    }

    public void updateCropAmounts()
    {
        if (currentCropType == CropType.Carrots)
        {
            baseCropHarvestAmount = data.carrotsBaseHarvestAmount;
        }
        else if (currentCropType == CropType.Sunflower)
        {
            baseCropHarvestAmount = data.sunflowerBaseHarvestAmount;
        }
        else
        {
            baseCropHarvestAmount = data.wheatBaseHarvestAmount;
        }
    }

    public string Harvest()
    {
        if (!cropReady) return "ready";

        int totalCrops = Mathf.RoundToInt(baseCropHarvestAmount * cropHarvestMultiplier);
        string cropName = currentCropType.ToString();

        // Crops give inventory items only - money comes from selling products
        InventoryManager.Instance.AddItem(cropName, totalCrops);

        switch (currentCropType)
        {
            case CropType.Sunflower: sunflowerTotal += totalCrops; break;
            case CropType.Wheat: wheatTotal += totalCrops; break;
            case CropType.Carrots: carrotTotal += totalCrops; break;
        }

        if (TutorialManager.Instance != null)
        {
            // For tutorial instant mechanics, trigger immediately. Otherwise add small delay.
            if (TutorialManager.Instance.IsTutorialActive() &&
                !TutorialManager.Instance.GetCompletedStepIds().Contains("plant_first_crop"))
            {
                // Instant tutorial mechanics - trigger immediately
                TutorialManager.Instance.Trigger(TutorialTrigger.HarvestedCrop);
            }
            else
            {
                // Normal harvest - add small delay to ensure animation completes
                StartCoroutine(DelayedTutorialTrigger(TutorialTrigger.HarvestedCrop, 0.1f));
            }
        }

        OnCropHarvested?.Invoke(currentCropType, totalCrops);
        currentCropType = CropType.None;
        cropReady = false;
        isGrowing = false;

        // Hide indicator after harvest
        if (readyIndicator != null)
            readyIndicator.HideIndicator();

        DestroyCrop();
        return "yes";
    }

    // Add this new method to CropStructure
    private int GetCropMoneyValue(CropType cropType, int amount)
    {
        int baseValue = cropType switch
        {
            CropType.Sunflower => 15,  // $15 per sunflower
            CropType.Wheat => 12,      // $12 per wheat
            CropType.Carrots => 18,    // $18 per carrot
            _ => 10
        };

        // Apply farm efficiency bonus if nearby farm house
        float efficiencyMultiplier = GetNearbyFarmEfficiency();
        return Mathf.RoundToInt(amount * baseValue * efficiencyMultiplier);
    }

    private float GetNearbyFarmEfficiency()
    {
        FarmHouseStructure[] farmHouses = FindObjectsByType<FarmHouseStructure>(FindObjectsSortMode.None);
        float maxEfficiency = 1f;

        foreach (var farmHouse in farmHouses)
        {
            float distance = Vector3.Distance(transform.position, farmHouse.transform.position);
            if (distance <= 15f) // Within range of farm house
            {
                maxEfficiency = Mathf.Max(maxEfficiency, farmHouse.GetFarmEfficiency());
            }
        }

        return maxEfficiency;
    }

    public void DestroyCrop()
    {
        if (currentCropInstance != null) Destroy(currentCropInstance);
        currentCropInstance = null;
        currentCropType = CropType.None;
        cropReady = false;
        isGrowing = false;
    }

    public void UpdateVisuals(int growthStage)
    {
        if (!isGrowing && !cropReady)
        {
            if (currentCropInstance != null) Destroy(currentCropInstance);
            currentCropInstance = null;
            return;
        }
        UpdateCropVisual(currentCropType, growthStage);
        if (growthStage == 2)
        {
            cropReady = true;
            isGrowing = false;

            // Show ready indicator when crop is ready for harvest
            if (readyIndicator != null)
                readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Harvest);
        }
    }

    private void UpdateCropVisual(CropType cropType, int growthStage)
    {
        if (currentCropInstance != null) Destroy(currentCropInstance);
        GameObject prefabToSpawn = GetPrefabForStage(cropType, growthStage);
        if (prefabToSpawn != null)
        {
            currentCropInstance = Instantiate(prefabToSpawn, transform);
            currentCropInstance.transform.localPosition = Vector3.zero;
        }
    }

    public void UpdateSiloSynergy()
    {
        SiloStructure[] silos = FindObjectsByType<SiloStructure>(FindObjectsSortMode.None);
        float minGridDistance = float.MaxValue;
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            cropHarvestMultiplier = 1f;
            return;
        }
        Vector2Int cropCell = gridController.WorldToGridCoords(transform.position);
        foreach (SiloStructure silo in silos)
        {
            Vector2Int siloCell = gridController.WorldToGridCoords(silo.transform.position);
            float gridDistance = Vector2Int.Distance(cropCell, siloCell);
            minGridDistance = Mathf.Min(minGridDistance, gridDistance);
        }
        
        float oldMultiplier = cropHarvestMultiplier;
        cropHarvestMultiplier = minGridDistance <= multiplierRange ? cropHarvestMultiplierIncrease : 1f;
        
        // REMOVED: Synergy notifications are now shown in harvest notifications instead
        // This avoids temporary/spam notifications
    }

    public static float[] GetAllCropHarvestMultipliers(string[] cropTypes)
    {
        float[] multipliers = new float[cropTypes.Length];
        CropStructure[] allCrops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        for (int i = 0; i < cropTypes.Length; i++)
            foreach (var crop in allCrops)
                if (crop.CurrentCropType.ToString().Equals(cropTypes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    multipliers[i] = crop.cropHarvestMultiplier;
                    break;
                }
        return multipliers;
    }

    public void OnPlaced() => UpdateSiloSynergy();

    public static void UpdateAllCropSynergies()
    {
        foreach (var crop in FindObjectsByType<CropStructure>(FindObjectsSortMode.None)) crop.UpdateSiloSynergy();
    }

    private GameObject GetPrefabForStage(CropType cropType, int stage)
    {
        return cropType switch
        {
            CropType.Sunflower => stage switch { 0 => sunflowerPrefab1, 1 => sunflowerPrefab2, 2 => sunflowerPrefab3, _ => null },
            CropType.Wheat => stage switch { 0 => wheatPrefab1, 1 => wheatPrefab2, 2 => wheatPrefab3, _ => null },
            CropType.Carrots => stage switch { 0 => carrotsPrefab1, 1 => carrotsPrefab2, 2 => carrotsPrefab3, _ => null },
            _ => null
        };
    }

    public char GetCurrCrop()
    {
        return currentCropType switch
        {
            CropType.Carrots => 'C',
            CropType.Sunflower => 'S',
            CropType.Wheat => 'W',
            _ => 'N'
        };
    }

    public void ForceHarvestReadyForTutorial()
    {
        if (TutorialManager.Instance == null) return;
        isGrowing = false;
        cropReady = true;
        if (currentCropType != CropType.None) UpdateCropVisual(currentCropType, 2);

        // Show ready indicator when crop is ready for harvest
        if (readyIndicator != null)
            readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Harvest);
    }

    public void SetCropState(string cropType, bool isGrowing, bool cropReady)
    {
        if (System.Enum.TryParse(cropType, out CropType parsedType))
            currentCropType = parsedType;
        else
            currentCropType = CropType.None;

        this.isGrowing = isGrowing;
        this.cropReady = cropReady;
        UpdateCropVisual(currentCropType, cropReady ? 2 : isGrowing ? 1 : 0);
    }

    // Add this method to CropStructure class
    public void CheatInstantGrowth()
    {
        if (currentCropType != CropType.None && !cropReady)
        {
            // Set proper crop growth model states
            cropReady = true;
            isGrowing = false;
            
            // Update visual to final growth stage (fully grown)
            UpdateCropVisual(currentCropType, 2);
            
            // Show ready indicator for harvest
            if (readyIndicator != null)
                readyIndicator.ShowIndicator(ReadyIndicator.IndicatorType.Harvest);
                
        }
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

    private float plantedAtHour = -1f; // When crop was planted
private float cropGrowthProgress = 0f; // 0 to 1
private bool wasGrowing = false;

public void TrackGrowth(NightManager nightManager)
{
    if (!isGrowing) 
    {
        cropGrowthProgress = cropReady ? 1f : 0f;
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
        cropReady = true;
        isGrowing = false;
        cropGrowthProgress = 1f;
    }
}

public float GetGrowthProgress() => cropGrowthProgress; // 0..1


    
}