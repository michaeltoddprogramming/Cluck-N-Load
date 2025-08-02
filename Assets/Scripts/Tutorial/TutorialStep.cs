using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class TutorialStep
{
    public string stepId;
    public string title;
    [TextArea]
    public string instructionText;

    public GameObject uiToHighlight;
    public TutorialTrigger triggerToWaitFor;

    public UnityEvent onStepStart;
    public UnityEvent onStepComplete;

    public Sprite characterSprite;
    public List<KeyCode> requiredInputs = new List<KeyCode>();
    public List<string> allowedShopItems = new List<string>();
    public bool restrictShopToAllowedItems = false;
    public bool waitForAllInputs = true;
}

public enum TutorialTrigger
{
    None,
    GameStarted,
    CameraControlsUsed,
    ExplainMoney,
    ShopOpened,
    BuiltFarmHouse,
    BuiltCropPlot,
    PlantedCrop,
    BuiltSilo,
    HarvestedCrop,
    BuiltChickenCoop,
    InputDetected,
    ButtonClicked
}