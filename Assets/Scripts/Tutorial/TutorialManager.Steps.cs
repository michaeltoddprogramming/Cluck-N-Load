using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    void InitializeTutorialSteps()
    {
        steps.Clear();

        steps.Add(new TutorialStep
        {
            title = "Old Pete needs your help!",
            instructionText = "Howdy, greenhorn! I'm Old Pete...",
            triggerToWaitFor = TutorialTrigger.None
        });

        steps.Add(new TutorialStep
        {
            title = "Look Around Your Farm",
            instructionText = "Use <color=#00FF00>WASD</color> to move the camera.\n\n" +
                            "Press <color=#00FF00>Q</color> and <color=#00FF00>E</color> to rotate.\n\n" +
                            "<color=#00FF00>Middle-click</color> and drag to also rotate.\n\n" +
                            "Use your <color=#00FF00>Mouse Wheel</color> or press <color=#00FF00>1/2</color> to zoom.",
            triggerToWaitFor = TutorialTrigger.InputDetected,
            requiredInputs = new List<KeyCode> { 
                KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, 
                KeyCode.Q, KeyCode.E,
                KeyCode.Alpha1, KeyCode.Alpha2,
                KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse2
            }
        });

        steps.Add(new TutorialStep
        {
            title = "Open the Build Shop",
            instructionText = "Click the shop icon...",
            triggerToWaitFor = TutorialTrigger.ShopOpened,
            uiToHighlight = GameObject.FindGameObjectWithTag("ShopButton")
        });
    }
}