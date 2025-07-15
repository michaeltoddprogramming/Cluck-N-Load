using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Data structure for defense readiness reporting
/// </summary>
[System.Serializable]
public struct DefenseReadinessReport
{
    public bool hasBarracks;
    public bool hasFlag;
    public bool hasArmy;
    public int barracksCount;
    public int totalArmyCount;
    public bool isReady;
}

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

            // DISABLED: TrackStructurePlacements() - now handled directly by BuildController
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
                
                // TUTORIAL: Mark crop for future instant growth, but don't do it immediately
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    Debug.Log("Tutorial: Crop planted, will grow when tutorial asks for it");
                }
            }

            // Track if crops are ready for harvest - but only when tutorial is asking for it
            if (crop.CropReady && hasPlantedFirstCrop && !hasHarvestedFirstCrop)
            {
                Debug.Log("Crop is ready for harvest!");
                CheckInventoryForHarvest();
            }
        }
    }

    private IEnumerator InstantGrowCropForTutorial(CropStructure crop)
    {
        yield return new WaitForSeconds(2f); // Wait 2 seconds after planting
        
        if (crop != null && !crop.CropReady)
        {
            // Use the new tutorial method for instant growth
            crop.InstantGrowForTutorial();
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
            // Check if enough animals have been bought (require at least 3 for tutorial)
            if (animal.AnimalCount >= 3 && !hasBoughtFirstAnimal)
            {
                hasBoughtFirstAnimal = true;
                Debug.Log($"Tutorial: {animal.AnimalCount} chickens bought - triggering FirstChickenBought condition");
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstChickenBought);
            }

            // Check if animals are producing (trigger when they start production cycle)
            if (animal.IsProducing && hasBoughtFirstAnimal)
            {
                Debug.Log("Animals are now producing after being fed!");
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.ChickensStartedProducing);
                
                // TUTORIAL: Speed up production for demonstration
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    StartCoroutine(InstantCompleteAnimalProductionForTutorial(animal));
                }
            }

            // Check if products are ready to collect
            if (animal.ProductReady && hasBoughtFirstAnimal && !hasCollectedFirstProduct)
            {
                Debug.Log("Animal products are ready for collection!");
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.AnimalProductsReady);
            }
        }
    }

    private IEnumerator FastTrackAnimalProductionForTutorial(AnimalStructure animal)
    {
        yield return new WaitForSeconds(5f); // Wait 5 seconds after feeding
        
        if (animal != null && animal.IsProducing && !animal.ProductReady)
        {
            // Force production to complete for tutorial
            var animalType = animal.GetType();
            var productionProgressField = animalType.GetField("productionProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var productReadyField = animalType.GetField("productReady", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isProducingField = animalType.GetField("isProducing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (productionProgressField != null && productReadyField != null && isProducingField != null)
            {
                productionProgressField.SetValue(animal, animal.ProductionSettings.productionTime);
                productReadyField.SetValue(animal, true);
                isProducingField.SetValue(animal, false);
                Debug.Log("TUTORIAL: Instantly completed animal production for demonstration!");
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
                Debug.Log($"Tutorial: Flag placed at position {barrack.GetFlagPosition} - triggering FlagPlaced condition");
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FlagPlaced);
            }

            // Check if army has been recruited (require at least 2 soldiers for tutorial)
            if (barrack.ArmyAnimalCount >= 2 && !hasRecruitedArmy)
            {
                hasRecruitedArmy = true;
                Debug.Log($"Tutorial: Army recruited with {barrack.ArmyAnimalCount} soldiers - triggering ArmyRecruited condition");
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
            ShopUIManager shopManager = FindFirstObjectByType<ShopUIManager>();
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
    /// Enhanced to use comprehensive defense readiness checks
    /// </summary>
    private bool ShouldBlockNightProgression()
    {
        if (TutorialManager.Instance == null) return false;
        
        // Don't block if tutorial is completed
        if (TutorialManager.Instance.IsTutorialCompleted()) 
        {
            Debug.Log("Tutorial: Not blocking night - tutorial completed");
            return false;
        }
        
        // CRITICAL FIX: Check if tutorial is in progress (not completed) rather than just showing a dialog
        bool tutorialInProgress = TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialCompleted();
        
        // ALWAYS block night if tutorial is in progress but first_night step hasn't been completed
        if (tutorialInProgress && !HasCompletedTutorialStep("first_night"))
        {
            Debug.Log("Tutorial: BLOCKING night progression - tutorial in progress and first_night step not completed yet");
            return true;
        }
        
        // Block night if tutorial is in progress and defenses are not fully ready
        bool defensesNotReady = !AreDefensesReadyForTutorial();
        
        if (tutorialInProgress && defensesNotReady)
        {
            Debug.Log("Tutorial: Blocking night progression - defenses not fully ready for night phase");
            return true;
        }
        
        // ADDITIONAL CHECK: Even if basic conditions are met, ensure the user has explicitly
        // completed the "recruit_army" tutorial step before allowing night
        if (tutorialInProgress && !HasCompletedTutorialStep("recruit_army"))
        {
            Debug.Log("Tutorial: Blocking night progression - user hasn't completed army recruitment tutorial step");
            return true;
        }
        
        // CRITICAL: Block automatic night transition during the "first_night" step
        // This step requires user to manually click the "Start Night" button
        if (tutorialInProgress && IsCurrentTutorialStep("first_night"))
        {
            Debug.Log("Tutorial: Blocking automatic night progression - waiting for user to click Start Night button");
            return true;
        }
        
        Debug.Log("Tutorial: Not blocking night progression - all conditions passed");
        return false;
    }

    /// <summary>
    /// Public method for NightManager and other systems to check if night progression should be blocked
    /// This is the main method other systems should use to check if tutorial is blocking night
    /// </summary>
    public bool ShouldBlockNightTransition()
    {
        bool shouldBlock = ShouldBlockNightProgression();
        Debug.Log($"TUTORIAL BLOCKING CHECK: ShouldBlockNightTransition() returning {shouldBlock}");
        return shouldBlock;
    }

    /// <summary>
    /// Debug method to check night blocking status
    /// </summary>
    [ContextMenu("Debug Night Blocking Status")]
    public void DebugNightBlockingStatus()
    {
        Debug.Log("=== NIGHT BLOCKING DEBUG ===");
        Debug.Log($"Tutorial Manager exists: {TutorialManager.Instance != null}");
        if (TutorialManager.Instance != null)
        {
            Debug.Log($"Tutorial Active: {TutorialManager.Instance.IsTutorialActive()}");
            Debug.Log($"Tutorial Completed: {TutorialManager.Instance.IsTutorialCompleted()}");
            Debug.Log($"Defenses Ready: {AreDefensesReadyForTutorial()}");
            Debug.Log($"Army Recruited Step Completed: {HasCompletedTutorialStep("recruit_army")}");
            Debug.Log($"First Night Step Completed: {HasCompletedTutorialStep("first_night")}");
            Debug.Log($"Is Current Step 'first_night': {IsCurrentTutorialStep("first_night")}");
        }
        Debug.Log($"FINAL RESULT - Should Block Night: {ShouldBlockNightTransition()}");
        Debug.Log("========================");
    }

    /// <summary>
    /// Public method for other systems to check if tutorial should prevent enemy spawning
    /// Enhanced to include comprehensive defense readiness checks
    /// </summary>
    public bool ShouldPreventEnemySpawning()
    {
        if (TutorialManager.Instance == null) return false;
        
        // Allow enemies if tutorial is completed
        if (TutorialManager.Instance.IsTutorialCompleted()) return false;
        
        // During tutorial, check comprehensive defense readiness
        bool tutorialInProgress = !TutorialManager.Instance.IsTutorialCompleted();
        bool defensesReady = AreDefensesReadyForTutorial();
        
        if (tutorialInProgress && !defensesReady)
        {
            Debug.Log("Tutorial: Preventing enemy spawning - defenses not ready");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Comprehensive check for defense readiness during tutorial
    /// </summary>
    private bool AreDefensesReadyForTutorial()
    {
        // Must have built barracks first
        if (!hasBarracksPlaced)
        {
            Debug.Log("Tutorial Defense Check: No barracks placed yet");
            return false;
        }
        
        // Must have placed flag
        if (!hasPlacedFlag)
        {
            Debug.Log("Tutorial Defense Check: No flag placed yet");
            return false;
        }
        
        // Must have recruited army
        if (!hasRecruitedArmy)
        {
            Debug.Log("Tutorial Defense Check: No army recruited yet");
            return false;
        }
        
        // Verify barracks actually exist and have army
        BarracksStructure[] barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        if (barracks.Length == 0)
        {
            Debug.Log("Tutorial Defense Check: No barracks found");
            return false;
        }
        
        bool hasActiveBarracksWithArmy = false;
        foreach (BarracksStructure barrack in barracks)
        {
            // Check if barracks has army and flag
            if (barrack.ArmyAnimalCount > 0 && barrack.GetFlagPosition != Vector3.zero)
            {
                hasActiveBarracksWithArmy = true;
                break;
            }
        }
        
        if (!hasActiveBarracksWithArmy)
        {
            Debug.Log("Tutorial Defense Check: No active barracks with army and flag");
            return false;
        }
        
        Debug.Log("Tutorial Defense Check: Defenses are ready! (Barracks + Flag + Army)");
        return true;
    }

    private IEnumerator InstantCompleteAnimalProductionForTutorial(AnimalStructure animal)
    {
        yield return new WaitForSeconds(2f); // Wait 2 seconds
        
        if (animal != null && animal.AnimalCount > 0)
        {
            // Use the new tutorial method for instant production
            animal.InstantCompleteProductionForTutorial();
        }
    }

    /// <summary>
    /// Debug method to log current defense status - useful for troubleshooting tutorial progression
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogDefenseStatus()
    {
        Debug.Log("=== TUTORIAL DEFENSE STATUS ===");
        Debug.Log($"Barracks Placed: {hasBarracksPlaced}");
        Debug.Log($"Flag Placed: {hasPlacedFlag}");
        Debug.Log($"Army Recruited: {hasRecruitedArmy}");
        
        BarracksStructure[] barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        Debug.Log($"Barracks Found: {barracks.Length}");
        
        foreach (BarracksStructure barrack in barracks)
        {
            Debug.Log($"Barracks Army Count: {barrack.ArmyAnimalCount}");
            Debug.Log($"Barracks Flag Position: {barrack.GetFlagPosition}");
            Debug.Log($"Barracks Has Active Defense: {barrack.ArmyAnimalCount > 0 && barrack.GetFlagPosition != Vector3.zero}");
        }
        
        bool defensesReady = AreDefensesReadyForTutorial();
        Debug.Log($"Overall Defenses Ready: {defensesReady}");
        Debug.Log($"Should Prevent Enemy Spawning: {ShouldPreventEnemySpawning()}");
        Debug.Log("================================");
    }
    
    /// <summary>
    /// Public method for external systems to check if tutorial defenses are ready
    /// Useful for UI indicators, night manager, etc.
    /// </summary>
    public bool AreDefensesReady()
    {
        return AreDefensesReadyForTutorial();
    }
    
    /// <summary>
    /// Get a detailed defense readiness report for UI or debugging
    /// </summary>
    public DefenseReadinessReport GetDefenseReadinessReport()
    {
        return new DefenseReadinessReport
        {
            hasBarracks = hasBarracksPlaced,
            hasFlag = hasPlacedFlag,
            hasArmy = hasRecruitedArmy,
            barracksCount = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None).Length,
            totalArmyCount = GetTotalArmyCount(),
            isReady = AreDefensesReadyForTutorial()
        };
    }
    
    private int GetTotalArmyCount()
    {
        int totalArmy = 0;
        BarracksStructure[] barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        foreach (BarracksStructure barrack in barracks)
        {
            totalArmy += barrack.ArmyAnimalCount;
        }
        return totalArmy;
    }

    /// <summary>
    /// Manually trigger crop growth for tutorial timing control
    /// Call this when the tutorial step specifically asks for crop growth
    /// </summary>
    public void TriggerCropGrowthForTutorial()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive())
        {
            Debug.LogWarning("TriggerCropGrowthForTutorial called but tutorial is not active!");
            return;
        }
        
        CropStructure[] cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        
        foreach (CropStructure crop in cropStructures)
        {
            if (crop.CurrentCropType != CropStructure.CropType.None && !crop.CropReady)
            {
                Debug.Log("Tutorial: Manually growing crop for tutorial step!");
                StartCoroutine(InstantGrowCropForTutorial(crop));
                break; // Only grow the first planted crop
            }
        }
    }

    /// <summary>
    /// Called by BuildController when a structure is placed
    /// This replaces the automatic tracking to ensure timing is correct
    /// </summary>
    public void OnStructurePlaced(StructureType structureType, string structureName)
    {
        Debug.Log($"TutorialConditionTracker: Structure placed - Type: {structureType}, Name: {structureName}");
        
        // Mark first structure as placed
        if (!hasPlacedFirstStructure)
        {
            hasPlacedFirstStructure = true;
            TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstStructurePlaced);
        }
        
        // Handle specific structure types
        switch (structureType)
        {
            case StructureType.Building:
                // Check if it's a farmhouse by name
                if (!hasFarmHousePlaced && (structureName.ToLower().Contains("farmhouse") || 
                    structureName.ToLower().Contains("farm house")))
                {
                    hasFarmHousePlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.FarmHousePlaced);
                }
                break;
                
            case StructureType.Silo:
                if (!hasSiloPlaced)
                {
                    hasSiloPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.SiloPlaced);
                }
                break;
                
            case StructureType.CropPlot:
                if (!hasCropPlotPlaced)
                {
                    hasCropPlotPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.CropPlotPlaced);
                }
                break;
                
            case StructureType.Animal:
            case StructureType.AnimalPlot:
                if (!hasChickenCoopPlaced && (structureName.ToLower().Contains("chicken") || 
                    structureName.ToLower().Contains("coop")))
                {
                    hasChickenCoopPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.ChickenCoopPlaced);
                }
                break;
                
            case StructureType.Barracks:
                if (!hasBarracksPlaced)
                {
                    hasBarracksPlaced = true;
                    TutorialManager.Instance?.OnConditionMet(TutorialCondition.BarracksPlaced);
                }
                break;
        }
    }

    /// <summary>
    /// Check if a specific tutorial step has been completed
    /// </summary>
    private bool HasCompletedTutorialStep(string stepId)
    {
        if (TutorialManager.Instance == null) return false;
        
        // Check if the specific tutorial step has been marked as completed
        var tutorialSteps = TutorialManager.Instance.GetTutorialSteps();
        if (tutorialSteps != null)
        {
            foreach (var step in tutorialSteps)
            {
                if (step.stepId == stepId)
                {
                    return step.isCompleted;
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// Check if the tutorial is currently on a specific step
    /// </summary>
    private bool IsCurrentTutorialStep(string stepId)
    {
        if (TutorialManager.Instance == null) return false;
        
        // Check if we're currently active and the current step matches
        if (!TutorialManager.Instance.IsTutorialActive()) return false;
        
        // Try to get the current step via reflection or find another way to check current step
        // For now, we'll use a different approach - check if the step is incomplete but its prerequisites are met
        var tutorialSteps = TutorialManager.Instance.GetTutorialSteps();
        if (tutorialSteps != null)
        {
            foreach (var step in tutorialSteps)
            {
                if (step.stepId == stepId && !step.isCompleted)
                {
                    // Check if all prerequisites are met (indicating this is likely the current step)
                    bool allPrereqsMet = true;
                    foreach (var prereq in step.prerequisites)
                    {
                        bool prereqMet = false;
                        foreach (var checkStep in tutorialSteps)
                        {
                            if (checkStep.triggerCondition == prereq && checkStep.isCompleted)
                            {
                                prereqMet = true;
                                break;
                            }
                        }
                        if (!prereqMet)
                        {
                            allPrereqsMet = false;
                            break;
                        }
                    }
                    return allPrereqsMet;
                }
            }
        }
        
        return false;
    }
}
