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
            instructionText = "Where's my chicken? - Oh sorry I didn't see you there, I'm Old Pete. You can call me Pete though, I'm not that old! I am going to help you get started on this very special farm of yours. So listen up!",
            triggerToWaitFor = TutorialTrigger.None
        });

        steps.Add(new TutorialStep
        {
            stepId = "camera_controls",
            title = "Look Around the Land",
            instructionText = "Honey you left the Chicken Coop open! - Oh darn! you again, how about you look for my lost chicken?\n\nUse <color=#00FF00>WASD</color> to move the camera.\nPress <color=#00FF00>Q</color> and <color=#00FF00>E</color> to rotate.\nUse your <color=#00FF00>Mouse Wheel</color> or press <color=#00FF00>1/2</color> to zoom.",
            triggerToWaitFor = TutorialTrigger.InputDetected,
            requiredInputs = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Q, KeyCode.E, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse2 }
        });

        steps.Add(new TutorialStep
        {
            stepId = "day_night_panel",
            title = "The Day Night Clock",
            instructionText = "You didn't find her I guess. Oh Melony. That's a shame, she was really nice. At the top middle on your screen. This shows whether it's day or night. Wolves come at night, poor Melony!",
            triggerToWaitFor = TutorialTrigger.None,
            uiToHighlight = GameObject.Find("DayNightPanel")
        });

        steps.Add(new TutorialStep
        {
            stepId = "money_explanation",
            title = "Check your finances",
            instructionText = "Top right is your... wait, no - top left - this is your <color=yellow>money counter</color>. You'll need cash for buildings and animals. Speaking of cash - HONEYYY! did we pay the rent this month?!...",
            triggerToWaitFor = TutorialTrigger.None,
            uiToHighlight = GameObject.Find("GoldPanel")
        });

        steps.Add(new TutorialStep
        {
            stepId = "time_controls",
            title = "Control Game Speed",
            instructionText = "You can control time on your farm! Click the <b>Pause</b> button to freeze time when you need to think. Press <b>Play</b> to resume normal speed. Use <b>Fast Forward</b> when you want to speed things up!",
            triggerToWaitFor = TutorialTrigger.TimeControlsUsed,
            uiToHighlight = GameObject.Find("PAUSE BG")
        });

        var seasonBonusStep = new TutorialStep
        {
            stepId = "season_bonuses",
            title = "Seasons and Animal Production",
            instructionText = "Each season brings special bonuses to your animals! Different animals produce more in different seasons. Check the seasonal icons in the top panel to track the current season.",
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
            title = "Open the Shop",
            instructionText = "All these numbers and clocks its too much for me! Skrew that, click the shop icon in the bottom-left. Lets build something!",
            triggerToWaitFor = TutorialTrigger.ShopOpened,
            uiToHighlight = shopButton ?? GameObject.Find("ShopButton") ?? GameObject.FindGameObjectWithTag("ShopButton")
        });

        var farmhouseStep = new TutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Your Farmhouse",
            instructionText = "Every farm needs a house! Select the Farmhouse and place it on your land.",
            triggerToWaitFor = TutorialTrigger.BuiltFarmHouse,
            uiToHighlight = farmhouseButton
        };
        farmhouseStep.onStepStart = new UnityEvent();
        farmhouseStep.onStepStart.AddListener(() => UpdateBuildButtonReference("Farmhouse"));
        steps.Add(farmhouseStep);

        var cropPlotStep = new TutorialStep
        {
            stepId = "build_crop_plot",
            title = "Build a Crop Plot",
            instructionText = "Now let's grow some food. Open the shop and build a Crop Plot.",
            triggerToWaitFor = TutorialTrigger.BuiltCropPlot,
            uiToHighlight = cropPlotButton
        };
        cropPlotStep.onStepStart = new UnityEvent();
        cropPlotStep.onStepStart.AddListener(() => UpdateBuildButtonReference("CropPlot"));
        steps.Add(cropPlotStep);

        var siloStep = new TutorialStep
        {
            stepId = "build_silo",
            title = "Build a Storage Silo",
            instructionText = "You'll need somewhere to store your crops. Build a Storage Silo.",
            triggerToWaitFor = TutorialTrigger.BuiltSilo,
            uiToHighlight = siloButton
        };
        siloStep.onStepStart = new UnityEvent();
        siloStep.onStepStart.AddListener(() => UpdateBuildButtonReference("Silo"));
        steps.Add(siloStep);

        var plantCropStep = new TutorialStep
        {
            stepId = "plant_first_crop",
            title = "Plant Your First Crop",
            instructionText = "Time to start farming! Click on your Crop Plot and plant some sunflowers. They'll grow quickly and provide seeds for feeding animals later!",
            triggerToWaitFor = TutorialTrigger.PlantedCrop
        };
        plantCropStep.onStepStart = new UnityEvent();
        plantCropStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("CropPlot"));
        steps.Add(plantCropStep);

        var harvestCropStep = new TutorialStep
        {
            stepId = "harvest_first_crops",
            title = "Harvest Your Crops",
            instructionText = "Perfect! Your crops have grown instantly for the tutorial! Now click the Harvest button to collect your sunflowers. You'll need these seeds to feed your chickens later!",
            triggerToWaitFor = TutorialTrigger.HarvestedCrop
        };
        harvestCropStep.onStepStart = new UnityEvent();
        harvestCropStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("CropPlot"));
        steps.Add(harvestCropStep);

        var chickenCoopStep = new TutorialStep
        {
            stepId = "build_chicken_coop",
            title = "Build a Chicken Coop",
            instructionText = "Time to start your poultry empire! First click the shop button in the bottom-left corner, then build a Chicken Coop to house chickens. They'll lay eggs and can be trained as scout soldiers!",
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
            title = "Build Chicken Barracks",
            instructionText = "Create an elite poultry force! Build Chicken Barracks to train your chickens into nimble scout warriors!",
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
            title = "Buy Your First Chickens",
            instructionText = "Now let's get some chickens! Click on your Chicken Coop and buy at least 3 chickens. You'll need them to start producing eggs!",
            triggerToWaitFor = TutorialTrigger.BoughtFirstAnimals
        };
        buyChickensStep.onStepStart = new UnityEvent();
        buyChickensStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("ChickenCoop"));
        steps.Add(buyChickensStep);

        steps.Add(new TutorialStep
        {
            stepId = "feed_chickens",
            title = "Feed Your Chickens",
            instructionText = "Hungry chickens don't lay eggs! You'll need sunflower seeds to feed them. Click the Feed button when your chickens are ready to eat.",
            triggerToWaitFor = TutorialTrigger.FedFirstAnimals
        });

        steps.Add(new TutorialStep
        {
            stepId = "collect_eggs",
            title = "Collect Your First Eggs",
            instructionText = "Your chickens have laid eggs! Click the Collect button to gather them and earn money. This is how your farm makes profit!",
            triggerToWaitFor = TutorialTrigger.CollectedFirstProducts
        });

        var recruitSoldiersStep = new TutorialStep
        {
            stepId = "recruit_soldiers",
            title = "Recruit Chicken Soldiers",
            instructionText = "Time to build your army! Click on your Chicken Barracks and recruit some of your chickens as soldiers to defend against the wolves!",
            triggerToWaitFor = TutorialTrigger.RecruitedFirstSoldiers
        };
        recruitSoldiersStep.onStepStart = new UnityEvent();
        recruitSoldiersStep.onStepStart.AddListener(() => HighlightLastBuiltStructure("ChickenBarracks"));
        steps.Add(recruitSoldiersStep);

        var placeFlagStep = new TutorialStep
        {
            stepId = "place_flag",
            title = "Place Your Battle Flag",
            instructionText = "Your soldiers need a rally point! Click 'Place Flag' in the barracks menu, then click somewhere on your land to set where your army will guard. Choose a strategic position to protect your farm!",
            triggerToWaitFor = TutorialTrigger.PlacedFirstFlag
        };
        placeFlagStep.onStepStart = new UnityEvent();
        placeFlagStep.onStepStart.AddListener(() => { HighlightLastBuiltStructure("ChickenBarracks"); HighlightPlaceFlagButton(); });
        steps.Add(placeFlagStep);

        steps.Add(new TutorialStep
        {
            stepId = "prepare_defense",
            title = "Farm Defense Complete!",
            instructionText = "Excellent work! You've built the basics of a defended farm. Your chickens will lay eggs for profit, and your chicken soldiers will protect you from wolves at night. Remember: wolves attack at night, so keep your defenses strong!",
            triggerToWaitFor = TutorialTrigger.None
        });

        InitializeDiscoverySteps();
        CleanupAllWorldHighlights();
    }

    private void InitializeDiscoverySteps()
    {
        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_spring",
            title = "Spring Season",
            instructionText = "Spring has arrived! This is a great time for planting crops. The soil is moist and your animals are happy after winter's end.",
            triggerToWaitFor = TutorialTrigger.SpringSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_summer",
            title = "Summer Season",
            instructionText = "Summer is here! Crops grow faster in the heat, but your animals might need more water. Watch out for more active wolves at night.",
            triggerToWaitFor = TutorialTrigger.SummerSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_fall",
            title = "Fall Season",
            instructionText = "Fall has begun! It's harvest time - your crops will yield more now. Animals are preparing for winter by eating more.",
            triggerToWaitFor = TutorialTrigger.FallSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "season_winter",
            title = "Winter Season",
            instructionText = "Winter has arrived! Crops grow slowly now. Your animals need more food to stay warm. Wolves are hungrier and more aggressive.",
            triggerToWaitFor = TutorialTrigger.WinterSeason
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "animal_production_boost",
            title = "Production Boost!",
            instructionText = "Your animals have received a seasonal production boost! Check the Price Panel to see which animals are producing more this season.",
            triggerToWaitFor = TutorialTrigger.AnimalProductionBoosted
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_cow_pen",
            title = "Cow Pen Discovery!",
            instructionText = "Moo-ve up to bigger livestock! Cows produce milk and make excellent heavy cavalry. They eat more but provide greater rewards!",
            triggerToWaitFor = TutorialTrigger.BuiltCowPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_pen",
            title = "Sheep Pen Discovery!",
            instructionText = "Wool you look at that! Sheep produce wool and make great support units with their fluffy armor!",
            triggerToWaitFor = TutorialTrigger.BuiltSheepPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_pen",
            title = "Goat Pen Discovery!",
            instructionText = "These hardy climbers are great at reaching difficult areas and provide milk and cheese!",
            triggerToWaitFor = TutorialTrigger.BuiltGoatPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_pen",
            title = "Pig Pen Discovery!",
            instructionText = "Oink oink! Pigs are excellent foragers and make surprisingly effective heavy troops!",
            triggerToWaitFor = TutorialTrigger.BuiltPigPen
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_cow_barracks",
            title = "Cow Barracks Discovery!",
            instructionText = "Unleash the power of bovine might! Cow soldiers are your heavy tanks - slow but nearly unstoppable!",
            triggerToWaitFor = TutorialTrigger.BuiltCowBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_sheep_barracks",
            title = "Sheep Barracks Discovery!",
            instructionText = "Ewe won't believe how effective these are! Sheep soldiers have natural wool armor that provides excellent protection against enemy attacks!",
            triggerToWaitFor = TutorialTrigger.BuiltSheepBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_goat_barracks",
            title = "Goat Barracks Discovery!",
            instructionText = "These aren't your average kids! Goat soldiers are nimble mountain warriors who can navigate any terrain and strike from unexpected angles!",
            triggerToWaitFor = TutorialTrigger.BuiltGoatBarracks
        });

        RegisterDiscoveryStep(new TutorialStep
        {
            stepId = "discover_pig_barracks",
            title = "Pig Barracks Discovery!",
            instructionText = "When pigs fly... into battle! Pig soldiers are fierce berserkers whose charging attacks can break through any enemy line!",
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
                targetTabIndex = 4;
                tabName = "Decorations";
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