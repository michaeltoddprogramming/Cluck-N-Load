using UnityEngine;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Build System/Structure Data")]
public class StructureData : ScriptableObject
{
    public string structureName;
    public GameObject prefab;
    public Sprite icon;
    public StructureType type; // enum for categorization
    public int cost; // Cost in resources or currency
    public int health; // Health points for the structure
}

public enum StructureType
{
    Building,
    Plant,
    EnemySpawner,
    Defense,
    Utility
}