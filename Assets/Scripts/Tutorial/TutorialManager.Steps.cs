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
            instructionText = "Whoa! You scared me! I'm Pete. Let's build us a farm!",
            triggerToWaitFor = TutorialTrigger.None
        });

        // Melony Hunt: Movement Controls
        var melonyMovementStep = new TutorialStep
        {
            stepId = "melony_movement",
            title = "Find Melony - Movement!",
            instructionText = "Melony's hiding! Use all movement keys below, then find and click her!",
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
            instructionText = "Use mouse wheel OR keys 1 and 2 to zoom in/out, then find and click Melony!",
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
            instructionText = "Hold middle mouse + move to rotate camera, then find Melony!",
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
            instructionText = "Pause, play, fast-forward. Control time itself!",
            triggerToWaitFor = TutorialTrigger.TimeControlsUsed,
            uiToHighlight = GameObject.Find("PAUSE BG")
        });

        var seasonBonusStep = new TutorialStep
        {
            stepId = "season_bonuses",
            title = "Seasonal Bonuses",
            instructionText = "Seasons boost production from different animals. Watch the season icon!",
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
            instructionText = "Watch this to know what types of enemies you can possibly expect at night! Every new season brings new threats!",
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
            instructionText = "Click the shop bottom-left to start building your farm",
            triggerToWaitFor = TutorialTrigger.ShopOpened,
            uiToHighlight = shopButton ?? GameObject.Find("ShopButton") ?? GameObject.FindGameObjectWithTag("ShopButton")
        });

        var farmhouseStep = new TutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Farmhouse",
            instructionText = "Build your Farmhouse first! WARNING: If destroyed, you lose! Protect it!",
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
            instructionText = "Build a Silo to store harvested crops.",
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
            instructionText = "Smart farmers check prices! Click the price panel to see what your crops and animals are worth!",
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

        steps.Add(new TutorialStep
        {
            stepId = "price_panel_explanation",
            title = "Market Intelligence",
            instructionText = "See the numbers? Left shows your inventory, right shows current prices! Green % means seasonal bonus. Close when done!",
            triggerToWaitFor = TutorialTrigger.PricePanelClosed
        });

        var plantCropStep = new TutorialStep
        {
            stepId = "plant_first_crop",
            title = "Plant Crops",
            instructionText = "Click your Crop Plot. Plant sunflowers - free animal food means more profit!",
            triggerToWaitFor = TutorialTrigger.PlantedCrop
        };
        plantCropStep.onStepStart = new UnityEvent();
        plantCropStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("CropPlot"));
        steps.Add(plantCropStep);

        var harvestCropStep = new TutorialStep
        {
            stepId = "harvest_first_crops",
            title = "Harvest!",
            instructionText = "Sunflowers are ready! (I sped up the growth) Click Harvest. Free chicken food = bigger profits!",
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
            instructionText = "Train your chickens! Build Barracks for your feathered fighters.",
            triggerToWaitFor = TutorialTrigger.BuiltChickenBarracks
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
            instructionText = "Get 3 chickens. Click your Coop, buy, and let the egg party begin!",
            triggerToWaitFor = TutorialTrigger.BoughtFirstAnimals
        };
        buyChickensStep.onStepStart = new UnityEvent();
        buyChickensStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("ChickenCoop"));
        steps.Add(buyChickensStep);

        steps.Add(new TutorialStep
        {
            stepId = "feed_chickens",
            title = "Feed Chickens",
            instructionText = "Feed chickens to make eggs! Well-fed animals = ready to produce. Click Feed.",
            triggerToWaitFor = TutorialTrigger.FedFirstAnimals
        });

        steps.Add(new TutorialStep
        {
            stepId = "collect_eggs",
            title = "Collect Eggs",
            instructionText = "Eggs ready! Click Collect - eggs automatically sell for coins! This is how you make money.",
            triggerToWaitFor = TutorialTrigger.CollectedFirstProducts
        });

        var recruitSoldiersStep = new TutorialStep
        {
            stepId = "recruit_soldiers",
            title = "Chicken Soldiers",
            instructionText = "Time for battle! Recruit chickens in the Barracks.",
            triggerToWaitFor = TutorialTrigger.RecruitedFirstSoldiers
        };
        recruitSoldiersStep.onStepStart = new UnityEvent();
        recruitSoldiersStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("ChickenBarracks"));
        steps.Add(recruitSoldiersStep);

        // Wall Building Tutorial - Step 1: Learn hay bale placement via chain cancel
        // This teaches the chain building mechanic: click to start, right-click to cancel (places 1 wall)
        // Tutorial trigger fired by CancelDefenceChain() method in BuildController
        var buildFirstHayBaleStep = new TutorialStep
        {
            stepId = "build_first_hay_bale",
            title = "Wall Building Basics",
            instructionText = "Select hay bale, CLICK to place first one. Then move mouse and RIGHT-CLICK to cancel (places just that 1 wall).",
            triggerToWaitFor = TutorialTrigger.BuiltFirstHayBale
        };
        buildFirstHayBaleStep.onStepStart = new UnityEvent();
        buildFirstHayBaleStep.onStepStart.AddListener(() =>
        {
            StartCoroutine(WaitForShopToOpen("HayBale"));
        });
        steps.Add(buildFirstHayBaleStep);

        // Wall Building Tutorial - Step 2: Build full wall chains
        // This teaches full chain building: click to start, move mouse, click again to place all
        // Tutorial trigger fired by FinalizeDefenceChain() method when total hay bales >= 10
        var buildWallChainStep = new TutorialStep
        {
            stepId = "build_wall_chain",
            title = "Chain Building (9 More)",
            instructionText = "Great! Build 9 more hay bales. CLICK places first, move mouse to chain, then CLICK again to confirm all!",
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
            instructionText = "Set a rally point! Click Place Flag, then pick a spot.",
            triggerToWaitFor = TutorialTrigger.PlacedFirstFlag
        };
        placeFlagStep.onStepStart = new UnityEvent();
        placeFlagStep.onStepStart.AddListener(() => { HighlightLastBuiltStructure("ChickenBarracks"); HighlightPlaceFlagButton(); });
        steps.Add(placeFlagStep);

        var finalStep = new TutorialStep
        {
            stepId = "prepare_defense",
            title = "Farm Defended!",
            instructionText = "You did it! Night time starts soon, so prepare to defend!",
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
            instructionText = "Spring! Plant stuff. Animals are happy.",
            triggerToWaitFor = TutorialTrigger.SpringSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_summer",
            title = "Summer!",
            instructionText = "Summer! Crops grow fast. Wolves get cranky.",
            triggerToWaitFor = TutorialTrigger.SummerSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_fall",
            title = "Fall!",
            instructionText = "Fall! Harvest time. Animals eat more.",
            triggerToWaitFor = TutorialTrigger.FallSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_winter",
            title = "Winter!",
            instructionText = "Winter! Crops slow down. Animals need more food.",
            triggerToWaitFor = TutorialTrigger.WinterSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "animal_production_boost",
            title = "Production Boost!",
            instructionText = "Bonus time! Some animals produce more. Check the icons.",
            triggerToWaitFor = TutorialTrigger.AnimalProductionBoosted
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_cow_pen",
            title = "Cow Pen!",
            instructionText = "Cows make milk. They eat wheat and train into sturdy, slow but powerful soldiers.",
            triggerToWaitFor = TutorialTrigger.BuiltCowPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_pen",
            title = "Sheep Pen!",
            instructionText = "Sheep make wool. They eat wheat and train into tough defenders with armor and explosives.",
            triggerToWaitFor = TutorialTrigger.BuiltSheepPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_pen",
            title = "Goat Pen!",
            instructionText = "Goats make cheese. They eat carrots and train into long-range snipers.",
            triggerToWaitFor = TutorialTrigger.BuiltGoatPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_pen",
            title = "Pig Pen!",
            instructionText = "Pigs make bacon. They eat carrots and train into flamethrower soldiers.",
            triggerToWaitFor = TutorialTrigger.BuiltPigPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_cow_barracks",
            title = "Cow Barracks!",
            instructionText = "Train cow soldiers. Slow but strong at mid range, great against groups.",
            triggerToWaitFor = TutorialTrigger.BuiltCowBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_barracks",
            title = "Sheep Barracks!",
            instructionText = "Train sheep soldiers. Armored and explosive, they deal huge damage up close, if at least 3 enemies are nearby.",
            triggerToWaitFor = TutorialTrigger.BuiltSheepBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_barracks",
            title = "Goat Barracks!",
            instructionText = "Train goat soldiers. Snipers that pick off enemies from long range.",
            triggerToWaitFor = TutorialTrigger.BuiltGoatBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_barracks",
            title = "Pig Barracks!",
            instructionText = "Train pig soldiers. Flamethrowers that burn groups of enemies at mid range.",
            triggerToWaitFor = TutorialTrigger.BuiltPigBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_price_panel",
            title = "Price Panel Discovery!",
            instructionText = "Market prices revealed! Smart farming means knowing when to sell!",
            triggerToWaitFor = TutorialTrigger.PricePanelOpened
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_price_panel_usage",
            title = "Market Master!",
            instructionText = "You've learned to read the markets! Knowledge is power and profit!",
            triggerToWaitFor = TutorialTrigger.PricePanelClosed
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
        GameObject[] structures = GameObject.FindGameObjectsWithTag(structureType);
        if (structures.Length > 0)
        {
            HighlightWorldStructure(structures[structures.Length - 1], true);
            return;
        }
        foreach (Structure structure in FindObjectsByType<Structure>(FindObjectsSortMode.None))
        {
            if (structure.gameObject.name.Contains(structureType))
            {
                HighlightWorldStructure(structure.gameObject, true);
                return;
            }
        }
    }

    private void HighlightWorldStructure(GameObject structure, bool enable)
    {
        if (structure == null) return;
        LeanTween.cancel(structure);
        if (!originalScales.ContainsKey(structure))
            originalScales[structure] = structure.transform.localScale;
        Transform highlightIndicator = structure.transform.Find("TutorialHighlight");
        if (highlightIndicator == null && enable)
        {
            if (highlightPrefab == null)
                return;
            GameObject highlight = Instantiate(highlightPrefab, structure.transform);
            highlight.transform.localPosition = new Vector3(0, 3.5f, 0);
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
            LeanTween.scale(structure, structure.transform.localScale * 1.08f, 0.5f)
                .setLoopPingPong()
                .setIgnoreTimeScale(true)
                .setEase(LeanTweenType.easeInOutQuad);
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
                LeanTween.scale(structure, structure.transform.localScale * 1.08f, 0.5f)
                    .setLoopPingPong()
                    .setIgnoreTimeScale(true)
                    .setEase(LeanTweenType.easeInOutQuad);
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
            }
        }
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

    private void CleanupAllWorldHighlights()
    {
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

        Debug.Log($"Spawned Melony for task: {task} at grid-based position: {spawnPosition}");
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
            ShowMelonyFeedback($"Use all keys first! Missing: {missingKeys.Replace("Mouse", "Mouse ")}");
            return; // Don't complete the step until they've used all required controls
        }

        if (!hasRequiredAction)
        {
            Debug.Log($"Player found Melony but hasn't used {currentMelonyTask} controls yet! Try using the controls first.");
            ShowMelonyFeedback($"Practice {currentMelonyTask} first!");
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
}