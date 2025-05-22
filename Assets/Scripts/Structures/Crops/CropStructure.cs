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
        public float growthTime = 24f;
        public int baseProductAmount = 10;
        public int moneyPerProduct = 5;
    }

    // Total crop amounts
    public int sunflowerTotal = 0;
    public int wheatTotal = 0;
    public int carrotTotal = 0;

    private GameObject currentCropInstance;
    private float productionMultiplier = 1f;

    // Public properties
    public bool IsGrowing => isGrowing;
    public bool CropReady => cropReady;
    public float GrowthProgress => growthProgress;
    public CropProductionSettings ProductionSettings => productionSettings;
    public float ProductionMultiplier => productionMultiplier;
    public CropType CurrentCropType => currentCropType;

    protected override void Start()
    {
        base.Start();

        if (productionSettings == null)
        {
            productionSettings = new CropProductionSettings();
        }

        if (structureData != null && structureData.type != StructureType.CropPlot)
        {
            Debug.LogWarning($"{gameObject.name} has CropStructure script but StructureData.type is {structureData.type}, expected CropPlot.");
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
            Debug.Log($"{GetStructureName()} is planting {currentCropType}...");
            UpdateCropVisual(cropType, 0);
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot plant {cropType}: Crop is already growing or ready.");
        }
    }

    public void Harvest()
    {
        if (cropReady)
        {
            int totalCrops = Mathf.RoundToInt(productionSettings.baseProductAmount * productionMultiplier);
            Debug.Log($"Harvesting crop type: {currentCropType} on {GetStructureName()} - {totalCrops} units");

            // Add to inventory
            string cropName = currentCropType.ToString();
            InventoryManager.Instance.AddItem(cropName, totalCrops);

            // Update total counts
            switch (currentCropType)
            {
                case CropType.Sunflower:
                    sunflowerTotal += totalCrops;
                    Debug.Log($"Added {totalCrops} Sunflowers to total: {sunflowerTotal}");
                    break;
                case CropType.Wheat:
                    wheatTotal += totalCrops;
                    Debug.Log($"Added {totalCrops} Wheat to total: {wheatTotal}");
                    break;
                case CropType.Carrots:
                    carrotTotal += totalCrops;
                    Debug.Log($"Added {totalCrops} Carrots to total: {carrotTotal}");
                    break;
                default:
                    Debug.LogError($"Unexpected crop type in Harvest: {currentCropType}");
                    break;
            }

            currentCropType = CropType.None;
            cropReady = false;
            isGrowing = false;
            growthProgress = 0f;

            DestroyCrop();
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} cannot harvest: Crop is not ready.");
        }
    }

    public void DestroyCrop()
    {
        if (currentCropInstance != null)
        {
            Destroy(currentCropInstance);
            currentCropInstance = null;
            Debug.Log($"{GetStructureName()} crop destroyed.");
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
                Debug.Log($"{GetStructureName()} cleared visuals (no crop planted).");
            }
            return;
        }

        UpdateCropVisual(currentCropType, growthStage);

        if (growthStage == 2)
        {
            cropReady = true;
            isGrowing = false;
            Debug.Log($"{GetStructureName()} is ready to harvest.");
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