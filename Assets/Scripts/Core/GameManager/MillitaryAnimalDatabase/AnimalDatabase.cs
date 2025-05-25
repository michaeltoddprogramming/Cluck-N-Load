using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AnimalDatabase", menuName = "Game/Animal Database", order = 1)]
public class AnimalDatabase : ScriptableObject
{
    public List<AnimalData> allAnimals;

    public AnimalData GetAnimalByType(AnimalStructure.AnimalType type)
    {
        foreach (var animal in allAnimals)
        {
            if (animal.animalType == type)
            {
                return animal;
            }
        }
        Debug.LogWarning($"Animal with type {type} not found in database.");
        return null;
    }

    public List<AnimalData> GetAllAnimals()
    {
        return allAnimals;
    }
}