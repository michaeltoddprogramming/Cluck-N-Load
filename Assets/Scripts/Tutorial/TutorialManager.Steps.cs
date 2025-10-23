using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public partial class TutorialManager
{
    private List<GameObject> activeWorldHighlights = new List<GameObject>();
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    public GameObject highlightPrefab;
    private Dictionary<GameObject, UnityAction> activeTabHandlers = new Dictionary<GameObject, UnityAction>();

    void InitializeTutorialSteps()
    {
        steps.Clear();

        steps.Add(new TutorialStep
        {
            stepId = "welcome",
            title = "Old Pete's Welcome",
            // instructionText = "Whoa! You scared me! I'm Pete. Let's make this farm legendary.",
            instructionText = "Whoa! You scared me! I'm <color=yellow>Pete</color>. Let's build us a <color=green>farm</color>!",
            triggerToWaitFor = TutorialTrigger.None
        });

        // Melony Hunt: Movement Controls
        var melonyMovementStep = new TutorialStep
        {
            stepId = "melony_movement",
            title = "Find Melony - Movement!",
            instructionText = "<color=magenta>Melony's</color> hiding! Use all <color=cyan>movement keys</color> above, then find and <color=yellow>click</color> her!",
            triggerToWaitFor = TutorialTrigger.MelonyMovementTest,
            requiredInputs = new System.Collections.Generic.List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Q, KeyCode.E },
            waitForAllInputs = true
        };
        melonyMovementStep.onStepStart = new UnityEvent();
        melonyMovementStep.onStepStart.AddListener(() =>
        {
            SpawnMelonyForTask("movement");
            Debug.Log("Melony Movement Step: Key indicators should now show WASD + Q/E");
        });
        steps.Add(melonyMovementStep);

        // Melony Hunt: Zoom Controls
        var melonyZoomStep = new TutorialStep
        {
            stepId = "melony_zoom",
            title = "Find Melony - Zoom!",
            instructionText = "Use <color=cyan>mouse wheel</color> OR keys <color=cyan>1 and 2</color> to zoom in/out, then find and <color=yellow>click</color> <color=magenta>Melony</color>!",
            triggerToWaitFor = TutorialTrigger.MelonyZoomTest,
            requiredInputs = new System.Collections.Generic.List<KeyCode> { KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Alpha1, KeyCode.Alpha2 }, // Mouse wheel up/down + 1/2 keys
            waitForAllInputs = false
        };
        melonyZoomStep.onStepStart = new UnityEvent();
        melonyZoomStep.onStepStart.AddListener(() =>
        {
            SpawnMelonyForTask("zoom");
            Debug.Log("Melony Zoom Step: Mouse wheel indicators should now show");
        });
        steps.Add(melonyZoomStep);

        // Melony Hunt: Rotation Controls
        var melonyRotateStep = new TutorialStep
        {
            stepId = "melony_rotate",
            title = "Find Melony - Rotate!",
            instructionText = "Hold <color=cyan>middle mouse + move</color> to rotate camera, then find <color=magenta>Melony</color>!",
            triggerToWaitFor = TutorialTrigger.MelonyRotateTest,
            requiredInputs = new System.Collections.Generic.List<KeyCode> { KeyCode.Mouse2 }, // Middle mouse button
            waitForAllInputs = true
        };
        melonyRotateStep.onStepStart = new UnityEvent();
        melonyRotateStep.onStepStart.AddListener(() =>
        {
            SpawnMelonyForTask("rotate");
            Debug.Log("Melony Rotate Step: Middle mouse button indicator should now show");
        });
        steps.Add(melonyRotateStep);

        steps.Add(new TutorialStep
        {
            stepId = "day_night_panel",
            title = "Day/Night",
            instructionText = "See the top middle? That’s the clock. Shows the time of day. Wolves love the night. Melony didn’t.",
            triggerToWaitFor = TutorialTrigger.None,
            uiToHighlight = GameObject.Find("DayNightPanel")
        });

        steps.Add(new TutorialStep
        {
            stepId = "money_explanation",
            title = "Money!",
            instructionText = "Top left: your cash stash. Spend wisely. Rent’s due, honey!",
            triggerToWaitFor = TutorialTrigger.None,
            uiToHighlight = GameObject.Find("GoldPanel")
        });

        steps.Add(new TutorialStep
        {
            stepId = "time_controls",
            title = "Time Controls",
            instructionText = "<color=cyan>Pause</color>, <color=green>play</color>, <color=yellow>fast-forward</color>. <color=orange>Control time itself!</color>",
            triggerToWaitFor = TutorialTrigger.TimeControlsUsed,
            uiToHighlight = GameObject.Find("PAUSE BG")
        });

        var seasonBonusStep = new TutorialStep
        {
            stepId = "season_bonuses",
            title = "Seasonal Bonuses",
            instructionText = "<color=green>Seasons</color> boost <color=yellow>production</color> from different <color=orange>animals</color>. Watch the <color=cyan>season icon</color>! <color=cyan><b>Synergy Tip:</b></color> Match <color=orange>animals</color> to their <color=green>bonus seasons</color> for <color=gold>maximum profit</color>! Check the price panel to see current bonuses.",
            triggerToWaitFor = TutorialTrigger.None
        };
        seasonBonusStep.onStepStart = new UnityEvent();
        seasonBonusStep.onStepStart.AddListener(() =>
        {
            // Highlight the entire Season panel
            GameObject seasonPanel = GameObject.Find("SeasonPanel");
            if (seasonPanel != null)
                HighlightUI(seasonPanel, true);
            if (NightManager.Instance != null)
                NightManager.Instance.ShowSimplifiedTutorialSeasonBonus();
        });
        seasonBonusStep.onStepComplete = new UnityEvent();
        seasonBonusStep.onStepComplete.AddListener(() =>
        {
            // Clear highlight from the Season panel
            GameObject seasonPanel = GameObject.Find("SeasonPanel");
            if (seasonPanel != null)
                HighlightUI(seasonPanel, false);
        });
        steps.Add(seasonBonusStep);

        // Enemy Indicator Tutorial Step
        var enemyIndicatorStep = new TutorialStep
        {
            stepId = "enemy_indicator_tutorial",
            title = "Enemy Indicators",
            instructionText = "Watch this to know what types of <color=red>enemies</color> you can possibly expect at <color=blue>night</color>! Every new <color=green>season</color> brings <color=orange>new threats</color>!",
            triggerToWaitFor = TutorialTrigger.None,
        };
        enemyIndicatorStep.onStepStart = new UnityEvent();
        enemyIndicatorStep.onStepStart.AddListener(() =>
        {
            // Highlight the Enemy Indicator panel
            GameObject enemyIndicatorPanel = GameObject.Find("Enemy Indicator");
            if (enemyIndicatorPanel != null)
                HighlightUI(enemyIndicatorPanel, true);
        });
        enemyIndicatorStep.onStepComplete = new UnityEvent();
        enemyIndicatorStep.onStepComplete.AddListener(() =>
        {
            // Clear highlight from Enemy Indicator panel
            GameObject enemyIndicatorPanel = GameObject.Find("Enemy Indicator");
            if (enemyIndicatorPanel != null)
                HighlightUI(enemyIndicatorPanel, false);
        });
        steps.Add(enemyIndicatorStep);

        steps.Add(new TutorialStep
        {
            stepId = "open_build_shop",
            title = "Shop Time",
            instructionText = "<color=yellow>Click</color> the <color=cyan>shop</color> <color=yellow>bottom-left</color> to start building your <color=green>farm</color>",
            triggerToWaitFor = TutorialTrigger.ShopOpened,
            uiToHighlight = shopButton ?? GameObject.Find("ShopButton") ?? GameObject.FindGameObjectWithTag("ShopButton")
        });

        var farmhouseStep = new TutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Farmhouse",
            instructionText = "Build your <color=green>Farmhouse</color> first! <color=red><b>WARNING:</b></color> If destroyed, you <color=red>lose</color>! <color=orange>Protect it!</color>",
            triggerToWaitFor = TutorialTrigger.BuiltFarmHouse,
            uiToHighlight = farmhouseButton
        };
        farmhouseStep.onStepStart = new UnityEvent();
        farmhouseStep.onStepStart.AddListener(() => UpdateBuildButtonReference("Farmhouse"));
        steps.Add(farmhouseStep);

        var cropPlotStep = new TutorialStep
        {
            stepId = "build_crop_plot",
            title = "Crop Plot",
            instructionText = "Let’s grow grub. Build a Crop Plot.",
            triggerToWaitFor = TutorialTrigger.BuiltCropPlot,
            uiToHighlight = cropPlotButton
        };
        cropPlotStep.onStepStart = new UnityEvent();
        cropPlotStep.onStepStart.AddListener(() => UpdateBuildButtonReference("CropPlot"));
        steps.Add(cropPlotStep);

        var siloStep = new TutorialStep
        {
            stepId = "build_silo",
            title = "Build Silo",
            instructionText = "Build a <color=brown>Silo</color> to <color=cyan>store</color> harvested <color=green>crops</color>.",
            triggerToWaitFor = TutorialTrigger.BuiltSilo,
            uiToHighlight = siloButton
        };
        siloStep.onStepStart = new UnityEvent();
        siloStep.onStepStart.AddListener(() => UpdateBuildButtonReference("Silo"));
        steps.Add(siloStep);

        var pricePanelStep = new TutorialStep
        {
            stepId = "price_panel_tutorial",
            title = "Check Market Prices",
            instructionText = "<color=orange>Smart farmers</color> check <color=gold>prices</color>! <color=yellow>Click</color> the <color=cyan>price panel</color> to see what your <color=green>crops</color> and <color=orange>animals</color> are worth!",
            triggerToWaitFor = TutorialTrigger.PricePanelOpened
        };
        pricePanelStep.onStepStart = new UnityEvent();
        pricePanelStep.onStepStart.AddListener(() =>
        {
            // Force close the shop first so it doesn't block the price panel
            if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen())
            {
                ShopUIManager.Instance.CloseShop();
            }

            // Try to find the clickable price panel element - check common names
            GameObject pricePanel = GameObject.Find("PricePanel") ??
                                   GameObject.Find("Price") ??
                                   GameObject.Find("PricePanelUI") ??
                                   GameObject.Find("Price Panel") ??
                                   FindFirstObjectByType<PricePanelUI>()?.gameObject;

            if (pricePanel != null)
                HighlightUI(pricePanel, true);
        });
        pricePanelStep.onStepComplete = new UnityEvent();
        pricePanelStep.onStepComplete.AddListener(() =>
        {
            GameObject pricePanel = GameObject.Find("PricePanel") ??
                                   GameObject.Find("Price") ??
                                   GameObject.Find("PricePanelUI") ??
                                   GameObject.Find("Price Panel") ??
                                   FindFirstObjectByType<PricePanelUI>()?.gameObject;

            if (pricePanel != null)
                HighlightUI(pricePanel, false);
        });
        steps.Add(pricePanelStep);

        var pricePanelExplanationStep = new TutorialStep
        {
            stepId = "price_panel_explanation",
            title = "Market Intelligence",
            instructionText = "See the numbers? <color=cyan>Left</color> shows your <color=yellow>inventory</color>, <color=cyan>right</color> shows <color=gold>current prices</color>! <color=green>Green %</color> means <color=orange>seasonal bonus</color>. Close when done!",
            triggerToWaitFor = TutorialTrigger.PricePanelClosed
        };
        pricePanelExplanationStep.onStepStart = new UnityEvent();
        pricePanelExplanationStep.onStepStart.AddListener(() =>
        {
            // Find and highlight the close button on the price panel
            GameObject closeBtn = null;
            
            // Try multiple methods to find the close button
            PricePanelUI pricePanelUI = FindFirstObjectByType<PricePanelUI>();
            if (pricePanelUI != null)
            {
                // Search within the price panel for common button names
                Transform[] children = pricePanelUI.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name.ToLower().Contains("close") || 
                        child.name.ToLower().Contains("red") ||
                        child.name == "redBtn" ||
                        child.name == "CloseButton")
                    {
                        closeBtn = child.gameObject;
                        Debug.Log($"Found close button: {child.name}");
                        break;
                    }
                }
            }
            
            // Fallback: try direct GameObject.Find
            if (closeBtn == null)
            {
                closeBtn = GameObject.Find("redBtn");
            }
            
            if (closeBtn != null)
            {
                HighlightUI(closeBtn, true);
                Debug.Log($"Highlighting close button: {closeBtn.name}");
            }
            else
            {
                Debug.LogWarning("Could not find price panel close button to highlight!");
            }
        });
        pricePanelExplanationStep.onStepComplete = new UnityEvent();
        pricePanelExplanationStep.onStepComplete.AddListener(() =>
        {
            // Clear highlight from the close button
            GameObject closeBtn = null;
            
            PricePanelUI pricePanelUI = FindFirstObjectByType<PricePanelUI>();
            if (pricePanelUI != null)
            {
                Transform[] children = pricePanelUI.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child.name.ToLower().Contains("close") || 
                        child.name.ToLower().Contains("red") ||
                        child.name == "redBtn" ||
                        child.name == "CloseButton")
                    {
                        closeBtn = child.gameObject;
                        break;
                    }
                }
            }
            
            if (closeBtn == null)
            {
                closeBtn = GameObject.Find("redBtn");
            }
            
            if (closeBtn != null)
            {
                HighlightUI(closeBtn, false);
            }
        });
        steps.Add(pricePanelExplanationStep);

        steps.Add(new TutorialStep
        {
            stepId = "synergy_explanation",
            title = "Farm Synergies!",
            instructionText = "<color=cyan><b>Key Synergies:</b></color> <color=green>Different crops</color> feed <color=orange>different animals</color>! <color=yellow>Sunflowers</color>→<color=orange>Chickens</color>, <color=yellow>Wheat</color>→<color=white>Cows/Sheep</color>, <color=orange>Carrots</color>→<color=white>Goats/Pigs</color>. <color=green>Seasons</color> boost specific <color=orange>animals</color>!",
            triggerToWaitFor = TutorialTrigger.None
        });

        var plantCropStep = new TutorialStep
        {
            stepId = "plant_first_crop",
            title = "Plant Crops",
            instructionText = "<color=yellow>Click</color> your <color=green>Crop Plot</color>. Plant <color=yellow>sunflowers</color> - <color=cyan>free animal food</color> means <color=gold>more profit</color>! <color=cyan><b>Synergy:</b></color> <color=yellow>Sunflowers</color> → <color=orange>Chickens</color> → <color=gold>Coins</color>!",
            triggerToWaitFor = TutorialTrigger.PlantedCrop
        };
        plantCropStep.onStepStart = new UnityEvent();
        plantCropStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("CropPlot"));
        steps.Add(plantCropStep);

        var harvestCropStep = new TutorialStep
        {
            stepId = "harvest_first_crops",
            title = "Harvest!",
            instructionText = "<color=yellow>Sunflowers</color> are <color=green>ready</color>! (I sped up the growth) <color=yellow>Click Harvest</color>. <color=cyan>Free chicken food</color> = <color=gold>bigger profits</color>!",
            triggerToWaitFor = TutorialTrigger.HarvestedCrop
        };
        harvestCropStep.onStepStart = new UnityEvent();
        harvestCropStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("CropPlot"));
        steps.Add(harvestCropStep);

        var chickenCoopStep = new TutorialStep
        {
            stepId = "build_chicken_coop",
            title = "Chicken Coop",
            instructionText = "Let’s get clucking! Build a Chicken Coop.",
            triggerToWaitFor = TutorialTrigger.BuiltChickenCoop,
            uiToHighlight = shopButton
        };
        chickenCoopStep.onStepStart = new UnityEvent();
        chickenCoopStep.onStepStart.AddListener(() =>
        {
            StartCoroutine(WaitForShopToOpen("ChickenCoop"));
        });
        steps.Add(chickenCoopStep);

        var chickenBarracksStep = new TutorialStep
        {
            stepId = "build_chicken_barracks",
            title = "Chicken Barracks",
            instructionText = "<color=cyan>Train</color> your <color=orange>chickens</color>! Build <color=red>Barracks</color> for your <color=yellow>feathered fighters</color>. <color=cyan><b>Synergy:</b></color> <color=orange>Civilian chickens</color> become <color=red>soldier chickens</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltChickenBarracks,
            uiToHighlight = shopButton
        };
        chickenBarracksStep.onStepStart = new UnityEvent();
        chickenBarracksStep.onStepStart.AddListener(() =>
        {
            StartCoroutine(WaitForShopToOpen("ChickenBarrack"));
        });
        steps.Add(chickenBarracksStep);

        var buyChickensStep = new TutorialStep
        {
            stepId = "buy_chickens",
            title = "Buy Chickens",
            instructionText = "Buy exactly <color=yellow><b>5 chickens</b></color> (<color=red>no more, no less</color>). <color=yellow>Click</color> your <color=orange>Coop</color>, click <color=cyan>Buy Animals</color> until you have <color=yellow><b>5</b></color>!",
            triggerToWaitFor = TutorialTrigger.Bought5CivilianAnimals
        };
        buyChickensStep.onStepStart = new UnityEvent();
        buyChickensStep.onStepStart.AddListener(() =>
        {
            HighlightLastBuiltStructure("ChickenCoop");
            HighlightBuyAnimalsButton();
        });
        steps.Add(buyChickensStep);

        // Explain the Chicken Barracks UI when player opens it (AFTER buying chickens)
        // This step completes immediately and moves to feeding step
        steps.Add(new TutorialStep
        {
            stepId = "explain_coop_ui",
            title = "Understanding Barracks",
            instructionText = "<color=yellow>Civilians</color> = recruits from <color=orange>Coop</color>. <color=red>Army</color> = trained soldiers. <color=red>Recruit</color> button trains them. <color=cyan>Place Flag</color> = defense position!",
            triggerToWaitFor = TutorialTrigger.None
        });

        var feedChickensStep = new TutorialStep
        {
            stepId = "feed_chickens",
            title = "Feed Chickens",
            instructionText = "<color=cyan>Feed</color> <color=orange>chickens</color> to make <color=yellow>eggs</color>! <color=green>Well-fed animals</color> = <color=cyan>ready to produce</color>. <color=yellow>Click Feed</color>.",
            triggerToWaitFor = TutorialTrigger.FedFirstAnimals
        };
        feedChickensStep.onStepStart = new UnityEvent();
        feedChickensStep.onStepStart.AddListener(() =>
        {
            // Ensure player has enough sunflowers to feed 5 chickens
            if (InventoryManager.Instance != null)
            {
                int currentSunflowers = InventoryManager.Instance.GetItemCount("Sunflower");
                int requiredSunflowers = 10; // Should be enough for 5 chickens
                if (currentSunflowers < requiredSunflowers)
                {
                    int toAdd = requiredSunflowers - currentSunflowers;
                    InventoryManager.Instance.AddItem("Sunflower", toAdd);
                    Debug.Log($"Tutorial: Added {toAdd} sunflowers to ensure player can feed chickens");
                }
            }
            
            // Highlight the chicken coop for feeding
            HighlightLastBuiltStructure("ChickenCoop");
        });
        steps.Add(feedChickensStep);

        var collectEggsStep = new TutorialStep
        {
            stepId = "collect_eggs",
            title = "Collect Eggs",
            instructionText = "<color=yellow>Eggs ready</color>! <color=yellow>Click Collect</color> - eggs <color=cyan>automatically sell</color> for <color=gold>coins</color>! This is how you <color=green>make money</color>.",
            triggerToWaitFor = TutorialTrigger.CollectedFirstProducts
        };
        collectEggsStep.onStepStart = new UnityEvent();
        collectEggsStep.onStepStart.AddListener(() =>
        {
            // Check if eggs were already collected (player was too fast)
            bool eggAlreadyCollected = false;
            
            // Look for any animal structure that's not producing and not ready (indicating it was collected)
            foreach (var animalStructure in FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None))
            {
                if (animalStructure != null && animalStructure.GetAnimalType == AnimalStructure.AnimalType.Chicken)
                {
                    // If chickens exist but no product is ready and not currently producing, eggs were likely collected
                    if (!animalStructure.ProductReady && !animalStructure.IsProducing && animalStructure.AnimalCount > 0)
                    {
                        eggAlreadyCollected = true;
                        Debug.Log("[Tutorial] Detected eggs were already collected - auto-completing step");
                        break;
                    }
                }
            }
            
            if (eggAlreadyCollected)
            {
                // Auto-complete this step with a small delay
                StartCoroutine(AutoCompleteCollectEggsStep());
            }
            else
            {
                // Highlight the chicken coop for collecting eggs
                HighlightLastBuiltStructure("ChickenCoop");
            }
        });
        steps.Add(collectEggsStep);

        var recruitSoldiersStep = new TutorialStep
        {
            stepId = "recruit_soldiers",
            title = "Chicken Soldiers",
            instructionText = "<color=cyan>Recruit</color> exactly <color=red><b>3 army chickens</b></color> (<color=red>no more, no less</color>). <color=yellow>Click</color> your <color=red>Barracks</color>, click <color=cyan>Recruit</color> until you have <color=red><b>3 soldiers</b></color>! <color=cyan><b>Synergy:</b></color> Uses your <color=orange>civilian animals</color> as <color=red>recruits</color>!",
            triggerToWaitFor = TutorialTrigger.Recruited3ArmyAnimals
        };
        recruitSoldiersStep.onStepStart = new UnityEvent();
        recruitSoldiersStep.onStepStart.AddListener(() =>
        {
            HighlightLastBuiltStructure("ChickenBarracks");
            HighlightRecruitButton();
        });
        steps.Add(recruitSoldiersStep);

        // Wall Building Tutorial - Step 1: Learn hay bale placement via chain cancel
        // This teaches the chain building mechanic: click and drag, right-click to cancel
        // Tutorial trigger fired by CancelDefenceChain() method in BuildController
        var buildFirstHayBaleStep = new TutorialStep
        {
            stepId = "build_first_hay_bale",
            title = "Wall Building Basics",
            instructionText = "Select <color=yellow>hay bale</color>, <color=cyan><b>CLICK and DRAG</b></color> to create a chain. <color=orange><b>RIGHT-CLICK</b></color> to cancel (places nothing).",
            triggerToWaitFor = TutorialTrigger.BuiltFirstHayBale,
            uiToHighlight = shopButton
        };
        buildFirstHayBaleStep.onStepStart = new UnityEvent();
        buildFirstHayBaleStep.onStepStart.AddListener(() =>
        {
            StartCoroutine(WaitForShopToOpen("HayBale"));
        });
        steps.Add(buildFirstHayBaleStep);

        // Wall Building Tutorial - Step 2: Build full wall chains
        // This teaches full chain building: click and drag, release to place all
        // Tutorial trigger fired by FinalizeDefenceChain() method when total hay bales >= 10
        var buildWallChainStep = new TutorialStep
        {
            stepId = "build_wall_chain",
            title = "Chain Building (10 Total)",
            instructionText = "<color=green>Great!</color> Build <color=yellow><b>10 total</b></color> hay bales. <color=cyan><b>CLICK and DRAG</b></color> to create chain, <color=orange><b>RELEASE</b></color> to place them all!",
            triggerToWaitFor = TutorialTrigger.Built10HayBales,
            requiredInputs = new List<KeyCode>(),
            waitForAllInputs = false
        };
        buildWallChainStep.onStepStart = new UnityEvent();
        buildWallChainStep.onStepStart.AddListener(() =>
        {
            // Ensure shop stays open and Defense tab is active
            if (ShopUIManager.Instance != null && !ShopUIManager.Instance.IsShopOpen())
            {
                ShopUIManager.Instance.OpenShop();
            }
            StartCoroutine(DelayedHighlightAfterShop("HayBale", 0.2f));
        });
        buildWallChainStep.onStepComplete = new UnityEvent();
        buildWallChainStep.onStepComplete.AddListener(() =>
        {
            // Exit build mode after completing wall tutorial
            BuildController buildController = FindObjectOfType<BuildController>();
            if (buildController != null)
            {
                buildController.ExitBuildMode();
            }
            // Close shop after wall tutorial
            if (ShopUIManager.Instance != null && ShopUIManager.Instance.IsShopOpen())
            {
                ShopUIManager.Instance.CloseShop();
            }
        });
        steps.Add(buildWallChainStep);

        var placeFlagStep = new TutorialStep
        {
            stepId = "place_flag",
            title = "Place Flag",
            instructionText = "Set a <color=cyan>rally point</color>! <color=yellow>Click</color> <color=red>Place Flag</color>, then <color=orange>pick a spot</color>.",
            triggerToWaitFor = TutorialTrigger.PlacedFirstFlag
        };
        placeFlagStep.onStepStart = new UnityEvent();
        placeFlagStep.onStepStart.AddListener(() => { HighlightLastBuiltStructure("ChickenBarracks"); HighlightPlaceFlagButton(); });
        steps.Add(placeFlagStep);

        var finalStep = new TutorialStep
        {
            stepId = "prepare_defense",
            title = "Farm Defended!",
            instructionText = "<color=green><b>You did it!</b></color> <color=blue>Night time</color> starts soon, so <color=orange>prepare to defend</color>! <color=cyan><b>Remember the synergies:</b></color> <color=green>Crops</color>→<color=orange>Animals</color>→<color=gold>Money</color> & <color=orange>Animals</color>→<color=red>Soldiers</color>→<color=blue>Defense</color>!",
            triggerToWaitFor = TutorialTrigger.None
        };
        finalStep.onStepStart = new UnityEvent();
        finalStep.onStepStart.AddListener(() =>
        {
            // Set time to midday so players have time to explore after tutorial
            if (NightManager.Instance != null)
            {
                NightManager.Instance.Hours = 12; // Set to 12:00 (midday)
                NightManager.Instance.Minutes = 0; // Set to exactly 12:00
            }
        });
        steps.Add(finalStep);

        InitializeDiscoverySteps();
        CleanupAllWorldHighlights();
    }

    private void InitializeDiscoverySteps()
    {
        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_spring",
            title = "Spring!",
            instructionText = "<color=green><b>Spring!</b></color> <color=yellow>Plant stuff</color>. <color=orange>Animals</color> are <color=green>happy</color>.",
            triggerToWaitFor = TutorialTrigger.SpringSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_summer",
            title = "Summer!",
            instructionText = "<color=yellow><b>Summer!</b></color> <color=green>Crops</color> grow <color=cyan>fast</color>. <color=red>Wolves</color> get <color=orange>cranky</color>.",
            triggerToWaitFor = TutorialTrigger.SummerSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_fall",
            title = "Fall!",
            instructionText = "<color=orange><b>Fall!</b></color> <color=yellow>Harvest time</color>. <color=orange>Animals</color> eat <color=red>more</color>.",
            triggerToWaitFor = TutorialTrigger.FallSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_winter",
            title = "Winter!",
            instructionText = "<color=cyan><b>Winter!</b></color> <color=green>Crops</color> <color=blue>slow down</color>. <color=orange>Animals</color> need <color=red>more food</color>.",
            triggerToWaitFor = TutorialTrigger.WinterSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "animal_production_boost",
            title = "Production Boost!",
            instructionText = "<color=green><b>Bonus time!</b></color> Some <color=orange>animals</color> produce <color=yellow>more</color>. Check the <color=cyan>icons</color>.",
            triggerToWaitFor = TutorialTrigger.AnimalProductionBoosted
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_cow_pen",
            title = "Cow Pen!",
            instructionText = "<color=white>Cows</color> make <color=cyan>milk</color>. They eat <color=yellow>wheat</color> and train into <color=orange>sturdy</color>, <color=blue>slow</color> but <color=red>powerful soldiers</color>. <color=cyan><b>Synergy:</b></color> <color=yellow>Wheat crops</color> → <color=white>Cow food</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltCowPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_pen",
            title = "Sheep Pen!",
            instructionText = "<color=white>Sheep</color> make <color=magenta>wool</color>. They eat <color=yellow>wheat</color> and train into <color=green>tough defenders</color> with <color=cyan>armor</color> and <color=red>explosives</color>. <color=cyan><b>Synergy:</b></color> <color=yellow>Wheat crops</color> → <color=white>Sheep food</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltSheepPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_pen",
            title = "Goat Pen!",
            instructionText = "<color=white>Goats</color> make <color=yellow>cheese</color>. They eat <color=orange>carrots</color> and train into <color=cyan>long-range snipers</color>. <color=cyan><b>Synergy:</b></color> <color=orange>Carrot crops</color> → <color=white>Goat food</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltGoatPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_pen",
            title = "Pig Pen!",
            instructionText = "<color=pink>Pigs</color> make <color=red>bacon</color>. They eat <color=orange>carrots</color> and train into <color=red>flamethrower soldiers</color>. <color=cyan><b>Synergy:</b></color> <color=orange>Carrot crops</color> → <color=pink>Pig food</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltPigPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_cow_barracks",
            title = "Cow Barracks!",
            instructionText = "<color=cyan>Train</color> <color=white>cow soldiers</color>. <color=blue>Slow</color> but <color=red>strong</color> at <color=orange>mid range</color>, great against <color=yellow>groups</color>. <color=cyan><b>Synergy:</b></color> <color=white>Civilian cows</color> → <color=red>Tank soldiers</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltCowBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_barracks",
            title = "Sheep Barracks!",
            instructionText = "<color=cyan>Train</color> <color=white>sheep soldiers</color>. <color=green>Armored</color> and <color=red>explosive</color>, they deal <color=orange>huge damage</color> up close, if at least <color=yellow>3 enemies</color> are nearby. <color=cyan><b>Synergy:</b></color> <color=white>Civilian sheep</color> → <color=red>Bomber soldiers</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltSheepBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_barracks",
            title = "Goat Barracks!",
            instructionText = "<color=cyan>Train</color> <color=white>goat soldiers</color>. <color=green>Snipers</color> that pick off enemies from <color=blue>long range</color>. <color=cyan><b>Synergy:</b></color> <color=white>Civilian goats</color> → <color=red>Sniper soldiers</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltGoatBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_barracks",
            title = "Pig Barracks!",
            instructionText = "<color=cyan>Train</color> <color=pink>pig soldiers</color>. <color=red>Flamethrowers</color> that <color=orange>burn groups</color> of enemies at <color=yellow>mid range</color>. <color=cyan><b>Synergy:</b></color> <color=pink>Civilian pigs</color> → <color=red>Flamethrower soldiers</color>!",
            triggerToWaitFor = TutorialTrigger.BuiltPigBarracks
        });
    }

    private IEnumerator GuidedShopHighlight(string buildingName)
    {
        yield return null;
        CleanupShopHighlights();
        yield return new WaitForSeconds(0.1f);
        GameObject shopPanel = GameObject.Find("ShopPanel");
        if (shopPanel == null)
            yield break;
        int targetTabIndex = -1;
        string tabName = "";
        switch (buildingName.ToLower())
        {
            case "chickencoop":
                targetTabIndex = 0;
                tabName = "Animals";
                break;
            case "farmhouse":
                targetTabIndex = 0;
                tabName = "Animals";
                break;
            case "silo":
            case "cropplot":
                targetTabIndex = 2;
                tabName = "Plants";
                break;
            case "chickenbarracks":
            case "chickenbarrack":
                targetTabIndex = 1;
                tabName = "Army";
                break;
            case "wall":
            case "fence":
            case "barrier":
            case "defense":
            case "defence":
            case "hay":
            case "bale":
            case "haybale":
                targetTabIndex = 3;
                tabName = "Defense";
                break;
        }
        if (targetTabIndex >= 0)
        {
            Transform navBar = shopPanel.transform.Find("Nav Bar");
            if (navBar == null)
                navBar = shopPanel.transform.Find("UI-shop/ShopPanel/Nav Bar");
            if (navBar != null)
            {
                Button[] tabButtons = navBar.GetComponentsInChildren<Button>();
                if (targetTabIndex < tabButtons.Length)
                {
                    Button targetTab = tabButtons[targetTabIndex];
                    HighlightUI(targetTab.gameObject, true);
                    UnityAction tabClickHandler = null;
                    tabClickHandler = () =>
                    {
                        HighlightUI(targetTab.gameObject, false);
                        StartCoroutine(HighlightItemAfterTabSelected(shopPanel, buildingName));
                    };
                    if (activeTabHandlers.TryGetValue(targetTab.gameObject, out UnityAction oldHandler))
                        targetTab.onClick.RemoveListener(oldHandler);
                    targetTab.onClick.AddListener(tabClickHandler);
                    activeTabHandlers[targetTab.gameObject] = tabClickHandler;
                }
            }
        }
    }

    private IEnumerator DelayedHighlightAfterShop(string buildingName, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (activeShopHighlightCoroutine != null)
        {
            StopCoroutine(activeShopHighlightCoroutine);
            activeShopHighlightCoroutine = null;
        }
        CleanupShopHighlights();
        activeShopHighlightCoroutine = StartCoroutine(GuidedShopHighlight(buildingName));
    }

    private Coroutine activeShopHighlightCoroutine;
    private void UpdateBuildButtonReference(string buildingName)
    {
        if (activeShopHighlightCoroutine != null)
        {
            StopCoroutine(activeShopHighlightCoroutine);
            activeShopHighlightCoroutine = null;
        }
        CleanupShopHighlights();
        activeShopHighlightCoroutine = StartCoroutine(GuidedShopHighlight(buildingName));
    }

    private IEnumerator HighlightItemAfterTabSelected(GameObject shopPanel, string buildingName)
    {
        yield return new WaitForSeconds(0.2f);
        CleanupShopItemHighlights(shopPanel);
        string searchName = buildingName.ToLowerInvariant();
        string singularName = searchName.EndsWith("s") ? searchName.Substring(0, searchName.Length - 1) : searchName;
        string pluralName = searchName.EndsWith("s") ? searchName : searchName + "s";
        
        // Special handling for hay bale
        string[] searchTerms = { searchName, singularName, pluralName };
        if (buildingName.ToLowerInvariant() == "haybale")
        {
            searchTerms = new string[] { "hay", "bale", "haybale" };
        }
        
        bool found = false;
        foreach (TextMeshProUGUI text in shopPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string textContent = text.text.Replace(" ", "").ToLowerInvariant();
            string textContentWithSpaces = text.text.ToLowerInvariant();
            
            foreach (string term in searchTerms)
            {
                if (textContent.Contains(term) || textContentWithSpaces.Contains(term))
                {
                    Button button = text.GetComponentInParent<Button>();
                    if (button != null)
                    {
                        Debug.Log($"Highlighting item with text: '{text.text}' for search term: '{term}'");
                        HighlightUI(button.gameObject, true);
                        found = true;
                        break;
                    }
                }
            }
            if (found) break;
        }
        if (!found)
        {
            foreach (Button button in shopPanel.GetComponentsInChildren<Button>(true))
            {
                string buttonName = button.gameObject.name.ToLowerInvariant();
                if (buttonName.Contains(searchName) || buttonName.Contains(singularName) || buttonName.Contains(pluralName))
                {
                    HighlightUI(button.gameObject, true);
                    found = true;
                    break;
                }
            }
        }
    }

    public void CleanupShopHighlights()
    {
        foreach (Outline outline in FindObjectsByType<Outline>(FindObjectsSortMode.None))
        {
            if (outline.enabled)
            {
                outline.enabled = false;
                LeanTween.cancel(outline.gameObject);
                Button button = outline.GetComponent<Button>();
                if (button != null)
                {
                    button.colors = new ColorBlock
                    {
                        normalColor = Color.white,
                        highlightedColor = new Color(0.9f, 0.9f, 0.9f),
                        pressedColor = new Color(0.8f, 0.8f, 0.8f),
                        selectedColor = new Color(0.9f, 0.9f, 0.9f),
                        disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.5f),
                        colorMultiplier = 1f,
                        fadeDuration = 0.1f
                    };
                }
            }
        }
        foreach (var entry in activeTabHandlers)
        {
            if (entry.Key != null)
            {
                Button button = entry.Key.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveListener(entry.Value);
                    HighlightUI(entry.Key, false);
                }
            }
        }
        activeTabHandlers.Clear();
        GameObject shopPanel = GameObject.Find("ShopPanel");
        if (shopPanel != null)
            CleanupShopItemHighlights(shopPanel);
    }

    private IEnumerator WaitForBuildingPlacement(TutorialTrigger triggerToWait, string stepId)
    {
        bool buildingPlaced = false;
        while (!buildingPlaced)
        {
            yield return new WaitForSeconds(0.5f);
            if (completedStepIds.Contains(stepId))
                buildingPlaced = true;
        }
        CleanupShopHighlights();
    }

    private void CleanupShopItemHighlights(GameObject shopPanel)
    {
        Button[] allButtons = shopPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in allButtons)
        {
            Outline outline = button.GetComponent<Outline>();
            if (outline != null && outline.enabled)
                HighlightUI(button.gameObject, false);
        }
    }

    private void HighlightLastBuiltStructure(string structureType)
    {
        Debug.Log($"[HighlightStructure] Looking for structure type: {structureType}");
        
        // Log all existing structures first
        Debug.Log("[HighlightStructure] All existing structures:");
        foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            Debug.Log($"  - Name: '{structure.gameObject.name}', Tag: '{structure.gameObject.tag}', Type: {structure.GetType().Name}");
        }
        
        // First try to find by tag
        GameObject[] structures = GameObject.FindGameObjectsWithTag(structureType);
        Debug.Log($"[HighlightStructure] Found {structures.Length} structures with tag '{structureType}'");
        
        if (structures.Length > 0)
        {
            GameObject targetStructure = structures[structures.Length - 1];
            Debug.Log($"[HighlightStructure] Highlighting structure with tag: {targetStructure.name} at position {targetStructure.transform.position}");
            HighlightWorldStructure(targetStructure, true);
            return;
        }
        
        // If no tagged structures found, search by name
        Debug.Log($"[HighlightStructure] No tagged structures found, searching by name containing '{structureType}'");
        foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            Debug.Log($"[HighlightStructure] Checking structure: {structure.gameObject.name}");
            if (structure.gameObject.name.ToLower().Contains(structureType.ToLower()))
            {
                Debug.Log($"[HighlightStructure] Found matching structure by name: {structure.gameObject.name} at position {structure.transform.position}");
                HighlightWorldStructure(structure.gameObject, true);
                return;
            }
        }
        
        // If still not found, try alternative names for chicken coop
        if (structureType.ToLower().Contains("chicken"))
        {
            Debug.Log("[HighlightStructure] Trying alternative chicken coop names...");
            string[] alternativeNames = { "coop", "chicken", "hen", "poultry" };
            
            foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
            {
                string structureName = structure.gameObject.name.ToLower();
                foreach (string altName in alternativeNames)
                {
                    if (structureName.Contains(altName))
                    {
                        Debug.Log($"[HighlightStructure] Found structure with alternative name: {structure.gameObject.name}");
                        HighlightWorldStructure(structure.gameObject, true);
                        return;
                    }
                }
            }
        }
        
        // Special case: If looking for ChickenCoop, try to find AnimalStructure
        if (structureType == "ChickenCoop")
        {
            Debug.Log("[HighlightStructure] Looking for AnimalStructure as chicken coop...");
            foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
            {
                if (structure is AnimalStructure)
                {
                    Debug.Log($"[HighlightStructure] Found AnimalStructure (chicken coop): {structure.gameObject.name}");
                    HighlightWorldStructure(structure.gameObject, true);
                    return;
                }
            }
        }
        
        Debug.LogWarning($"[HighlightStructure] Could not find any structure matching '{structureType}'");
    }

    private void HighlightWorldStructure(GameObject structure, bool enable)
    {
        if (structure == null) return;
        LeanTween.cancel(structure);
        if (!originalScales.ContainsKey(structure))
            originalScales[structure] = structure.transform.localScale;
        Transform highlightIndicator = structure.transform.Find("TutorialHighlight");
        
        // Show/hide the UI arrow pointing to the structure
        ShowArrowPointing(structure, enable);
        
        // Check if this is a structure with UI components that shouldn't be scaled
        Structure structureComponent = structure.GetComponent<Structure>();
        bool hasUIComponents = false;
        
        if (structureComponent != null)
        {
            // Check by actual structure type instead of name patterns
            hasUIComponents = structureComponent is BarracksStructure ||     // Chicken barracks
                             structureComponent is AnimalStructure ||       // Animal structures like chicken coops
                             structureComponent is CropStructure ||         // Crop plots with planting UI
                             structureComponent is SiloStructure ||         // Silos with storage UI
                             structureComponent.GetComponent<BaseStructureUI>() != null; // Any structure with UI component
        }
        
        // Fallback to name-based detection if component check fails
        if (!hasUIComponents)
        {
            hasUIComponents = structure.name.Contains("Barracks") || 
                              structure.name.Contains("Coop") ||        
                              structure.name.Contains("Pen") ||         
                              structure.name.Contains("FarmHouse") ||   
                              structure.name.Contains("CropPlot") ||    
                              structure.name.Contains("Silo") ||        
                              structure.GetComponentInChildren<Canvas>() != null ||
                              structure.GetComponentInChildren<UnityEngine.UI.Button>() != null;
        }
        
        Debug.Log($"[StructureHighlight] Structure: {structure.name}, Type: {structureComponent?.GetType().Name}, HasUI: {hasUIComponents}");
        
        if (highlightIndicator == null && enable)
        {
            if (highlightPrefab == null)
            {
                Debug.LogWarning("Tutorial highlightPrefab is null! 3D structure highlights won't work. Please assign a highlight prefab in the TutorialManager.");
                return;
            }
            GameObject highlight = Instantiate(highlightPrefab, structure.transform);
            
            // Calculate proper height based on structure bounds to avoid arrow going inside
            float structureHeight = GetStructureHeight(structure);
            float arrowHeight = Mathf.Max(structureHeight + 2.0f, 4.0f); // At least 2 units above structure, minimum 4 units
            highlight.transform.localPosition = new Vector3(0, arrowHeight, 0);
            
            highlight.name = "TutorialHighlight";
            activeWorldHighlights.Add(highlight);
            Renderer arrowRenderer = highlight.GetComponentInChildren<Renderer>();
            if (arrowRenderer != null)
            {
                Material mat = arrowRenderer.material;
                mat.color = new Color(1f, 1f, 0f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 1f, 0f) * 1.5f);
            }
            Light light = highlight.GetComponentInChildren<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 1f, 0f);
                light.intensity = 2f;
                light.range = 5f;
            }
            LeanTween.moveLocalY(highlight, highlight.transform.localPosition.y + 1.0f, 0.5f)
                .setLoopPingPong()
                .setIgnoreTimeScale(true)
                .setEase(LeanTweenType.easeInOutQuad);
            
            // Only scale structures that don't have UI components
            if (!hasUIComponents)
            {
                LeanTween.scale(structure, structure.transform.localScale * 1.08f, 0.5f)
                    .setLoopPingPong()
                    .setIgnoreTimeScale(true)
                    .setEase(LeanTweenType.easeInOutQuad);
            }
        }
        else if (highlightIndicator != null)
        {
            highlightIndicator.gameObject.SetActive(enable);
            if (enable)
            {
                LeanTween.moveLocalY(highlightIndicator.gameObject, highlightIndicator.transform.localPosition.y + 1.0f, 0.5f)
                    .setLoopPingPong()
                    .setIgnoreTimeScale(true)
                    .setEase(LeanTweenType.easeInOutQuad);
                
                // Only scale structures that don't have UI components
                if (!hasUIComponents)
                {
                    LeanTween.scale(structure, structure.transform.localScale * 1.08f, 0.5f)
                        .setLoopPingPong()
                        .setIgnoreTimeScale(true)
                        .setEase(LeanTweenType.easeInOutQuad);
                }
            }
            else
            {
                LeanTween.cancel(structure);
                LeanTween.scale(structure, originalScales[structure], 0.2f)
                    .setOnComplete(() =>
                    {
                        if (highlightIndicator != null)
                        {
                            Destroy(highlightIndicator.gameObject);
                            activeWorldHighlights.Remove(highlightIndicator.gameObject);
                        }
                    });
                // Hide the arrow when disabling highlight
                ShowArrowPointing(structure, false);
            }
        }
    }

    private float GetStructureHeight(GameObject structure)
    {
        if (structure == null) return 1f;
        
        // Try to get the renderer bounds to calculate actual height
        Renderer renderer = structure.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        
        // Fallback: try to get collider bounds
        Collider collider = structure.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            return collider.bounds.size.y;
        }
        
        // Default fallback height
        return 2f;
    }

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private void HighlightPlaceFlagButton()
    {
        BarracksStructureUI barracksUI = FindFirstObjectByType<BarracksStructureUI>();
        if (barracksUI != null)
            foreach (Button btn in barracksUI.GetComponentsInChildren<Button>())
                if (btn.GetComponentInChildren<TextMeshProUGUI>()?.text.Contains("Flag") == true)
                {
                    HighlightUI(btn.gameObject, true);
                    return;
                }
    }

    private void HighlightBuyAnimalsButton()
    {
        StartCoroutine(MonitorAndHighlightBuyButton());
    }

    private IEnumerator MonitorAndHighlightBuyButton()
    {
        // Keep checking until we find the Buy button (structure UI might not be open yet)
        float timeout = 10f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // Find all active buttons - don't require interactable since tutorial restrictions may disable them
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (Button btn in allButtons)
            {
                if (btn.gameObject.activeInHierarchy)
                {
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    // Check for buyAnimal button specifically (this is the serialized field name)
                    if (btn.name.Contains("buyAnimal") || btn.name.ToLower().Contains("buy"))
                    {
                        Debug.Log($"Highlighting Buy button by name: {btn.name}");
                        HighlightUIWithoutArrow(btn.gameObject, true);
                        yield break;
                    }
                    // Also check by text content
                    else if (btnText != null && btnText.text.ToLower().Contains("buy"))
                    {
                        Debug.Log($"Highlighting Buy button: {btn.name} with text: '{btnText.text}'");
                        HighlightUIWithoutArrow(btn.gameObject, true);
                        yield break;
                    }
                }
            }
            
            elapsed += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }
        
        Debug.LogWarning("Could not find Buy Animals button to highlight after timeout");
    }

    private void HighlightRecruitButton()
    {
        StartCoroutine(MonitorAndHighlightRecruitButton());
    }

    private IEnumerator MonitorAndHighlightRecruitButton()
    {
        // Keep checking until we find the Recruit button (structure UI might not be open yet)
        float timeout = 10f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // Find all active buttons - don't require interactable since tutorial restrictions may disable them
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (Button btn in allButtons)
            {
                if (btn.gameObject.activeInHierarchy)
                {
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    // Check for recruitButton specifically (this is the serialized field name)
                    if (btn.name.Contains("recruitButton") || btn.name.ToLower().Contains("recruit"))
                    {
                        Debug.Log($"Highlighting Recruit button by name: {btn.name}");
                        HighlightUIWithoutArrow(btn.gameObject, true);
                        yield break;
                    }
                    // Also check by text content (exclude "Confirm" buttons)
                    else if (btnText != null && btnText.text.ToLower().Contains("recruit") && !btnText.text.Contains("Confirm"))
                    {
                        Debug.Log($"Highlighting Recruit button: {btn.name} with text: '{btnText.text}'");
                        HighlightUIWithoutArrow(btn.gameObject, true);
                        yield break;
                    }
                }
            }
            
            elapsed += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }
        
        Debug.LogWarning("Could not find Recruit button to highlight after timeout");
    }

    private void CleanupAllWorldHighlights()
    {
        // Hide the arrow when cleaning up all highlights
        ShowArrowPointing(null, false);
        
        foreach (GameObject highlight in activeWorldHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }
        activeWorldHighlights.Clear();
        foreach (var kvp in originalScales)
        {
            if (kvp.Key != null)
            {
                LeanTween.cancel(kvp.Key);
                kvp.Key.transform.localScale = kvp.Value;
            }
        }
        originalScales.Clear();
    }

    private void CleanupStructureHighlight(string structureType)
    {
        GameObject[] structures = GameObject.FindGameObjectsWithTag(structureType);
        foreach (var structure in structures)
        {
            Transform highlight = structure.transform.Find("TutorialHighlight");
            if (highlight != null)
                Destroy(highlight.gameObject);
        }
        foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (structure.gameObject.name.Contains(structureType))
            {
                Transform highlight = structure.transform.Find("TutorialHighlight");
                if (highlight != null)
                    Destroy(highlight.gameObject);
            }
        }
    }

    private IEnumerator WaitForShopToOpen(string buildingToHighlight)
    {
        GameObject shopPanel = null;
        while (shopPanel == null)
        {
            shopPanel = GameObject.Find("ShopPanel");
            yield return new WaitForSeconds(0.2f);
        }
        yield return new WaitForSeconds(0.3f);
        UpdateBuildButtonReference(buildingToHighlight);
    }

    public void SpawnMelonyForTask(string task)
    {
        currentMelonyTask = task;
        detectedMelonyActions.Clear();

        // Clean up existing Melony
        if (currentMelony != null)
        {
            Destroy(currentMelony);
        }

        if (melonyPrefab == null)
        {
            Debug.LogError("Melony prefab not assigned! Please assign the civilian chicken prefab.");
            return;
        }

        Vector3 spawnPosition = GetMelonySpawnPositionForTask(task);
        // Create a random Y-axis rotation (keeping chicken upright)
        Quaternion uprightRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        currentMelony = Instantiate(melonyPrefab, spawnPosition, uprightRotation);

        // Make Melony clickable with a bigger click area
        SphereCollider melonyCollider = currentMelony.GetComponent<SphereCollider>();
        if (melonyCollider == null)
        {
            melonyCollider = currentMelony.AddComponent<SphereCollider>();
        }
        // Make the clickable area bigger for easier clicking
        melonyCollider.radius = 2.5f; // Increased from default ~1f to 2.5f
        melonyCollider.isTrigger = true;

        // Add a component to identify this as Melony (instead of using tags)
        currentMelony.name = "MelonyChicken_Tutorial";

        // Add a distinctive effect to make her easier to spot
        AddMelonyEffects();
    }

    private Vector3 GetMelonySpawnPositionForTask(string task)
    {
        // Get the grid system to spawn within valid building area
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            Debug.LogError("GridController not found! Melony will spawn at camera position.");
            return Camera.main.transform.position + Vector3.forward * 5f;
        }

        Camera mainCamera = Camera.main;
        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;

        // Get grid bounds
        GridDataGenerator gridDataGenerator = FindFirstObjectByType<GridDataGenerator>();
        if (gridDataGenerator == null)
        {
            Debug.LogError("GridDataGenerator not found! Using fallback position.");
            return cameraPos + Vector3.forward * 5f;
        }

        // Get camera position in grid coordinates to spawn relative to player view
        Vector2Int cameraGridPos = gridController.WorldToGridCoords(cameraPos);

        // Find all valid buildable cells first
        var validCells = GetValidBuildableCells(gridDataGenerator, gridController);
        if (validCells.Count == 0)
        {
            Debug.LogError("No valid buildable cells found! Using camera position.");
            return cameraPos + Vector3.forward * 5f;
        }

        // Filter valid cells based on task requirements
        var suitableCells = FilterCellsByTask(validCells, cameraGridPos, task);

        if (suitableCells.Count == 0)
        {
            Debug.LogWarning($"No suitable cells found for task {task}, using any valid cell.");
            suitableCells = validCells;
        }

        // Pick a random suitable cell
        Vector2Int chosenCell = suitableCells[Random.Range(0, suitableCells.Count)];
        Vector3 worldPos = gridController.GetCellCenterFromTexture(chosenCell.x, chosenCell.y);

        Debug.Log($"Melony spawn: Task={task}, GridPos=({chosenCell.x}, {chosenCell.y}), WorldPos={worldPos}, ValidCells={validCells.Count}, SuitableCells={suitableCells.Count}");

        // Convert back to world position
        return worldPos;
    }

    private System.Collections.Generic.List<Vector2Int> GetValidBuildableCells(GridDataGenerator gridDataGenerator, GridController gridController)
    {
        var validCells = new System.Collections.Generic.List<Vector2Int>();
        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null && cell.flags.isVisible && !cell.flags.isOccupied && !cell.flags.isObstacle)
                {
                    validCells.Add(new Vector2Int(x, y));
                }
            }
        }

        return validCells;
    }

    private System.Collections.Generic.List<Vector2Int> FilterCellsByTask(System.Collections.Generic.List<Vector2Int> validCells, Vector2Int cameraPos, string task)
    {
        var suitableCells = new System.Collections.Generic.List<Vector2Int>();

        foreach (var cell in validCells)
        {
            float distance = Vector2Int.Distance(cameraPos, cell);

            switch (task.ToLower())
            {
                case "movement":
                    // More constrained distance for movement practice, avoid edges
                    if (distance >= 10f && distance <= 12f && IsCellSafeFromObstacles(cell, validCells))
                        suitableCells.Add(cell);
                    break;

                case "zoom":
                    // More constrained zoom distances, avoid edges
                    if ((distance >= 4f && distance <= 5f) || (distance >= 15f && distance <= 18f))
                    {
                        if (IsCellSafeFromObstacles(cell, validCells))
                            suitableCells.Add(cell);
                    }
                    break;

                case "rotate":
                    // More constrained rotation distance, avoid edges  
                    if (distance >= 12f && distance <= 15f && IsCellSafeFromObstacles(cell, validCells))
                        suitableCells.Add(cell);
                    break;



                default:
                    // More constrained default distance, avoid edges
                    if (distance >= 8f && distance <= 15f && IsCellSafeFromObstacles(cell, validCells))
                        suitableCells.Add(cell);
                    break;
            }
        }

        return suitableCells;
    }

    private bool IsCellSafeFromObstacles(Vector2Int cell, System.Collections.Generic.List<Vector2Int> validCells)
    {
        // Check if the cell has enough clearance around it (no obstacles in a 3x3 area)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2Int neighborCell = new Vector2Int(cell.x + dx, cell.y + dy);

                // If this neighbor cell is not in our valid cells list, it might be an obstacle
                if (!validCells.Contains(neighborCell))
                {
                    return false; // Not safe - there's an obstacle nearby
                }
            }
        }
        return true; // Safe - all surrounding cells are valid
    }

    private void AddMelonyEffects()
    {
        if (currentMelony == null) return;

        // Add a glowing effect
        Light melonyLight = currentMelony.GetComponent<Light>();
        if (melonyLight == null)
        {
            melonyLight = currentMelony.AddComponent<Light>();
        }
        melonyLight.type = LightType.Point;
        // melonyLight.color = Color.yellow;
        melonyLight.color = Color.magenta;
        melonyLight.intensity = 2f;
        melonyLight.range = 8f;

        // Add a bouncing animation
        LeanTween.moveY(currentMelony, currentMelony.transform.position.y + 0.5f, 1f)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong();

        // Add rotation animation
        LeanTween.rotateAround(currentMelony, Vector3.up, 360f, 3f)
            .setLoopClamp();
    }

    public void OnMelonyClicked()
    {
        if (currentMelony == null) return;

        // Get current step to check required inputs
        if (currentStepIndex < 0 || currentStepIndex >= steps.Count) return;
        var currentStep = steps[currentStepIndex];

        // Check if all required key inputs have been detected
        bool allRequiredKeysPressed = true;
        string missingKeys = "";

        if (currentStep.requiredInputs != null && currentStep.requiredInputs.Count > 0)
        {
            foreach (KeyCode key in currentStep.requiredInputs)
            {
                if (!detectedInputs.Contains(key))
                {
                    allRequiredKeysPressed = false;
                    if (missingKeys.Length > 0) missingKeys += ", ";
                    missingKeys += key.ToString();
                }
            }
        }

        // Also check if player has performed the specific action for this task
        bool hasRequiredAction = false;
        TutorialTrigger triggerToFire = TutorialTrigger.MelonyFound;

        switch (currentMelonyTask.ToLower())
        {
            case "movement":
                hasRequiredAction = detectedMelonyActions.Contains("movement");
                triggerToFire = TutorialTrigger.MelonyMovementTest;
                break;
            case "zoom":
                hasRequiredAction = detectedMelonyActions.Contains("zoom");
                triggerToFire = TutorialTrigger.MelonyZoomTest;
                break;
            case "rotate":
                hasRequiredAction = detectedMelonyActions.Contains("rotate");
                triggerToFire = TutorialTrigger.MelonyRotateTest;
                break;

        }

        if (!allRequiredKeysPressed)
        {
            Debug.Log($"Player found Melony but hasn't used all required keys yet! Missing: {missingKeys}. Practice the controls first!");
            ShowMelonyFeedback($"<color=orange>Use all keys first!</color> Missing: <color=cyan>{missingKeys.Replace("Mouse", "Mouse ")}</color>");
            return; // Don't complete the step until they've used all required controls
        }

        if (!hasRequiredAction)
        {
            Debug.Log($"Player found Melony but hasn't used {currentMelonyTask} controls yet! Try using the controls first.");
            ShowMelonyFeedback($"<color=cyan>Practice</color> <color=yellow>{currentMelonyTask}</color> <color=orange>first!</color>");
            return; // Don't complete the step until they've used the required controls
        }

        // Play explosion effect
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, currentMelony.transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        // Play explosion sound
        if (melonyExplosionSound != null && effectsAudioSource != null)
        {
            effectsAudioSource.PlayOneShot(melonyExplosionSound);
        }

        // Destroy Melony
        Destroy(currentMelony);
        currentMelony = null;

        // Trigger the tutorial step completion
        Trigger(triggerToFire);

        Debug.Log($"Melony found for task: {currentMelonyTask}! Player demonstrated {currentMelonyTask} controls.");
    }

    private IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        Debug.Log($"AutoAdvanceAfterDelay: Starting {delay}s delay for step {currentStepIndex}");
        yield return new WaitForSeconds(delay);
        
        // Auto-advance to next step
        if (waitingForStepToComplete && currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var currentStep = steps[currentStepIndex];
            Debug.Log($"AutoAdvanceAfterDelay: Completing step '{currentStep.stepId}' at index {currentStepIndex}");
            
            // Mark step as complete if it has a stepId
            if (!string.IsNullOrEmpty(currentStep.stepId))
            {
                MarkStepComplete(currentStep.stepId);
            }
            
            // Clear any highlights
            if (currentStep.uiToHighlight != null)
            {
                HighlightUI(currentStep.uiToHighlight, false);
            }
            
            waitingForStepToComplete = false;
            lastStepCompletionTime = Time.realtimeSinceStartup;
            
            if (!isProcessingStep)
            {
                StartCoroutine(DelayedNextStep());
            }
        }
        else
        {
            Debug.Log($"AutoAdvanceAfterDelay: Cancelled - waitingForStepToComplete: {waitingForStepToComplete}, currentStepIndex: {currentStepIndex}");
        }
    }

    private IEnumerator AutoCompleteCollectEggsStep()
    {
        // Small delay to let the step UI appear briefly
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("[Tutorial] Auto-completing collect_eggs step - eggs were already collected");
        
        // Trigger the completion
        Trigger(TutorialTrigger.CollectedFirstProducts);
    }
}