using System.Collections.Generic;
using UnityEngine;

public class CivilianSpawner : MonoBehaviour
{
    [Header("Pen setup")]
    public GameObject spawningLocation;
    public GameObject floorParent;
    [SerializeField] private CivilianData data;
    private List<GameObject> spawnedAnimals = new List<GameObject>();

    public void SpawnAnimals(int userPurchaseCount)
    {
        if (userPurchaseCount == 1)
        {
            SpawnSingleAnimal();
        }
        else if (userPurchaseCount % 2 == 0)
        {
            SpawnSingleAnimal();
        }
        return;
    }

    private void SpawnSingleAnimal()
    {
        GameObject newAnimal = Instantiate(data.Prefab, spawningLocation.transform.position, Quaternion.identity, transform);
        CivilianUnit unit = newAnimal.GetComponent<CivilianUnit>();
        if (unit != null)
            unit.Initialize(floorParent.transform);

        spawnedAnimals.Add(newAnimal);
    }

    private void RemoveAnimal()
    {
        if (spawnedAnimals.Count == 0) return;

        GameObject lastAnimal = spawnedAnimals[spawnedAnimals.Count - 1];
        if (lastAnimal != null)
            Destroy(lastAnimal);

        spawnedAnimals.RemoveAt(spawnedAnimals.Count - 1);
    }

    public void ClearAnimals()
    {
        foreach (var a in spawnedAnimals)
        {
            if (a != null)
                Destroy(a);
        }
        spawnedAnimals.Clear();
    }
}
