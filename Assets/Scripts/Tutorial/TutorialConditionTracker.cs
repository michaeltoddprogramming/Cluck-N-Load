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
    // Returns true if tutorial logic should run (not completed)
    private bool TutorialLogicAllowed()
    {
        return TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialCompleted();
    }

    private bool hasPlacedFirstStructure = false;
    private bool hasPlantedFirstCrop = false;
    public bool hasHarvestedFirstCrop = false;
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

    // Track initial crop counts to detect new harvests
    private Dictionary<string, int> initialCropCounts = new Dictionary<string, int>();

    private void Start()
    {
        if (!TutorialLogicAllowed()) return;

        // Subscribe to relevant game events
        SubscribeToGameEvents();

        // Subscribe to crop harvest events
        CropStructure[] cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        foreach (var crop in cropStructures)
        {
            crop.OnCropHarvested += OnCropHarvested;
        }

        // Start tracking routine
        StartCoroutine(TrackGameConditions());
    }

    private void OnDestroy()
    {
        if (!TutorialLogicAllowed()) return;

        // Unsubscribe from MoneyManager events
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }

        // Unsubscribe from crop harvest events
        CropStructure[] cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        foreach (var crop in cropStructures)
        {
            crop.OnCropHarvested -= OnCropHarvested;
        }
    }

    private void SubscribeToGameEvents()
    {
        if (!TutorialLogicAllowed()) return;

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
        if (!TutorialLogicAllowed()) yield break;
        while (true)
        {
            yield return new WaitForSeconds(1f); // Check every second
            TrackCropStatus();
            TrackAnimalStatus();
            TrackDefenseStatus();
            TrackNightStatus();
            TrackShopStatus();
        }
    }

    private IEnumerator TrackDayNightChanges()
    {
        if (!TutorialLogicAllowed()) yield break;
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
        if (!TutorialLogicAllowed()) return;
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
        if (!TutorialLogicAllowed()) return;
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

        // Check for farmhouse by name, regardless of type
        if (!hasFarmHousePlaced && (structure.name.ToLower().Contains("farmhouse") || 
            structure.name.ToLower().Contains("farm house") ||
            structure.name.ToLower().Contains("mainbuilding")))
        {
            Debug.Log("Farm House placed - triggering tutorial condition!+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            structure.structureData.type = StructureType.Placed;
            hasFarmHousePlaced = true;
            TutorialManager.Instance?.OnConditionMet(TutorialCondition.FarmHousePlaced);
            // Do not return; allow other type checks to run if needed
        }

        switch (structure.structureData.type)
        {
            case StructureType.Decoration:
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
        // Debug.Log($"[Tutorial] TrackCropStatus called. TutorialActive={TutorialManager.Instance?.IsTutorialActive()}");
        if (!TutorialLogicAllowed()) return;
        CropStructure[] cropStructures = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        Debug.Log($"TutorialConditionTracker: Checking {cropStructures.Length} crop structures");
    
        foreach (CropStructure crop in cropStructures)
        {
            // Check if any crop has been planted
            if (crop.CurrentCropType != CropStructure.CropType.None && !hasPlantedFirstCrop)
            {
                Debug.Log($"[Tutorial] Crop: {crop.name}, Type={crop.CurrentCropType}, IsGrowing={crop.IsGrowing}, CropReady={crop.CropReady}");
                hasPlantedFirstCrop = true;
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstCropPlanted);
                Debug.Log($"TutorialConditionTracker: First crop planted (Type: {crop.CurrentCropType}), triggering FirstCropPlanted");
    
                // Store initial crop counts to detect new harvests
                initialCropCounts["Sunflower"] = InventoryManager.Instance.GetItemCount("Sunflower");
                initialCropCounts["Wheat"] = InventoryManager.Instance.GetItemCount("Wheat");
                initialCropCounts["Carrots"] = InventoryManager.Instance.GetItemCount("Carrots");
                Debug.Log($"Tutorial: Initial crop counts - Sunflower: {initialCropCounts["Sunflower"]}, Wheat: {initialCropCounts["Wheat"]}, Carrots: {initialCropCounts["Carrots"]}");
    
                if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
                {
                    Debug.Log("Tutorial: Crop planted, triggering instant growth");
                    StartCoroutine(InstantGrowCropForTutorial(crop));
                }
            }
    
            // Show harvest step when crop is ready
            if (crop.CropReady && hasPlantedFirstCrop && !hasHarvestedFirstCrop &&
                HasCompletedTutorialStep("plant_first_crop") && !HasCompletedTutorialStep("harvest_first_crops"))
            {
                Debug.Log($"TutorialConditionTracker: Crop ready for harvest: {crop.name}, Type: {crop.CurrentCropType}");
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstCropReady); // <-- Trigger the condition!
                TutorialStep harvestStep = TutorialManager.Instance.GetTutorialSteps().Find(s => s.stepId == "harvest_first_crops");
                if (harvestStep != null && !TutorialManager.Instance.IsTutorialActive())
                {
                    harvestStep.worldPosition = crop.transform.position + Vector3.up * 3f;
                    TutorialManager.Instance.ShowTutorialStep(harvestStep);
                    Debug.Log($"Tutorial: Showing harvest_first_crops step for crop {crop.name}");
                }
            }
        }
    
        // Check inventory for harvests
        CheckInventoryForHarvest();
    }

private void OnCropHarvested(CropStructure.CropType cropType, int amount)
{
    if (!TutorialLogicAllowed()) return;

    Debug.Log($"Tutorial: OnCropHarvested triggered for {amount} {cropType}. Current Step: {TutorialManager.Instance?.GetCurrentStepId() ?? "None"}");

    if (!hasHarvestedFirstCrop)
    {
        hasHarvestedFirstCrop = true;
        Debug.Log("Tutorial: First crop harvested, triggering FirstCropHarvested and TimeControlsExplained conditions");
        TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstCropHarvested);
        TutorialManager.Instance?.OnConditionMet(TutorialCondition.TimeControlsExplained);
        // Force immediate inventory check to confirm harvest
        CheckInventoryForHarvest();
    }
}
    public void CheckInventoryForHarvest()
    {
        if (!TutorialLogicAllowed()) return;
        if (InventoryManager.Instance != null && HasCompletedTutorialStep("plant_first_crop") && !HasCompletedTutorialStep("harvest_first_crops"))
        {
            int sunflowerCount = InventoryManager.Instance.GetItemCount("Sunflower");
            int wheatCount = InventoryManager.Instance.GetItemCount("Wheat");
            int carrotCount = InventoryManager.Instance.GetItemCount("Carrots");
            int totalNewCrops = (sunflowerCount - initialCropCounts.GetValueOrDefault("Sunflower", sunflowerCount)) +
                                (wheatCount - initialCropCounts.GetValueOrDefault("Wheat", wheatCount)) +
                                (carrotCount - initialCropCounts.GetValueOrDefault("Carrots", carrotCount));

            Debug.Log($"Tutorial: Checking inventory for harvest - Sunflower: {sunflowerCount} (New: {sunflowerCount - initialCropCounts.GetValueOrDefault("Sunflower", sunflowerCount)}), Wheat: {wheatCount} (New: {wheatCount - initialCropCounts.GetValueOrDefault("Wheat", wheatCount)}), Carrots: {carrotCount} (New: {carrotCount - initialCropCounts.GetValueOrDefault("Carrots", carrotCount)}), Total New: {totalNewCrops}");

            if (totalNewCrops > 0 && !hasHarvestedFirstCrop)
            {
                hasHarvestedFirstCrop = true;
                Debug.Log("Tutorial: First crop harvested detected via inventory!");
                TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstCropHarvested);
            }
        }


    }

    private void TrackAnimalStatus()
    {
        if (!TutorialLogicAllowed()) return;
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

private IEnumerator InstantGrowCropForTutorial(CropStructure crop)
{
    Debug.Log($"[Tutorial] InstantGrowCropForTutorial called. TutorialActive={TutorialManager.Instance?.IsTutorialActive()}, Crop={crop?.name}, CropType={crop?.CurrentCropType}, IsGrowing={crop?.IsGrowing}, CropReady={crop?.CropReady}");
    if (crop != null && crop.CurrentCropType != CropStructure.CropType.None && crop.IsGrowing && !crop.CropReady)
    {
        crop.InstantGrowForTutorial();
        yield return null;
        Debug.Log($"[Tutorial] Crop {crop.name} grown instantly. Ready={crop.CropReady}, IsGrowing={crop.IsGrowing}");
    }
    else
    {
        Debug.LogWarning($"[Tutorial] Cannot grow crop instantly. Crop={crop?.name}, CropType={crop?.CurrentCropType}, IsGrowing={crop?.IsGrowing}, CropReady={crop?.CropReady}");
        yield return null;
    }
}

    private IEnumerator InstantCompleteAnimalProductionForTutorial(AnimalStructure animal)
    {
        if (!TutorialLogicAllowed()) yield break;
        yield return new WaitForSeconds(2f); // Wait 2 seconds

        if (animal != null && animal.AnimalCount > 0)
        {
            // Use the new tutorial method for instant production
            animal.InstantCompleteProductionForTutorial();
        }
    }

    private void TrackDefenseStatus()
    {
        if (!TutorialLogicAllowed()) return;
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
        // if (!TutorialLogicAllowed()) return;
        // if (isNightTime)
        // {
        //     // Track wolf defeats
        //     Wolf[] wolves = FindObjectsByType<Wolf>(FindObjectsSortMode.None);

        //     // If there are no wolves but night has started, assume some were defeated
        //     if (wolves.Length == 0 && nightHasStarted && !hasDefeatedFirstWolf)
        //     {
        //         hasDefeatedFirstWolf = true;
        //         TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstWolfDefeated);
        //     }
        // }
    }

    private void TrackShopStatus()
    {
        if (!TutorialLogicAllowed()) return;
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
        if (!TutorialLogicAllowed()) return;
        // Note: MoneyEarned condition disabled for now - not needed for core tutorial flow
        // TutorialManager.Instance?.OnConditionMet(TutorialCondition.MoneyEarned);
    }

    public void OnProductCollected()
    {
        if (!TutorialLogicAllowed()) return;
        if (!hasCollectedFirstProduct)
        {
            hasCollectedFirstProduct = true;
            TutorialManager.Instance?.OnConditionMet(TutorialCondition.AnimalProductsCollected);
        }
    }

    public void OnSynergyDiscovered()
    {
        if (!TutorialLogicAllowed()) return;
        TutorialManager.Instance?.OnConditionMet(TutorialCondition.SynergyDiscovered);
    }

    public void OnLandExpanded()
    {
        if (!TutorialLogicAllowed()) return;
        TutorialManager.Instance?.OnConditionMet(TutorialCondition.LandExpanded);
    }

    /// <summary>
    /// Check if tutorial is currently active and should block night progression
    /// Enhanced to use comprehensive defense readiness checks
    /// </summary>
    private bool ShouldBlockNightProgression()
    {
        if (!TutorialLogicAllowed()) return false;
        if (TutorialManager.Instance == null) return false;

        // Don't block if tutorial is completed
        if (TutorialManager.Instance.IsTutorialCompleted())
        {
            Debug.Log("Tutorial: Not blocking night - tutorial completed");
            return false;
        }

        // ALWAYS block night if tutorial is in progress but first_night step hasn't been completed
        if (!HasCompletedTutorialStep("first_night"))
        {
            Debug.Log("Tutorial: BLOCKING night progression - tutorial in progress and first_night step not completed yet");
            return true;
        }

        // Block night if tutorial is in progress and defenses are not fully ready
        bool defensesNotReady = !AreDefensesReadyForTutorial();

        if (defensesNotReady)
        {
            Debug.Log("Tutorial: Blocking night progression - defenses not fully ready for night phase");
            return true;
        }

        // ADDITIONAL CHECK: Even if basic conditions are met, ensure the user has explicitly
        // completed the "recruit_army" tutorial step before allowing night
        if (!HasCompletedTutorialStep("recruit_army"))
        {
            Debug.Log("Tutorial: Blocking night progression - user hasn't completed army recruitment tutorial step");
            return true;
        }

        // CRITICAL: Block automatic night transition during the "first_night" step
        if (IsCurrentTutorialStep("first_night"))
        {
            Debug.Log("Tutorial: Blocking automatic night progression - waiting for user to click Start Night button");
            return true;
        }

        Debug.Log("Tutorial: Not blocking night progression - all conditions passed");
        return false;
    }

    /// <summary>
    /// Public method for NightManager and other systems to check if night progression should be blocked
    /// </summary>
    public bool ShouldBlockNightTransition()
    {
        if (!TutorialLogicAllowed()) return false;
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
        if (!TutorialLogicAllowed()) return;
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
    /// Public method for external systems to check if tutorial should prevent enemy spawning
    /// </summary>
    public bool ShouldPreventEnemySpawning()
    {
        if (!TutorialLogicAllowed()) return false;
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
        if (!TutorialLogicAllowed()) return false;
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

    /// <summary>
    /// Debug method to log current defense status - useful for troubleshooting tutorial progression
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogDefenseStatus()
    {
        if (!TutorialLogicAllowed()) return;
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
    /// </summary>
    public bool AreDefensesReady()
    {
        if (!TutorialLogicAllowed()) return false;
        return AreDefensesReadyForTutorial();
    }

    /// <summary>
    /// Get a detailed defense readiness report for UI or debugging
    /// </summary>
    public DefenseReadinessReport GetDefenseReadinessReport()
    {
        if (!TutorialLogicAllowed()) return new DefenseReadinessReport();
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
        if (!TutorialLogicAllowed()) return 0;
        int totalArmy = 0;
        BarracksStructure[] barracks = FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None);
        foreach (BarracksStructure barrack in barracks)
        {
            totalArmy += barrack.ArmyAnimalCount;
        }
        return totalArmy;
    }

    /// <summary>
    /// Deprecated: Crop growth is now handled in TrackCropStatus
    /// </summary>
    public void TriggerCropGrowthForTutorial()
    {
        Debug.Log("TriggerCropGrowthForTutorial called but deprecated; growth handled in TrackCropStatus");
    }

    /// <summary>
    /// Called by BuildController when a structure is placed
    /// </summary>
        public void OnStructurePlaced(StructureType structureType, string structureName)
    {
        if (!TutorialLogicAllowed()) return;
        Debug.Log($"TutorialConditionTracker: Structure placed - Type: {structureType}, Name: {structureName}");
    
        // Mark first structure as placed
        if (!hasPlacedFirstStructure)
        {
            hasPlacedFirstStructure = true;
            TutorialManager.Instance?.OnConditionMet(TutorialCondition.FirstStructurePlaced);
        }
    
        // Trigger FarmHousePlaced if the name matches, regardless of type
        if (!hasFarmHousePlaced && (structureName.ToLower().Contains("farmhouse") ||
            structureName.ToLower().Contains("farm house") ||
            structureName.ToLower().Contains("mainbuilding")))
        {
            // Debug.Log("Farm House placed - triggering tutorial condition!+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            // structure
            hasFarmHousePlaced = true;
            TutorialManager.Instance?.OnConditionMet(TutorialCondition.FarmHousePlaced);
        }
    
        // Handle specific structure types for other tutorial steps
        switch (structureType)
        {
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
                // FIX: Only trigger if this is a *newly placed* Chicken Coop, not just any animal structure
                if (!hasChickenCoopPlaced && structureName.ToLower().Contains("chicken") && structureName.ToLower().Contains("coop"))
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
    public bool HasCompletedTutorialStep(string stepId)
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

    /// <summary>
    /// Debug method to inspect crop status and inventory
    /// </summary>

    [ContextMenu("Debug Crop Status")]
    public void DebugCropStatus()
    {
        Debug.Log("=== CROP STATUS DEBUG ===");
        Debug.Log($"Has Planted First Crop: {hasPlantedFirstCrop}");
        Debug.Log($"Has Harvested First Crop: {hasHarvestedFirstCrop}");
        CropStructure[] crops = FindObjectsByType<CropStructure>(FindObjectsSortMode.None);
        foreach (var crop in crops)
        {
            Debug.Log($"Crop: {crop.name}, Type: {crop.CurrentCropType}, Growing: {crop.IsGrowing}, Ready: {crop.CropReady}");
        }
        Debug.Log($"Initial Crop Counts: Sunflower={initialCropCounts.GetValueOrDefault("Sunflower", 0)}, Wheat={initialCropCounts.GetValueOrDefault("Wheat", 0)}, Carrots={initialCropCounts.GetValueOrDefault("Carrots", 0)}");
        Debug.Log($"Current Inventory: Sunflower={InventoryManager.Instance?.GetItemCount("Sunflower")}, Wheat={InventoryManager.Instance?.GetItemCount("Wheat")}, Carrots={InventoryManager.Instance?.GetItemCount("Carrots")}");
        Debug.Log($"Silo Capacity: Current={InventoryManager.Instance?.GetCurrentSiloCapacity()}, Total={InventoryManager.Instance?.GetTotalSiloCapacity()}");
        Debug.Log($"Tutorial Step Status: plant_first_crop={HasCompletedTutorialStep("plant_first_crop")}, harvest_first_crops={HasCompletedTutorialStep("harvest_first_crops")}");
        Debug.Log("========================");
    }

[ContextMenu("Debug Tutorial Progression")]
public void DebugTutorialProgression()
{
    Debug.Log("=== TUTORIAL PROGRESSION DEBUG ===");
    Debug.Log($"Tutorial Active: {TutorialManager.Instance?.IsTutorialActive()}");
    Debug.Log($"Tutorial Completed: {TutorialManager.Instance?.IsTutorialCompleted()}");
    Debug.Log($"Current Step: {TutorialManager.Instance?.GetCurrentStepId() ?? "None"}");
    Debug.Log($"Has Planted First Crop: {hasPlantedFirstCrop}");
    Debug.Log($"Has Harvested First Crop: {hasHarvestedFirstCrop}");
    Debug.Log($"Initial Crop Counts: Sunflower={initialCropCounts.GetValueOrDefault("Sunflower", 0)}, Wheat={initialCropCounts.GetValueOrDefault("Wheat", 0)}, Carrots={initialCropCounts.GetValueOrDefault("Carrots", 0)}");
    Debug.Log($"Current Inventory: Sunflower={InventoryManager.Instance?.GetItemCount("Sunflower") ?? 0}, Wheat={InventoryManager.Instance?.GetItemCount("Wheat") ?? 0}, Carrots={InventoryManager.Instance?.GetItemCount("Carrots") ?? 0}");
    Debug.Log("================================");
}
}