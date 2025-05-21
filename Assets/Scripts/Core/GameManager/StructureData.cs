using UnityEngine;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Build System/Structure Data")]
public class StructureData : ScriptableObject
{
    public string structureName;
    public GameObject prefab;
    public Sprite icon;
    public StructureType type; 
    public int cost; 
    public int health; 
    public GameObject uiPrefab;
}

public enum StructureType
{
    Building,
    Plant,
    EnemySpawner,
    Defense,
    Utility,
    AnimalPlot
}