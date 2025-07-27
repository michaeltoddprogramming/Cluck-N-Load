using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TutorialStep
{
    public string title;
    [TextArea]
    public string instructionText;

    public GameObject uiToHighlight;

    public TutorialTrigger triggerToWaitFor;

    public UnityEvent onStepStart;
    public UnityEvent onStepComplete;

    public Sprite characterSprite;
}

public enum TutorialTrigger
    {
        GameStarted,
        ShopOpened,
        BuiltFarmHouse,
        BuiltCropPlot,
        PlantedCrop,
        BuiltSilo,
        HarvestedCrop,
        BuiltChickenCoop
    }
