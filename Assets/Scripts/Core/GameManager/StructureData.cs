using UnityEngine;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Build System/Structure Data")]
public class StructureData : ScriptableObject
{
    public string structureName;
    public GameObject prefab;
    public Sprite icon;
    public StructureType type; // enum for categorization
}

public enum StructureType
{
    Building,
    Plant,
    EnemySpawner,
    Defense,
    Utility
}