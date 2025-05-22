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

        InventoryManager.Instance.RegisterSilo(this);
        CropStructure.UpdateAllCropSynergies();
        AnimalStructure.UpdateAllAnimalSynergies();
    }


    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.UnregisterSilo(this);
        CropStructure.UpdateAllCropSynergies();
        AnimalStructure.UpdateAllAnimalSynergies();
    }
}