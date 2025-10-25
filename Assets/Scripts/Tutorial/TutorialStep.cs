using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TutorialStep
{
    [Header("Basic Step Info")]
    public string stepId;
    public string title;
    [TextArea] public string instructionText;
    
    [Header("Pete Context System")]
    public PeteContext peteContext = PeteContext.Auto;
    public Vector3 peteWorldPosition = Vector3.zero;
    public Transform peteWorldTarget; // Pete will position himself near this world object
    public RectTransform peteUITarget; // Pete will position himself near this UI element
    public bool peteLooksAtTarget = true;
    public PeteEmotion peteEmotion = PeteEmotion.Neutral;
    
    [Header("Legacy System (Backward Compatibility)")]
    public GameObject uiToHighlight;
    public TutorialTrigger triggerToWaitFor;
    public UnityEvent onStepStart, onStepComplete;
    public Sprite characterSprite;
    public List<KeyCode> requiredInputs = new List<KeyCode>();
    public bool waitForAllInputs = true;
}

public enum PeteContext
{
    Auto,           // System automatically determines best context
    WorldGuide,     // Pete appears in 3D world near target
    UIHelper,       // Pete appears in screen space near UI
    CornerBuddy,    // Pete appears in small corner window for complex UI
    Hidden          // Pete is hidden for this step
}

public enum PeteEmotion
{
    Neutral,
    Excited,
    Worried,
    Thinking,
    Celebrating,
    Pointing
}

public enum TutorialTrigger
{
    None, GameStarted, CameraControlsUsed, ExplainMoney, TimeControlsUsed, ShopOpened,
    BuiltFarmHouse, BuiltCropPlot, PlantedCrop, HarvestedCrop, BuiltSilo,
    BuiltChickenCoop, BuiltCowPen, BuiltSheepPen, BuiltGoatPen, BuiltPigPen,
    BuiltChickenBarracks, BuiltCowBarracks, BuiltSheepBarracks, BuiltGoatBarracks, BuiltPigBarracks,
    BoughtFirstAnimals, Bought5CivilianAnimals, FedFirstAnimals, CollectedFirstProducts, RecruitedFirstSoldiers, Recruited3ArmyAnimals, BuiltFirstWall, BuiltFirstHayBale, Built10HayBales, PlacedFirstFlag,
    InputDetected, SpringSeason, SummerSeason, FallSeason, WinterSeason, AnimalProductionBoosted, PricePanelOpened, PricePanelClosed,
    MelonyFound, MelonyMovementTest, MelonyZoomTest, MelonyRotateTest, BarracksUIOpened
}