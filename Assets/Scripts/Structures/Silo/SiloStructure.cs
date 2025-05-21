using UnityEngine;

public class SiloStructure : Structure
{
    protected override void Start()
    {
        base.Start();

        if (structureData != null && structureData.type != StructureType.Silo)
        {
            Debug.LogWarning($"{gameObject.name} has SiloStructure script but StructureData.type is {structureData.type}, expected Silo.");
        }
    }
}