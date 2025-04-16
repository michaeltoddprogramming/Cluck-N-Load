using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StructureDatabase", menuName = "Build System/Structure Database")]
public class StructureDatabase : ScriptableObject
{
    public List<StructureData> allStructures;


    public StructureData GetStructureByName(string name)
    {
        foreach (var structure in allStructures)
        {
            if (structure.structureName == name)
            {
                return structure;
            }
        }
        Debug.LogWarning($"Structure with name {name} not found in database.");
        return null;
    }

    public List<StructureData> GetStructuresByType(StructureType type)
    {
        List<StructureData> structuresOfType = new List<StructureData>();
        foreach (var structure in allStructures)
        {
            if (structure.type == type)
            {
                structuresOfType.Add(structure);
            }
        }
        return structuresOfType;
    }
    public List<StructureData> GetAllStructures()
    {
        return allStructures;
    }
}    