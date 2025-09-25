using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TutorialStep
{
    public string stepId;
    public string title;
    [TextArea] public string instructionText;
    public GameObject uiToHighlight;
    public TutorialTrigger triggerToWaitFor;
    public UnityEvent onStepStart, onStepComplete;
    public Sprite characterSprite;
    public List<KeyCode> requiredInputs = new List<KeyCode>();
    public bool waitForAllInputs = true;
}

public enum TutorialTrigger
{
    None, GameStarted, CameraControlsUsed, ExplainMoney, TimeControlsUsed, ShopOpened,
    BuiltFarmHouse, BuiltCropPlot, PlantedCrop, HarvestedCrop, BuiltSilo,
    BuiltChickenCoop, BuiltCowPen, BuiltSheepPen, BuiltGoatPen, BuiltPigPen,
    BuiltChickenBarracks, BuiltCowBarracks, BuiltSheepBarracks, BuiltGoatBarracks, BuiltPigBarracks,
    BoughtFirstAnimals, FedFirstAnimals, CollectedFirstProducts, RecruitedFirstSoldiers, PlacedFirstFlag,
    InputDetected, SpringSeason, SummerSeason, FallSeason, WinterSeason, AnimalProductionBoosted, PricePanelOpened, PricePanelClosed,
    MelonyFound, MelonyMovementTest, MelonyZoomTest, MelonyRotateTest
}