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

    [Header("Growth Stages Visuals")]
    [SerializeField] private GameObject stage1Visual; // Seedling stage
    [SerializeField] private GameObject stage2Visual; // Growing stage
    [SerializeField] private GameObject stage3Visual; // Mature stage

    [System.Serializable]
    public class CropProductionSettings
    {
        public float growthTime = 24f;
        public int baseProductAmount = 10;
        public int moneyPerProduct = 5;
    }

    private NightManager nightManager;
    private float lastCheckedHour;
    private float productionMultiplier = 1f;

    public bool IsGrowing => isGrowing;
    public bool CropReady => cropReady;
    public float GrowthProgress => growthProgress;
    public CropProductionSettings ProductionSettings => productionSettings;
    public float ProductionMultiplier => productionMultiplier;
    public CropType CurrentCropType => currentCropType;

    protected override void Start()
    {
        base.Start();
        isGrowing = false;
        cropReady = false;
        growthProgress = 0f;
        if (productionSettings == null)
        {
            productionSettings = new CropProductionSettings();
        }

        nightManager = FindObjectOfType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError("NightManager not found in the scene! CropStructure requires NightManager to function.");
        }

        lastCheckedHour = nightManager != null ? nightManager.Hours + (nightManager.Minutes / 60f) : 0f;

        if (structureData != null && structureData.type != StructureType.CropPlot)
        {
            Debug.LogWarning($"{gameObject.name} has CropStructure script but StructureData.type is {structureData.type}, expected CropPlot.");
        }

        UpdateVisuals();
        UpdateSynergies();
    }

    private void Update()
    {
        if (nightManager == null || !isGrowing || cropReady) return;

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

        growthProgress += hourDelta;
        lastCheckedHour = currentHour;

        if (growthProgress >= productionSettings.growthTime)
        {
            cropReady = true;
            isGrowing = false;
            growthProgress = productionSettings.growthTime;
            Debug.Log($"{GetStructureName()} has finished growing!");
        }

        UpdateVisuals();
    }

    public void Plant(CropType cropType)
    {
        if (!isGrowing && !cropReady && cropType != CropType.None)
        {
            currentCropType = cropType;
            Debug.Log($"{GetStructureName()} is planting {currentCropType}...");
            isGrowing = true;
            growthProgress = 0f;
            if (nightManager != null)
            {
                lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
            }
            UpdateSynergies();
            UpdateVisuals();
        }
    }

    public void Harvest()
    {
        if (cropReady)
        {
            int totalCrops = Mathf.RoundToInt(productionSettings.baseProductAmount * productionMultiplier);
            Debug.Log($"{GetStructureName()} is harvesting {totalCrops} {currentCropType}...");

            int totalMoneyEarned = totalCrops * productionSettings.moneyPerProduct;
            MoneyManager.Instance.AddMoney(totalMoneyEarned);
            Debug.Log($"Earned {totalMoneyEarned} {MoneyManager.Instance.GetCurrencyName()} from harvesting {totalCrops} {currentCropType}!");

            // Add to inventory
            InventoryManager.Instance.AddItem(currentCropType.ToString(), totalCrops);

            currentCropType = CropType.None;
            cropReady = false;
            isGrowing = false;
            growthProgress = 0f;
            if (nightManager != null)
            {
                lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
            }
            UpdateSynergies();
            UpdateVisuals();
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
        productionMultiplier = minDistance <= maxDistance ? 1.5f : 1f;
        Debug.Log($"{GetStructureName()} synergy updated: minDistance to silo={minDistance}, productionMultiplier={productionMultiplier}");
    }

    private void UpdateVisuals()
    {
        if (stage1Visual != null) stage1Visual.SetActive(false);
        if (stage2Visual != null) stage2Visual.SetActive(false);
        if (stage3Visual != null) stage3Visual.SetActive(false);

        if (!isGrowing && !cropReady)
        {
            // No crop planted, no visuals
            return;
        }

        float growthPercentage = growthProgress / productionSettings.growthTime;
        if (growthPercentage < 0.33f)
        {
            if (stage1Visual != null) stage1Visual.SetActive(true);
        }
        else if (growthPercentage < 0.66f)
        {
            if (stage2Visual != null) stage2Visual.SetActive(true);
        }
        else
        {
            if (stage3Visual != null) stage3Visual.SetActive(true);
        }
    }


    
}