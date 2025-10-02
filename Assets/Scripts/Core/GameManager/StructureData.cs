using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Build System/Structure Data")]
public class StructureData : ScriptableObject
{
    [Header("Structure Info")]
    public string structureName;
    public string description;
    public GameObject prefab;
    public Sprite icon;
    public StructureType type;
    public int cost;
    public int health;
    public GameObject uiPrefab;

    [Header("Civilian stats")]
    public int costPerAnimal = 50;
    public int moneyPerProduct = 50;
    public int baseFoodRequired = 2;
    public float foodSynergyMultiplier = 1f;
    public float siloSynergyRange = 15f;
    

    [Header("Crop stats")]
    public float sunflowerBaseHarvestAmount = 10f;
    public float wheatBaseHarvestAmount = 10f;
    public float carrotsBaseHarvestAmount = 10f;
    public float cropSynergyMultiplier = 1.5f;
    public float cropSiloSynergyRange = 15f;

    [Header("Silo stats")]
    public int totalPerSilo = 100;

    [Header("Progressive Unlocks")]
    [Tooltip("Day number when this structure becomes available in the shop/game.")]
    public int unlockDay = 0; // Default: available from day 0

    [Header("Barracks Settings")]
    [Tooltip("Animal type this barracks recruits from (e.g., Chicken, Cow). Leave empty for non-barracks structures.")]
    public string targetAnimalType; // e.g., "Chicken", "Cow"
    [Tooltip("Prefabs for army animals (used by BarracksStructure).")]
    public List<GameObject> armyAnimalPrefabs; // Supports multiple animal types
    [Tooltip("Prefab for the flag (used by BarracksStructure).")]
    public GameObject flagPrefab;
    [Tooltip("Range to find target AnimalStructure (used by BarracksStructure).")]
    public float recruitmentRange = 4000f;
    [Tooltip("Maximum army animals this barracks can manage.")]
    public int maxArmyAnimals = 5;
    [Tooltip("Money cost per animal recruited.")]
    public int recruitmentCostPerAnimal = 50;
    [Tooltip("Radius for army animals to patrol around the flag.")]
    public float protectionRadius = 5f;
    public float synergyMinDist = 20f;
    public float synergyMaxDist = 30f;
}

public enum StructureType
{
    Building,
    Plant,
    EnemySpawner,
    Defense,
    Utility,
    AnimalPlot,
    Silo,
    CropPlot,
    Animal,
    Barracks,
    Decoration,
    Placed
}