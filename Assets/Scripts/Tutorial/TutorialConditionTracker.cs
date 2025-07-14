using UnityEngine;
using System.Collections;

public class TutorialConditionTracker : MonoBehaviour
{
    private bool hasPlacedFirstStructure = false;
    private bool hasPlantedFirstCrop = false;
    private bool hasHarvestedFirstCrop = false;
    private bool hasBoughtFirstAnimal = false;
    private bool hasCollectedFirstProduct = false;
    private bool hasRecruitedArmy = false;
    private bool hasPlacedFlag = false;
    private bool nightHasStarted = false;
    private bool hasDefeatedFirstWolf = false;
    private bool hasStartedSecondDay = false;

    private int structurePlacementCount = 0;
    private int currentDay = 0;
    private bool isNightTime = false;

    private void Start()
    {
        // Subscribe to relevant game events
        SubscribeToGameEvents();
        
        // Start tracking routine
        StartCoroutine(TrackGameConditions());
    }

    private void SubscribeToGameEvents()
    {
        // Subscribe to night manager events
        if (NightManager.Instance != null)
        {
            // Track day/night changes
            StartCoroutine(TrackDayNightChanges());
        }

        // Subscribe to money manager events
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
        }
    }

    private IEnumerator TrackGameConditions()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Check every second

            TrackStructurePlacements();
            TrackCropStatus();
            TrackAnimalStatus();
            TrackDefenseStatus();
            TrackNightStatus();
        }
    }

    private IEnumerator TrackDayNightChanges()
    {
        bool wasNight = false;
        int lastDay = 0;

        while (true)
        {
            if (NightManager.Instance != null)
            {
                bool currentIsNight = !NightManager.Instance.IsDay;
                int currentDay = NightManager.Instance.Days;

                // Night started
                if (currentIsNight && !wasNight && !nightHasStarted)
                {
                    nightHasStarted = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.NightStarted);
                }

                // Day changed
                if (currentDay > lastDay)
                {
                    if (currentDay == 1 && !hasStartedSecondDay)
                    {
                        hasStartedSecondDay = true;
                        TutorialManager.Instance?.OnConditionMet(TutorialCondition.SecondDayStarted);
                    }
                }

                wasNight = currentIsNight;
                lastDay = currentDay;
                isNightTime = currentIsNight;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void TrackStructurePlacements()
    {
        // Count all structures in the scene
        Structure[] allStructures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
        
        if (allStructures.Length > structurePlacementCount)
        {
            structurePlacementCount = allStructures.Length;

            if (!hasPlacedFirstStructure)
            {
                hasPlacedFirstStructure = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstStructurePlaced);
            }

            // Check for specific structure types
            foreach (Structure structure in allStructures)
            {
                CheckSpecificStructureType(structure);
            }
        }
    }

    private void CheckSpecificStructureType(Structure structure)
    {
        if (structure.structureData == null) return;

        switch (structure.structureData.type)
        {
            case StructureType.Building:
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FarmHousePlaced);
                break;
            
            case StructureType.Silo:
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.SiloPlaced);
                break;
            
            case StructureType.CropPlot:
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.CropPlotPlaced);
                break;
            
            case StructureType.Animal:
            case StructureType.AnimalPlot:
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.ChickenCoopPlaced);
                break;
            
            case StructureType.Barracks:
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.BarracksPlaced);
                break;
        }
    }

    private void TrackCropStatus()
    {
        CropStructure[] cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        
        foreach (CropStructure crop in cropStructures)
        {
            // Check if any crop has been planted
            if (crop.CurrentCropType != CropStructure.CropType.None && !hasPlantedFirstCrop)
            {
                hasPlantedFirstCrop = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstCropPlanted);
            }

            // Track if inventory has increased (indicating harvest)
            if (hasPlantedFirstCrop && !hasHarvestedFirstCrop)
            {
                CheckInventoryForHarvest();
            }
        }
    }

    private void CheckInventoryForHarvest()
    {
        if (InventoryManager.Instance != null)
        {
            int totalCrops = InventoryManager.Instance.GetItemCount("Sunflower") +
                           InventoryManager.Instance.GetItemCount("Wheat") +
                           InventoryManager.Instance.GetItemCount("Carrots");

            if (totalCrops > 0 && !hasHarvestedFirstCrop)
            {
                hasHarvestedFirstCrop = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstCropHarvested);
            }
        }
    }

    private void TrackAnimalStatus()
    {
        AnimalStructure[] animalStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        
        foreach (AnimalStructure animal in animalStructures)
        {
            // Check if any animals have been bought
            if (animal.AnimalCount > 0 && !hasBoughtFirstAnimal)
            {
                hasBoughtFirstAnimal = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstChickenBought);
            }

            // Check if animals are producing
            if (animal.ProductReady && hasBoughtFirstAnimal)
            {
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.ChickensStartedProducing);
            }
        }
    }

    private void TrackDefenseStatus()
    {
        BarracksStructure[] barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        
        foreach (BarracksStructure barrack in barracks)
        {
            // Check if flag has been placed
            if (barrack.GetFlagPosition != Vector3.zero && !hasPlacedFlag)
            {
                hasPlacedFlag = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FlagPlaced);
            }

            // Check if army has been recruited
            if (barrack.ArmyAnimalCount > 0 && !hasRecruitedArmy)
            {
                hasRecruitedArmy = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.ArmyRecruited);
            }
        }
    }

    private void TrackNightStatus()
    {
        if (isNightTime)
        {
            // Track wolf defeats
            Wolf[] wolves = FindObjectsByType<Wolf>(FindObjectsSortMode.None);
            
            // If there are no wolves but night has started, assume some were defeated
            if (wolves.Length == 0 && nightHasStarted && !hasDefeatedFirstWolf)
            {
                hasDefeatedFirstWolf = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstWolfDefeated);
            }
        }
    }

    private void OnMoneyChanged(int newAmount)
    {
        // Trigger money earned condition
        TutorialManager.Instance?.OnConditionMet(TutorialCondition.MoneyEarned);
    }

    public void OnProductCollected()
    {
        if (!hasCollectedFirstProduct)
        {
            hasCollectedFirstProduct = true;
            TutorialManager.Instance?.OnConditionMet(TutorialCondition.AnimalProductsCollected);
        }
    }

    public void OnSynergyDiscovered()
    {
        TutorialManager.Instance?.OnConditionMet(TutorialCondition.SynergyDiscovered);
    }

    public void OnLandExpanded()
    {
        TutorialManager.Instance?.OnConditionMet(TutorialCondition.LandExpanded);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }
    }
}
