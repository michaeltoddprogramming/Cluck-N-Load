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
    private bool hasOpenedShop = false;
    
    // Structure-specific tracking
    private bool hasFarmHousePlaced = false;
    private bool hasSiloPlaced = false;
    private bool hasCropPlotPlaced = false;
    private bool hasChickenCoopPlaced = false;
    private bool hasBarracksPlaced = false;

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
            TrackShopStatus();
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

                // Night started - but only trigger if not blocked by tutorial
                if (currentIsNight && !wasNight && !nightHasStarted)
                {
                    if (!ShouldBlockNightProgression())
                    {
                        nightHasStarted = true;
                        TutorialManager.Instance?.OnConditionMet(TutorialCondition.NightStarted);
                    }
                    else
                    {
                        Debug.Log("Tutorial: Night start condition blocked - tutorial in progress");
                    }
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
        // Count all structures in the scene (excluding BuildGhost)
        Structure[] allStructures = FindObjectsByType<Structure>(FindObjectsSortMode.None);
        int actualStructureCount = 0;
        
        foreach (Structure structure in allStructures)
        {
            // Only count non-BuildGhost structures
            if (structure.gameObject.name != "BuildGhost")
            {
                actualStructureCount++;
            }
        }
        
        if (actualStructureCount > structurePlacementCount)
        {
            Debug.Log($"New structure placed! Total structures: {actualStructureCount}");
            structurePlacementCount = actualStructureCount;

            if (!hasPlacedFirstStructure)
            {
                hasPlacedFirstStructure = true;
                Debug.Log("First structure placed - triggering tutorial condition!");
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstStructurePlaced);
            }

            // Check for specific structure types
            Debug.Log($"Checking {actualStructureCount} structures for specific types...");
            foreach (Structure structure in allStructures)
            {
                // Skip BuildGhost objects (they're just previews)
                if (structure.gameObject.name == "BuildGhost")
                {
                    continue;
                }
                
                Debug.Log($"Found structure in scene: {structure.name} with StructureData: {(structure.structureData != null ? structure.structureData.structureName : "NULL")} (Type: {(structure.structureData != null ? structure.structureData.type.ToString() : "NULL")})");
                CheckSpecificStructureType(structure);
            }
        }
    }

    private void CheckSpecificStructureType(Structure structure)
    {
        if (structure.structureData == null) 
        {
            Debug.LogWarning($"Structure {structure.name} has no structureData!");
            return;
        }

        // Skip BuildGhost objects (they're just previews)
        if (structure.gameObject.name == "BuildGhost")
        {
            return;
        }

        Debug.Log($"Checking structure: {structure.name}, Type: {structure.structureData.type}");

        // Check for farmhouse first (by name or type)
        if (!hasFarmHousePlaced && (structure.name.ToLower().Contains("farmhouse") || 
            structure.name.ToLower().Contains("farm house") ||
            structure.name.ToLower().Contains("mainbuilding") ||
            structure.structureData.type == StructureType.Building))
        {
            Debug.Log("Farm House placed - triggering tutorial condition!");
            hasFarmHousePlaced = true;
            TutorialManager.Instance?.OnConditionMet(TutorialCondition.FarmHousePlaced);
            return; // Exit early since we found the farmhouse
        }

        switch (structure.structureData.type)
        {
            case StructureType.Building:
                // Building type already handled above in farmhouse check
                // No need to log as unknown
                break;
                
            case StructureType.Silo:
                if (!hasSiloPlaced)
                {
                    Debug.Log("Silo placed - triggering tutorial condition!");
                    hasSiloPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.SiloPlaced);
                }
                break;
            
            case StructureType.CropPlot:
                if (!hasCropPlotPlaced)
                {
                    Debug.Log("Crop Plot placed - triggering tutorial condition!");
                    hasCropPlotPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.CropPlotPlaced);
                }
                break;
            
            case StructureType.Animal:
            case StructureType.AnimalPlot:
                if (!hasChickenCoopPlaced)
                {
                    Debug.Log("Animal structure placed - triggering tutorial condition!");
                    hasChickenCoopPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.ChickenCoopPlaced);
                }
                break;
            
            case StructureType.Barracks:
                if (!hasBarracksPlaced)
                {
                    Debug.Log("Barracks placed - triggering tutorial condition!");
                    hasBarracksPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.BarracksPlaced);
                }
                break;
                
            default:
                Debug.LogWarning($"Unknown structure type: {structure.structureData.type}");
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

    private void TrackShopStatus()
    {
        if (!hasOpenedShop)
        {
            // Find the shop UI manager
            ShopUIManager shopManager = FindObjectOfType<ShopUIManager>();
            if (shopManager != null)
            {
                // Check if shop panel is active
                GameObject shopPanel = shopManager.GetShopPanel();
                if (shopPanel != null && shopPanel.activeSelf)
                {
                    Debug.Log("Shop opened detected!");
                    hasOpenedShop = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.ShopOpened);
                }
            }
        }
    }

    private void OnMoneyChanged(int newAmount)
    {
        // Note: MoneyEarned condition disabled for now - not needed for core tutorial flow
        // TutorialManager.Instance?.OnConditionMet(TutorialCondition.MoneyEarned);
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

    /// <summary>
    /// Check if tutorial is currently active and should block night progression
    /// </summary>
    private bool ShouldBlockNightProgression()
    {
        if (TutorialManager.Instance == null) return false;
        
        // Block night if tutorial is active and we haven't completed the army recruitment step yet
        bool tutorialActive = TutorialManager.Instance.IsTutorialActive();
        bool armyNotReady = !hasRecruitedArmy;
        
        if (tutorialActive && armyNotReady)
        {
            Debug.Log("Tutorial: Blocking night progression - player not ready for night phase");
            return true;
        }
        
        return false;
    }
}
