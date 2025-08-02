using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    void InitializeTutorialSteps()
    {
        steps.Clear();
    
        steps.Add(new TutorialStep
        {
            stepId = "welcome",
            title = "Welcome to Cluck N Load!",
            instructionText = "Howdy, greenhorn! I'm Old Pete. I'll help you build a thriving farm... if the wolves don't get us first!",
            triggerToWaitFor = TutorialTrigger.None
        });
    
        steps.Add(new TutorialStep
        {
            stepId = "camera_controls",
            title = "Look Around Your Farm",
            instructionText = "Use <color=#00FF00>WASD</color> to move the camera.\n\n" +
                            "Press <color=#00FF00>Q</color> and <color=#00FF00>E</color> to rotate.\n\n" +
                            "Use your <color=#00FF00>Mouse Wheel</color> or press <color=#00FF00>1/2</color> to zoom.",
            triggerToWaitFor = TutorialTrigger.InputDetected,
            requiredInputs = new List<KeyCode> { 
                KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, 
                KeyCode.Q, KeyCode.E,
                KeyCode.Alpha1, KeyCode.Alpha2,
                KeyCode.Mouse3, KeyCode.Mouse4
            }
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "money_explanation",
            title = "Managing Your Money",
            instructionText = "This is your <color=yellow>money counter</color>. You'll need cash for buildings and animals. Selling crops and animal products will earn you more!",
            triggerToWaitFor = TutorialTrigger.ExplainMoney
        });
    
        steps.Add(new TutorialStep
        {
            stepId = "open_build_shop",
            title = "Open the Build Shop",
            instructionText = "Click the shop icon in the bottom-left corner to start building.",
            triggerToWaitFor = TutorialTrigger.ShopOpened,
            uiToHighlight = GameObject.FindGameObjectWithTag("ShopButton")
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "build_farmhouse",
            title = "Build Your Farmhouse",
            instructionText = "Every farm needs a house! Select the Farmhouse and place it on your land.",
            triggerToWaitFor = TutorialTrigger.BuiltFarmHouse
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "build_crop_plot",
            title = "Build a Crop Plot",
            instructionText = "Now let's grow some food. Open the shop and build a Crop Plot.",
            triggerToWaitFor = TutorialTrigger.BuiltCropPlot
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "build_silo",
            title = "Build a Storage Silo",
            instructionText = "You'll need somewhere to store your crops. Build a Storage Silo.",
            triggerToWaitFor = TutorialTrigger.BuiltSilo
        });
    
        steps.Add(new TutorialStep
        {
            stepId = "plant_crop",
            title = "Plant Your First Crop",
            instructionText = "Click on your crop plot and select a crop to plant. Wheat grows quickly!",
            triggerToWaitFor = TutorialTrigger.PlantedCrop
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "harvest_crop",
            title = "Harvest Your Crops",
            instructionText = "Once your crops are ready, click on the plot and select Harvest to collect them.",
            triggerToWaitFor = TutorialTrigger.HarvestedCrop
        });
    
        steps.Add(new TutorialStep
        {
            stepId = "build_chicken_coop",
            title = "Build a Chicken Coop",
            instructionText = "Let's add some animals! Build a Chicken Coop from the shop menu.",
            triggerToWaitFor = TutorialTrigger.BuiltChickenCoop
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "buy_animals",
            title = "Buy Your First Animals",
            instructionText = "Click on your Chicken Coop and use the Buy button to purchase some chickens.",
            triggerToWaitFor = TutorialTrigger.ButtonClicked
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "feed_animals",
            title = "Feed Your Animals",
            instructionText = "Animals need food! Click the coop and select Feed to give them some crops.",
            triggerToWaitFor = TutorialTrigger.ButtonClicked
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "collect_products",
            title = "Collect Animal Products",
            instructionText = "Your animals have produced goods! Click the coop and select Collect.",
            triggerToWaitFor = TutorialTrigger.ButtonClicked
        });
    
        steps.Add(new TutorialStep
        {
            stepId = "build_barracks",
            title = "Build a Barracks",
            instructionText = "To defend your farm from wolves, you'll need a Barracks. Build one now!",
            triggerToWaitFor = TutorialTrigger.ButtonClicked
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "recruit_soldiers",
            title = "Recruit Your Army",
            instructionText = "Click on your Barracks and use the Recruit button to train animal soldiers.",
            triggerToWaitFor = TutorialTrigger.ButtonClicked
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "place_flag",
            title = "Set Defense Position",
            instructionText = "Click 'Place Flag' and select where you want your soldiers to defend.",
            triggerToWaitFor = TutorialTrigger.ButtonClicked
        });
        
        steps.Add(new TutorialStep
        {
            stepId = "prepare_defense",
            title = "Prepare for Night",
            instructionText = "Night is coming! Make sure you have enough soldiers and good flag placement.",
            triggerToWaitFor = TutorialTrigger.ButtonClicked
        });
    }
}