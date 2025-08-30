using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int money;
    public int sunflowerAmount;
    public int wheatAmount;
    public int carrotsAmount;
    public int day;
    public int season;
    public List<StructureSaveData> structures = new List<StructureSaveData>();
}

[Serializable]
public class StructureSaveData
{
    public string type;
    public Vector3 position;
    public int health;
    public Quaternion rotation;
    public int animalCount;
    public int maxAnimalCount;
    public string animalType;
    public string cropType;
    public bool isGrowing;
    public bool cropReady;
    public int armyAnimalCount;
    public bool isProducing;
    public bool productReady;
    public float productionProgress;
}