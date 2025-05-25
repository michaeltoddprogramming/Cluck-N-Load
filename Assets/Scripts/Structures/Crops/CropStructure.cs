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
    [SerializeField] private CropProductionSettings productionSettings;
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

    [System.Serializable]
    public class CropProductionSettings
    {
        public float growthTime = 24f; // Hours to fully grow
        public int baseProductAmount = 10;
        public int moneyPerProduct = 5;
    }

    // Total crop amounts
    public int sunflowerTotal = 0;
    public int wheatTotal = 0;
    public int carrotTotal = 0;

    private GameObject currentCropInstance;
    private float productionMultiplier = 1f;
    private float lastCheckedHour;

    // Public properties
    public bool IsGrowing => isGrowing;
    public bool CropReady => cropReady;
    public float GrowthProgress => growthProgress;
    public CropProductionSettings ProductionSettings => productionSettings;
    public float ProductionMultiplier => productionMultiplier;
    public CropType CurrentCropType => currentCropType;


    //synergies
    [Header("Mechanic variations")]
    [Header("Base synergies (increase crop closer to silo)")]
    [SerializeField] private float cropHarvestMultiplier = 1.5f;
    [SerializeField] private float cropHarvestMultiplierIncrease = 1.5f;
    [SerializeField] private float multiplierRange = 10f;
    [SerializeField] private float baseCropHarvestAmount = 10f;


    protected override void Start()
    {
        base.Start();
        UpdateSiloSynergy();

        if (productionSettings == null)
        {
            productionSettings = new CropProductionSettings();
        }

        if (structureData != null && structureData.type != StructureType.CropPlot)
        {
            Debug.LogWarning($"{gameObject.name} has CropStructure script but StructureData.type is {structureData.type}, expected CropPlot.");
        }

        // Prefer singleton access
        if (nightManager == null)
        {
            nightManager = NightManager.Instance;
            if (nightManager == null)
            {
                nightManager = FindObjectOfType<NightManager>();
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

        int growthStage = Mathf.Min(Mathf.FloorToInt(growthProgress / (productionSettings.growthTime / 3)), 2);
        UpdateCropVisual(currentCropType, growthStage);

        if (growthProgress >= productionSettings.growthTime)
        {
            cropReady = true;
            isGrowing = false;
            growthProgress = productionSettings.growthTime;
        }
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
            Debug.Log($"{GetStructureName()} is planting {currentCropType}...");
            UpdateCropVisual(cropType, 0);
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot plant {cropType}: Crop is already growing or ready.");
        }
    }

    public string Harvest()
    {
        if (cropReady)
        {
            int totalCrops = Mathf.RoundToInt(baseCropHarvestAmount * cropHarvestMultiplier);

            //check if silos have space to store the crops
            if (InventoryManager.Instance.canHarvest(totalCrops) == false)
            {
                Debug.LogWarning($"{GetStructureName()} cannot harvest: Not enough space in inventory.");
                return "space";
            }

            // int totalCrops = Mathf.RoundToInt(10);

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

            currentCropType = CropType.None;
            cropReady = false;
            isGrowing = false;
            growthProgress = 0f;

            DestroyCrop();

            return "yes";
        }
        else
        {
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
            Debug.Log($"{GetStructureName()} spawned visual for {cropType} stage {growthStage}.");
        }
        else
        {
            Debug.LogWarning($"No prefab found for {cropType} stage {growthStage} on {GetStructureName()}");
        }
    }

    public void UpdateSiloSynergy()
    {
        SiloStructure[] silos = FindObjectsOfType<SiloStructure>();
        float minGridDistance = float.MaxValue;

        // Get the grid controller (assumes only one in scene)
        GridController gridController = FindObjectOfType<GridController>();

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
            cropHarvestMultiplier = cropHarvestMultiplierIncrease; // or whatever bonus you want
        else
            cropHarvestMultiplier = 1f;
    }

    public static float[] GetAllCropHarvestMultipliers(string[] cropTypes)
    {
        float[] multipliers = new float[cropTypes.Length];
        CropStructure[] allCrops = GameObject.FindObjectsOfType<CropStructure>();

        for (int i = 0; i < cropTypes.Length; i++)
        {
            foreach (var crop in allCrops)
            {
                if (crop.CurrentCropType.ToString().Equals(cropTypes[i], System.StringComparison.OrdinalIgnoreCase))
                {
                    multipliers[i] = crop.cropHarvestMultiplier;
                    break; // Found the crop type, move to next
                }
            }
        }
        return multipliers;
    }

    

    public void OnPlaced()
    {
        // base.OnPlaced();
        UpdateSiloSynergy();
    }

    public static void UpdateAllCropSynergies()
    {
        foreach (var crop in GameObject.FindObjectsOfType<CropStructure>())
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
}