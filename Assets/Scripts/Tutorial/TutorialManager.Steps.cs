using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public partial class TutorialManager
{
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
            instructionText = "Time to start your poultry empire! Build a Chicken Coop to house chickens. They'll lay eggs and can be trained as scout soldiers!",
            triggerToWaitFor = TutorialTrigger.BuiltChickenCoop,
            uiToHighlight = chickenCoopButton
        };
        chickenCoopStep.onStepStart = new UnityEvent();
        chickenCoopStep.onStepStart.AddListener(() => UpdateBuildButtonReference("ChickenCoop"));
        steps.Add(chickenCoopStep);

        var chickenBarracksStep = new TutorialStep
        {
            stepId = "build_chicken_barracks",
            title = "Build Chicken Barracks",
            instructionText = "Create an elite poultry force! Build Chicken Barracks to train your chickens into nimble scout warriors!",
            triggerToWaitFor = TutorialTrigger.BuiltChickenBarracks
        };
        chickenBarracksStep.onStepStart = new UnityEvent();
        chickenBarracksStep.onStepStart.AddListener(() => UpdateBuildButtonReference("ChickenBarracks"));
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
        placeFlagStep.onStepStart.AddListener(() => { HighlightLastBuiltStructure("ChickenBarracks"); StartCoroutine(DelayedHighlightPlaceFlagButton()); });
        steps.Add(placeFlagStep);

        steps.Add(new TutorialStep
        {
            stepId = "prepare_defense",
            title = "Farm Defense Complete!",
            instructionText = "Excellent work! You've built the basics of a defended farm. Your chickens will lay eggs for profit, and your chicken soldiers will protect you from wolves at night. Remember: wolves attack at night, so keep your defenses strong!",
            triggerToWaitFor = TutorialTrigger.None
        });

        InitializeDiscoverySteps();
    }

    private void InitializeDiscoverySteps()
    {
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

    private void UpdateBuildButtonReference(string buildingName)
    {
        GameObject buildMenu = GameObject.Find("ShopPanel");
        if (buildMenu != null)
        {
            Transform buttonTransform = buildMenu.transform.Find(buildingName + "Button");
            if (buttonTransform != null)
                HighlightUI(buttonTransform.gameObject, true);
        }
    }

    private void HighlightLastBuiltStructure(string structureType)
    {
        GameObject[] structures = GameObject.FindGameObjectsWithTag(structureType);
        if (structures.Length > 0)
            HighlightUI(structures[structures.Length - 1], true);
    }

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

    private IEnumerator DelayedHighlightPlaceFlagButton()
    {
        yield return new WaitForSeconds(0.5f);
        HighlightPlaceFlagButton();
    }
}