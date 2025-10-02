using System.Collections.Generic;
using UnityEngine;

public class CivilianSpawner : MonoBehaviour
{
    [Header("Pen setup")]
    public GameObject spawningLocation;
    public GameObject floorParent;
    [SerializeField] private CivilianData data;
    private List<GameObject> spawnedAnimals = new List<GameObject>();

    public void SpawnAnimals(int totalPurchases)
    {
        int desiredTotalAnimals = 1; // default

        if (totalPurchases >= 10)
            desiredTotalAnimals = 5;
        else if (totalPurchases >= 8)
            desiredTotalAnimals = 4;
        else if (totalPurchases >= 6)
            desiredTotalAnimals = 3;
        else if (totalPurchases >= 3)
            desiredTotalAnimals = 2;
        else
            desiredTotalAnimals = 1;

        // Spawn the difference
        while (spawnedAnimals.Count < desiredTotalAnimals)
        {
            SpawnSingleAnimal();
        }
    }



    public void SpawnSingleAnimal()
    {
        GameObject newAnimal = Instantiate(data.Prefab, spawningLocation.transform.position, Quaternion.identity, transform);
        CivilianUnit unit = newAnimal.GetComponent<CivilianUnit>();
        if (unit != null)
        {
            unit.spawner = this; 
            unit.Initialize(floorParent);
        }

        spawnedAnimals.Add(newAnimal);
    }

    public void RemoveAnimal()
    {
        if (spawnedAnimals.Count == 0) return;

        GameObject lastAnimal = spawnedAnimals[spawnedAnimals.Count - 1];
        if (lastAnimal != null)
            Destroy(lastAnimal);

        spawnedAnimals.RemoveAt(spawnedAnimals.Count - 1);
    }

    public void DespawnAnimals(int totalPurchases)
    {
        // Calculate how many should exist after despawning
        int desiredTotalAnimals = 1;

        if (totalPurchases >= 10)
            desiredTotalAnimals = 5;
        else if (totalPurchases >= 8)
            desiredTotalAnimals = 4;
        else if (totalPurchases >= 6)
            desiredTotalAnimals = 3;
        else if (totalPurchases >= 3)
            desiredTotalAnimals = 2;
        else
            desiredTotalAnimals = 1;

        // Remove the difference
        while (spawnedAnimals.Count > desiredTotalAnimals)
        {
            RemoveAnimal();
        }
    }

    public void RespawnAnimals()
    {
        // save how many animals we currently have
        int count = spawnedAnimals.Count;

        // destroy all
        ClearAnimals();

        // spawn the same amount again at the spawn location
        for (int i = 0; i < count; i++)
        {
            SpawnSingleAnimal();
        }
    }

    public void RemoveSpecificAnimal(GameObject animal)
    {
        if (spawnedAnimals.Contains(animal))
        {
            spawnedAnimals.Remove(animal);   // Remove from the list
            Destroy(animal); 
        }
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
