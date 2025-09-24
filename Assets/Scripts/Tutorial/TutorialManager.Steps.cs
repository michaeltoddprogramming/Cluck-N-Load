using System.Collections;
using System.Collections.Generic;
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
            instructionText = "Whoa! You scared me! I'm Pete. Let's make this farm legendary.",
            triggerToWaitFor = TutorialTrigger.None
        });

        steps.Add(new TutorialStep
        {
            stepId = "camera_controls",
            title = "Look Around",
            instructionText = "Use WASD, Q/E to move. Mouse wheel to zoom. Scroll wheel + middle mouse to rotate!",
            triggerToWaitFor = TutorialTrigger.InputDetected,
            requiredInputs = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Q, KeyCode.E, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse2 }
        });

        steps.Add(new TutorialStep
        {
            stepId = "day_night_panel",
            title = "Day/Night",
            instructionText = "See the top middle? That’s the clock. Wolves love the night. Melony didn’t.",
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
            instructionText = "Seasons boost different animals. Watch the season icon!",
            triggerToWaitFor = TutorialTrigger.None
        };
        seasonBonusStep.onStepStart = new UnityEvent();
        seasonBonusStep.onStepStart.AddListener(() =>
        {
            GameObject seasonDisplay = GameObject.Find("SeasonIcon") ?? GameObject.Find("Season");
            if (seasonDisplay != null)
                HighlightUI(seasonDisplay, true);
            if (NightManager.Instance != null)
                NightManager.Instance.ShowSimplifiedTutorialSeasonBonus();
        });
        seasonBonusStep.onStepComplete = new UnityEvent();
        seasonBonusStep.onStepComplete.AddListener(() =>
        {
            GameObject seasonDisplay = GameObject.Find("SeasonIcon") ?? GameObject.Find("Season");
            if (seasonDisplay != null)
                HighlightUI(seasonDisplay, false);
        });
        steps.Add(seasonBonusStep);

        steps.Add(new TutorialStep
        {
            stepId = "open_build_shop",
            title = "Shop Time",
            instructionText = "Numbers, clocks... yawn. Click the shop bottom-left. Let’s build!",
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
            instructionText = "Let’s grow grub. Build a Crop Plot. Sunflowers, here we come!",
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
            instructionText = "Let’s get clucking! Build a Chicken Coop. Eggs and soldiers await.",
            triggerToWaitFor = TutorialTrigger.BuiltChickenCoop,
            uiToHighlight = shopButton
        };
        chickenCoopStep.onStepStart = new UnityEvent();
        chickenCoopStep.onStepStart.AddListener(() => {
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
        chickenBarracksStep.onStepStart.AddListener(() => {
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
            instructionText = "You did it! Eggs, soldiers, and no wolves (hopefully). Night time starts soon make sure that flag is placed or not you'll find out!",
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
            instructionText = "Winter! Crops slow down. Feed animals extra.",
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
            instructionText = "Cows produce valuable milk! They eat wheat and make sturdy soldiers - slow but powerful.",
            triggerToWaitFor = TutorialTrigger.BuiltCowPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_pen",
            title = "Sheep Pen!",
            instructionText = "Sheep produce wool! They eat wheat like cows but their soldiers have natural wool armor - tough defenders!",
            triggerToWaitFor = TutorialTrigger.BuiltSheepPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_pen",
            title = "Goat Pen!",
            instructionText = "Goats produce cheese! They eat carrots and are agile climbers - their soldiers are fast mountain ninjas!",
            triggerToWaitFor = TutorialTrigger.BuiltGoatPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_pen",
            title = "Pig Pen!",
            instructionText = "Pigs produce bacon! They eat carrots like goats but their soldiers charge fearlessly and break through enemy lines!",
            triggerToWaitFor = TutorialTrigger.BuiltPigPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_cow_barracks",
            title = "Cow Barracks!",
            instructionText = "Train cow soldiers! Heavy hitters with high health - perfect tanks for your army formations.",
            triggerToWaitFor = TutorialTrigger.BuiltCowBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_barracks",
            title = "Sheep Barracks!",
            instructionText = "Train sheep soldiers! Natural wool armor makes them excellent defensive units with great survivability.",
            triggerToWaitFor = TutorialTrigger.BuiltSheepBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_barracks",
            title = "Goat Barracks!",
            instructionText = "Train goat soldiers! Fast and agile mountain fighters - perfect for hit-and-run tactics!",
            triggerToWaitFor = TutorialTrigger.BuiltGoatBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_barracks",
            title = "Pig Barracks!",
            instructionText = "Train pig soldiers! Fearless chargers who break enemy formations with devastating rushes!",
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
                    tabClickHandler = () => {
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
        bool found = false;
        foreach (TextMeshProUGUI text in shopPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string textContent = text.text.Replace(" ", "").ToLowerInvariant();
            if (textContent.Contains(searchName) || textContent.Contains(singularName) || textContent.Contains(pluralName))
            {
                Button button = text.GetComponentInParent<Button>();
                if (button != null)
                {
                    HighlightUI(button.gameObject, true);
                    found = true;
                    break;
                }
            }
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

    private void CleanupShopHighlights()
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
}