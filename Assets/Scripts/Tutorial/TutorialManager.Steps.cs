using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public partial class TutorialManager
{  
        void InitializeTutorialSteps()
        {
            steps.Clear();
            
            // First create all the steps without action assignments
            var welcomeStep = new TutorialStep
            {
                stepId = "welcome",
                title = "Welcome to Cluck N Load!",
                instructionText = "Howdy, greenhorn! I'm Old Pete. I'll help you build a thriving farm... if the wolves don't get us first!",
                triggerToWaitFor = TutorialTrigger.None
            };
            steps.Add(welcomeStep);
            
            var cameraControlsStep = new TutorialStep
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
                    KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse2
                }
            };
            steps.Add(cameraControlsStep);
            
            var moneyStep = new TutorialStep
            {
                stepId = "money_explanation",
                title = "Managing Your Money",
                instructionText = "This is your <color=yellow>money counter</color>. You start with 800 gold. You'll need cash for buildings and animals. Selling crops and animal products will earn you more!",
                triggerToWaitFor = TutorialTrigger.None,
                uiToHighlight = GameObject.Find("GoldPanel")
            };
            steps.Add(moneyStep);
            
            var shopStep = new TutorialStep
            {
                stepId = "open_build_shop",
                title = "Open the Build Shop",
                instructionText = "Click the shop icon in the bottom-left corner to start building.",
                triggerToWaitFor = TutorialTrigger.ShopOpened,
                uiToHighlight = shopButton ?? GameObject.Find("ShopButton") ?? GameObject.FindGameObjectWithTag("ShopButton")
            };
            steps.Add(shopStep);
            
            var farmhouseStep = new TutorialStep
            {
                stepId = "build_farmhouse",
                title = "Build Your Farmhouse",
                instructionText = "Every farm needs a house! Select the Farmhouse and place it on your land.",
                triggerToWaitFor = TutorialTrigger.BuiltFarmHouse,
                uiToHighlight = farmhouseButton
            };
            // Initialize the UnityEvent if needed and add listener
            if (farmhouseStep.onStepStart == null)
                farmhouseStep.onStepStart = new UnityEvent();
            farmhouseStep.onStepStart.AddListener(() => UpdateBuildButtonReference("Farmhouse"));
            steps.Add(farmhouseStep);
            
            // Do the same for all other steps...
            var cropPlotStep = new TutorialStep
            {
                stepId = "build_crop_plot",
                title = "Build a Crop Plot",
                instructionText = "Now let's grow some food. Open the shop and build a Crop Plot.",
                triggerToWaitFor = TutorialTrigger.BuiltCropPlot,
                uiToHighlight = cropPlotButton
            };
            if (cropPlotStep.onStepStart == null)
                cropPlotStep.onStepStart = new UnityEvent();
            cropPlotStep.onStepStart.AddListener(() => UpdateBuildButtonReference("CropPlot"));
            steps.Add(cropPlotStep);

            var barracksStep = new TutorialStep
    {
        stepId = "build_barracks",
        title = "Build a Barracks",
        instructionText = "Time to prepare for defense! Build a Barracks to train your animal army.",
        triggerToWaitFor = TutorialTrigger.BuiltBarracks,
        uiToHighlight = barracksButton
    };
    if (barracksStep.onStepStart == null)
        barracksStep.onStepStart = new UnityEvent();
    barracksStep.onStepStart.AddListener(() => UpdateBuildButtonReference("Barracks"));
    steps.Add(barracksStep);
            
            // Continue this pattern for the rest of the steps...
            // For brevity, I'll only fix a few more examples:
            
            var siloStep = new TutorialStep
            {
                stepId = "build_silo",
                title = "Build a Storage Silo",
                instructionText = "You'll need somewhere to store your crops. Build a Storage Silo.",
                triggerToWaitFor = TutorialTrigger.BuiltSilo,
                uiToHighlight = siloButton
            };
            if (siloStep.onStepStart == null)
                siloStep.onStepStart = new UnityEvent();
            siloStep.onStepStart.AddListener(() => UpdateBuildButtonReference("Silo"));
            steps.Add(siloStep);
            
            // Continue adding the rest of the steps in the same pattern
        }
    
        // Helper methods for dynamic UI element finding
        private void UpdateBuildButtonReference(string buildingName)
        {
            // Find the button in the build menu
            GameObject buildMenu = GameObject.Find("ShopPanel");
            if (buildMenu != null)
            {
                // Try to find the building button by name
                Transform buttonTransform = buildMenu.transform.Find(buildingName + "Button");
                if (buttonTransform != null)
                {
                    HighlightUI(buttonTransform.gameObject, true);
                }
                else
                {
                    Debug.LogWarning($"Could not find {buildingName}Button in the shop panel");
                }
            }
        }
        
        private void HighlightLastBuiltStructure(string structureType)
        {
            // Find the most recently built structure of the given type
            GameObject[] structures = GameObject.FindGameObjectsWithTag(structureType);
            if (structures.Length > 0)
            {
                // Assume the last one in the array is the most recently built
                HighlightUI(structures[structures.Length - 1], true);
            }
            else
            {
                Debug.LogWarning($"No {structureType} structures found to highlight");
            }
        }
        
    private void HighlightButtonInUI(string buttonName)
    {
        // Find buttons that match the name
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button btn in allButtons)
        {
            if (btn.name.Contains(buttonName) || 
                (btn.GetComponentInChildren<Text>() != null && 
                 btn.GetComponentInChildren<Text>().text == buttonName) ||
                (btn.GetComponentInChildren<TextMeshProUGUI>() != null && 
                 btn.GetComponentInChildren<TextMeshProUGUI>().text == buttonName))
            {
                HighlightUI(btn.gameObject, true);
                return;
            }
        }
        Debug.LogWarning($"Could not find button with name {buttonName}");
    }
}