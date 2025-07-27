using UnityEngine;

public class CropStructure : Structure
{
    public enum CropType
    {
        None,
        Sunflower,
        Wheat,
        Carrots
    }

    [Header("Crop Settings")]
    [SerializeField] private CropType currentCropType = CropType.None;
    [SerializeField] private bool isGrowing;
    [SerializeField] private bool cropReady;
    [SerializeField] private float growthProgress;
    // [SerializeField] private CropProductionSettings productionSettings;
    [SerializeField] private NightManager nightManager; // Optional, for Inspector

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

    // [System.Serializable]
    // public class CropProductionSettings
    // {
    //     public float growthTime = 24f; // Hours to fully grow
    //     public int baseProductAmount = 10;
    //     public int moneyPerProduct = 5;
    // }

    // Total crop amounts
    public int sunflowerTotal = 0;
    public int wheatTotal = 0;
    public int carrotTotal = 0;

    private GameObject currentCropInstance;
    private float productionMultiplier = 1f;
    private float lastCheckedHour;

    // Synergies
    [Header("Mechanic variations")]
    [Header("Base synergies (increase crop closer to silo)")]
    [SerializeField] private float cropHarvestMultiplier = 1.5f;
    [SerializeField] private float cropHarvestMultiplierIncrease = 1.5f;
    [SerializeField] private float multiplierRange = 10f;
    [SerializeField] private float baseCropHarvestAmount = 10f;

    // Event triggered when a crop is harvested
    public delegate void CropHarvestedHandler(CropType cropType, int amount);
    public event CropHarvestedHandler OnCropHarvested;

    // Public properties
    public bool IsGrowing => isGrowing;
    public bool CropReady => cropReady;
    public float GrowthProgress => growthProgress;
    // public CropProductionSettings ProductionSettings => productionSettings;
    public float ProductionMultiplier => productionMultiplier;
    public CropType CurrentCropType => currentCropType;

    protected override void Start()
    {
        base.Start();
        UpdateSiloSynergy();

        // if (productionSettings == null)
        // {
        //     productionSettings = new CropProductionSettings();
        // }

        if (structureData != null && structureData.type != StructureType.CropPlot)
        {
            Debug.LogWarning($"{GetStructureName()} has incorrect structureData type: {structureData.type}. Expected CropPlot.");
        }

        if (nightManager == null)
        {
            nightManager = NightManager.Instance;
            if (nightManager == null)
            {
                nightManager = FindFirstObjectByType<NightManager>();
                if (nightManager == null)
                {
                    Debug.LogError($"{GetStructureName()} cannot find NightManager!");
                }
            }
        }

        lastCheckedHour = nightManager?.Hours ?? 7;
    }

    private void Update()
    {
        if (nightManager == null || !isGrowing || cropReady) return;

        float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
        float hourDelta = currentHour >= lastCheckedHour ? currentHour - lastCheckedHour : (24f - lastCheckedHour) + currentHour;
        growthProgress += hourDelta;
        lastCheckedHour = currentHour;

        // int growthStage = Mathf.Min(Mathf.FloorToInt(growthProgress / (productionSettings.growthTime / 3)), 2);
        // UpdateCropVisual(currentCropType, growthStage);

        // if (growthProgress >= productionSettings.growthTime)
        // {
        //     cropReady = true;
        //     isGrowing = false;
        //     growthProgress = productionSettings.growthTime;
        // }
    }

    public void Plant(CropType cropType)
    {
        if (cropType == CropType.None)
        {
            Debug.LogWarning($"{GetStructureName()} cannot plant CropType.None.");
            return;
        }

        if (!isGrowing && !cropReady)
        {
            currentCropType = cropType;
            isGrowing = true;
            growthProgress = 0f;
            lastCheckedHour = nightManager?.Hours ?? 7;
            UpdateCropVisual(cropType, 0);
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot plant {cropType}: Crop is already growing or ready.");
        }
    }

    public string Harvest()
    {
        Debug.Log($"Tutorial: Attempting to harvest {currentCropType} from {GetStructureName()}, CropReady: {cropReady}, Current Step: {TutorialManager.Instance?.GetCurrentStepId() ?? "None"}");
        if (cropReady)
        {
            int totalCrops = Mathf.RoundToInt(baseCropHarvestAmount * cropHarvestMultiplier);

            TutorialConditionTracker conditionTracker = FindFirstObjectByType<TutorialConditionTracker>();
            bool isTutorialHarvest = TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive() && (conditionTracker != null && !conditionTracker.HasCompletedTutorialStep("harvest_first_crops"));
            Debug.Log($"Tutorial: Harvest check - isTutorialHarvest: {isTutorialHarvest}, CanHarvest: {InventoryManager.Instance.canHarvest(totalCrops)}, TotalCrops: {totalCrops}");

            if (!isTutorialHarvest && InventoryManager.Instance.canHarvest(totalCrops) == false)
            {
                Debug.LogWarning($"{GetStructureName()} cannot harvest: Not enough space in inventory.");
                return "space";
            }

            string cropName = currentCropType.ToString();
            InventoryManager.Instance.AddItem(cropName, totalCrops);

            switch (currentCropType)
            {
                case CropType.Sunflower:
                    sunflowerTotal += totalCrops;
                    break;
                case CropType.Wheat:
                    wheatTotal += totalCrops;
                    break;
                case CropType.Carrots:
                    carrotTotal += totalCrops;
                    break;
                default:
                    break;
            }

            Debug.Log($"Tutorial: Triggering OnCropHarvested for {totalCrops} {currentCropType}");
            OnCropHarvested?.Invoke(currentCropType, totalCrops);
            Debug.Log($"Tutorial: Harvested {totalCrops} {currentCropType} from {GetStructureName()}");

            currentCropType = CropType.None;
            cropReady = false;
            isGrowing = false;
            growthProgress = 0f;

            DestroyCrop();

            return "yes";
        }
        else
        {
            Debug.Log($"Tutorial: Harvest failed - Crop not ready");
            return "ready";
        }
    }

    public void DestroyCrop()
    {
        if (currentCropInstance != null)
        {
            Destroy(currentCropInstance);
            currentCropInstance = null;
        }
        currentCropType = CropType.None;
        cropReady = false;
        isGrowing = false;
        growthProgress = 0f;
    }

    public void UpdateVisuals(int growthStage)
    {
        if (!isGrowing && !cropReady)
        {
            if (currentCropInstance != null)
            {
                Destroy(currentCropInstance);
                currentCropInstance = null;
            }
            return;
        }

        UpdateCropVisual(currentCropType, growthStage);

        if (growthStage == 2)
        {
            cropReady = true;
            isGrowing = false;
        }
    }

    private void UpdateCropVisual(CropType cropType, int growthStage)
    {
        if (currentCropInstance != null)
        {
            Destroy(currentCropInstance);
        }

        GameObject prefabToSpawn = GetPrefabForStage(cropType, growthStage);
        if (prefabToSpawn != null)
        {
            currentCropInstance = Instantiate(prefabToSpawn, transform);
            currentCropInstance.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogWarning($"No prefab found for {cropType} stage {growthStage} on {GetStructureName()}");
        }
    }

    public void UpdateSiloSynergy()
    {
        SiloStructure[] silos = FindObjectsByType<SiloStructure>(FindObjectsSortMode.None);
        float minGridDistance = float.MaxValue;

        GridController gridController = FindFirstObjectByType<GridController>();

        if (gridController == null)
        {
            Debug.LogWarning("No GridController found for CropStructure synergy check.");
            cropHarvestMultiplier = 1f;
            return;
        }

        Vector2Int cropCell = gridController.WorldToGridCoords(transform.position);

        foreach (SiloStructure silo in silos)
        {
            Vector2Int siloCell = gridController.WorldToGridCoords(silo.transform.position);
            float gridDistance = Vector2Int.Distance(cropCell, siloCell);
            if (gridDistance < minGridDistance)
            {
                minGridDistance = gridDistance;
            }
        }

        if (minGridDistance <= multiplierRange)
            cropHarvestMultiplier = cropHarvestMultiplierIncrease;
        else
            cropHarvestMultiplier = 1f;
    }

    public static float[] GetAllCropHarvestMultipliers(string[] cropTypes)
    {
        float[] multipliers = new float[cropTypes.Length];
        CropStructure[] allCrops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);

        for (int i = 0; i < cropTypes.Length; i++)
        {
            foreach (var crop in allCrops)
            {
                if (crop.CurrentCropType.ToString().Equals(cropTypes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    multipliers[i] = crop.cropHarvestMultiplier;
                    break;
                }
            }
        }
        return multipliers;
    }

    public void OnPlaced()
    {
        UpdateSiloSynergy();
    }

    public static void UpdateAllCropSynergies()
    {
        foreach (var crop in FindObjectsByType<CropStructure>(FindObjectsSortMode.None))
        {
            crop.UpdateSiloSynergy();
        }
    }

    private GameObject GetPrefabForStage(CropType cropType, int stage)
    {
        switch (cropType)
        {
            case CropType.Sunflower:
                if (stage == 0) return sunflowerPrefab1;
                else if (stage == 1) return sunflowerPrefab2;
                else if (stage == 2) return sunflowerPrefab3;
                break;
            case CropType.Wheat:
                if (stage == 0) return wheatPrefab1;
                else if (stage == 1) return wheatPrefab2;
                else if (stage == 2) return wheatPrefab3;
                break;
            case CropType.Carrots:
                if (stage == 0) return carrotsPrefab1;
                else if (stage == 1) return carrotsPrefab2;
                else if (stage == 2) return carrotsPrefab3;
                break;
        }
        return null;
    }

    public void InstantGrowForTutorial()
    {
        if (isGrowing && !cropReady)
        {
            // growthProgress = productionSettings.growthTime;
            cropReady = true;
            isGrowing = false;
            UpdateCropVisual(currentCropType, 2);
            Debug.Log($"TUTORIAL: Instantly completed growth for {currentCropType}");
        }
    }

    public char GetCurrCrop()
    {
        if (currentCropType == CropType.Carrots)
        {
            return 'C';
        }
        else if (currentCropType == CropType.Sunflower)
        {
            return 'S';
        }
        else if (currentCropType == CropType.Wheat)
        {
            return 'W';
        }
        else
        {
            return 'N';
        }
    }
}