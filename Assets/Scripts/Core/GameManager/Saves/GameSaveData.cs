using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public List<StructureSaveData> structures = new List<StructureSaveData>();
    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public int money;
    public int day;
    public int season;
    // Add more fields as needed
}

[Serializable]
public class StructureSaveData
{
    public string type;
    public Vector3 position;
    public int health;
    // Add more fields as needed
}