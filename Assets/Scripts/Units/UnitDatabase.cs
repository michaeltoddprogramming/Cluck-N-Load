// using UnityEngine;
// using System.Collections.Generic;

// [CreateAssetMenu(fileName = "UnitDatabase", menuName = "Units/Unit Database")]
// public class UnitDatabase : ScriptableObject 
// {
//     public List<UnitData> AllUnits = new List<UnitData>();
    
//     public List<UnitData> GetUnitsOfType(UnitType type) 
//     {
//         return AllUnits.FindAll(u => u.Type == type);
//     }
    
//     public UnitData GetRandomUnitOfType(UnitType type) 
//     {
//         List<UnitData> units = GetUnitsOfType(type);
//         if (units.Count == 0) return null;
        
//         return units[Random.Range(0, units.Count)];
//     }
    
//     public UnitData GetUnitByName(string name) 
//     {
//         return AllUnits.Find(u => u.UnitName == name);
//     }
// }