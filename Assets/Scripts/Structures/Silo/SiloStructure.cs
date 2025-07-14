using UnityEngine;

public class SiloStructure : Structure
{
    protected override void Start()
    {
        base.Start();

        if (structureData != null && structureData.type != StructureType.Silo)
        {
            }

        InventoryManager.Instance.RegisterSilo(this);
        CropStructure.UpdateAllCropSynergies();
        AnimalStructure.UpdateAllAnimalSynergies();
    }

    protected override void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.UnregisterSilo(this);
        CropStructure.UpdateAllCropSynergies();
        AnimalStructure.UpdateAllAnimalSynergies();
        base.OnDestroy();
    }
}