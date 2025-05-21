using UnityEngine;
using System;

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
    [SerializeField] public bool isGrowing = true;
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

    // public static StructureManager Instance;
    // public List<CropStructure> placedCrops = new List<CropStructure>();
    private float lastCheckedHour;
    private float productionMultiplier = 1f;

    // public bool IsGrowing => isGrowing;
    public bool CropReady => cropReady;
    public float GrowthProgress => growthProgress;
    public CropProductionSettings ProductionSettings => productionSettings;
    public float ProductionMultiplier => productionMultiplier;
    public CropType CurrentCropType => currentCropType;

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


    private GameObject currentCropInstance;
    // private GameObject currentCropInstance;


    //totalCrop amounts
    public int sunflowerTotal = 0;
    public int wheatTotal = 0;
    public int carrotTotal = 0;
    

    protected override void Start()
    {
        // Add this instance to the global list of placed crops
        // CropRegistry.Instance.RegisterCrop(this);



        base.Start();
        // isGrowing = false;
        // cropReady = false;
        // growthProgress = 0f;


        if (productionSettings == null)
        {
            productionSettings = new CropProductionSettings();
        }

        // nightManager = FindObjectOfType<NightManager>();
        // if (nightManager == null)
        // {
        //     Debug.LogError("NightManager not found in the scene! CropStructure requires NightManager to function.");
        // }

        // lastCheckedHour = nightManager != null ? nightManager.Hours + (nightManager.Minutes / 60f) : 0f;

        if (structureData != null && structureData.type != StructureType.CropPlot)
        {
            Debug.LogWarning($"{gameObject.name} has CropStructure script but StructureData.type is {structureData.type}, expected CropPlot.");
        }

        // UpdateVisuals(0);
        // UpdateSynergies();
    }

    // private void Update()
    // {
    //     if (nightManager == null || !isGrowing || cropReady) return;

    //     float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
    //     float hourDelta;
    //     if (currentHour < lastCheckedHour)
    //     {
    //         hourDelta = (24f - lastCheckedHour) + currentHour;
    //     }
    //     else
    //     {
    //         hourDelta = currentHour - lastCheckedHour;
    //     }

    //     growthProgress += hourDelta;
    //     lastCheckedHour = currentHour;

    //     if (growthProgress >= productionSettings.growthTime)
    //     {
    //         cropReady = true;
    //         isGrowing = false;
    //         growthProgress = productionSettings.growthTime;
    //         Debug.Log($"{GetStructureName()} has finished growing!");
    //     }

    //     UpdateVisuals();
    // }

    public void Plant(CropType cropType)
    {
        // if (!isGrowing && !cropReady && cropType != CropType.None)
        Debug.Log($"I am in the plant function growing : {isGrowing} and crop ready : {cropReady}");
        if (!isGrowing && !cropReady)
        {
            currentCropType = cropType;
            Debug.Log($"{GetStructureName()} is planting {currentCropType}...");

            isGrowing = true;
            Debug.Log($"Setting isGrowing=true on instance: {this.GetInstanceID()}--------------------------------------------------------------------------------------------------------------------------------");
            Debug.Log($"Planting now growing is {isGrowing}");


            SpawnCropVisual(cropType);
            // UpdateVisuals(0);
        }
    }

    public void Harvest()
    {
        if (cropReady && isGrowing)
        {
            int totalCrops = Mathf.RoundToInt(10);
            Debug.Log($"{GetStructureName()} is harvesting {totalCrops} {currentCropType}...");


            // Debug.Log($"Earned {totalMoneyEarned} {MoneyManager.Instance.GetCurrencyName()} from harvesting {totalCrops} {currentCropType}!");

            // Add to inventory
            // InventoryManager.Instance.AddItem(currentCropType.ToString(), totalCrops);

            if (currentCropType == CropType.Sunflower)
            {
                sunflowerTotal += totalCrops;
            }
            else if (currentCropType == CropType.Wheat)
            {
                wheatTotal += totalCrops;
            }
            else if (currentCropType == CropType.Carrots)
            {
                carrotTotal += totalCrops;
            }

            currentCropType = CropType.None;
            cropReady = false;
            isGrowing = false;

            DestroyCrop();
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
        // cropReady = false;
        // isGrowing = false;
        // growthProgress = 0f;
        // if (nightManager != null)
        // {
        //     lastCheckedHour = nightManager.Hours + (nightManager.Minutes / 60f);
        // }
        // UpdateSynergies();
    }

    // public void UpdateSynergies()
    // {
    //     SiloStructure[] silos = FindObjectsOfType<SiloStructure>();
    //     float minDistance = float.MaxValue;
    //     foreach (SiloStructure silo in silos)
    //     {
    //         float distance = Vector3.Distance(transform.position, silo.transform.position);
    //         if (distance < minDistance)
    //         {
    //             minDistance = distance;
    //         }
    //     }

    //     float maxDistance = 10f;
    //     productionMultiplier = minDistance <= maxDistance ? 1.5f : 1f;
    //     Debug.Log($"{GetStructureName()} synergy updated: minDistance to silo={minDistance}, productionMultiplier={productionMultiplier}");
    // }

    // public void SpawnCropVisual(CropType cropType)
    // {
    //     // Destroy previous instance if any
    //     if (currentCropInstance != null)
    //     {
    //         Destroy(currentCropInstance);
    //     }

    //     GameObject prefabToSpawn = null;

    //     switch (cropType)
    //     {
    //         case CropType.Sunflower:
    //             prefabToSpawn = sunflowerPrefab;
    //             break;
    //         case CropType.Wheat:
    //             prefabToSpawn = wheatPrefab;
    //             break;
    //         case CropType.Carrots:
    //             prefabToSpawn = carrotsPrefab;
    //             break;
    //     }

    //     if (prefabToSpawn != null)
    //     {
    //         currentCropInstance = Instantiate(prefabToSpawn, transform);
    //         currentCropInstance.transform.localPosition = Vector3.zero;
    //         Debug.Log("Crop was planted");
    //     }
    // }

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

    // public void UpdateVisuals(int growth)
    // {
    //     if (stage1Visual != null) stage1Visual.SetActive(false);
    //     if (stage2Visual != null) stage2Visual.SetActive(false);
    //     if (stage3Visual != null) stage3Visual.SetActive(false);

    //     if (!isGrowing && !cropReady)
    //     {
    //         // No crop planted, no visuals
    //         return;
    //     }

    //     // float growthPercentage = growthProgress / productionSettings.growthTime;

    //     //if growth is 0 then it is night and go to second growth stage
    //     //if growth is 1 then it is day and go to third growth stage and allow harvest
    //     if (growth == 0)
    //     {
    //         if (stage1Visual != null) stage1Visual.SetActive(true);
    //         Debug.Log("now stage 0 of growth");
    //     }
    //     else if (growth == 1)
    //     {
    //         if (stage2Visual != null) stage2Visual.SetActive(true);
    //         Debug.Log("now stage 1 of growth");
    //     }
    //     else if (growth == 2)
    //     {
    //         if (stage3Visual != null) stage3Visual.SetActive(true);
    //         Debug.Log("now stage 2 of growth can harvest");
    //         cropReady = true;
    //     }
    // } 


    public void UpdateVisuals(int growth)
    {
        Debug.Log("UpdateVisuals() was called.", this);
    Debug.Log(Environment.StackTrace);
        Debug.Log($"I am in the update visuals function growing : {isGrowing} and crop ready : {cropReady}");
        Debug.Log($"UpdateVisuals called on instance: {this.GetInstanceID()} with isGrowing={isGrowing}-------------------------------------------------------------------------------------------------------------------");


        if (!isGrowing && !cropReady)
        {
            // No crop planted, no visuals
            if (currentCropInstance != null)
            {
                Debug.Log("I have deleted the old crop that had no hope");
                Destroy(currentCropInstance);
            }
            Debug.Log("I have deleted the old crop that had no hope --------------");
            return;
        }

        CropVisualChanger(currentCropType, growth);

        if (growth == 2)
        {
            cropReady = true;
        }
    }


    public void SpawnCropVisual(CropType cropType)
    {
        if (currentCropInstance != null)
        {
            Destroy(currentCropInstance);
        }

        GameObject prefabToSpawn = GetPrefabForStage(cropType, 0);

        if (prefabToSpawn != null)
        {
            currentCropInstance = Instantiate(prefabToSpawn, transform);
            currentCropInstance.transform.localPosition = Vector3.zero;
            Debug.Log($"Crop visual spawned for {cropType} stage");
        }
    }  

    public void CropVisualChanger(CropType cropType, int growth)
    {
        if (currentCropInstance != null)
        {
            Destroy(currentCropInstance);
            Debug.Log("I have deleted the old crop-------------------------");
        }
            Debug.Log("I have deleted the old crop+++++++++++++++++++++++++");

        GameObject prefabToSpawn = GetPrefabForStage(cropType, growth);

        if (prefabToSpawn != null)
        {
        
            Debug.Log("deleted old crop going to spawn new crop size--------------------------------");
            currentCropInstance = Instantiate(prefabToSpawn, transform);
            currentCropInstance.transform.localPosition = Vector3.zero;
            Debug.Log($"Crop visual spawned for {cropType} stage");
        }
    }  
}