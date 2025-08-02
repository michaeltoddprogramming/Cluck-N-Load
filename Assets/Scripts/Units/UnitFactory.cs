// using UnityEngine;

// public static class UnitFactory 
// {
//     public static Unit CreateUnit(UnitData data, Vector3 position, Quaternion rotation = default) 
//     {
//         if (data == null || data.Prefab == null) 
//         {
//             Debug.LogError("Cannot create unit: Invalid unit data or missing prefab");
//             return null;
//         }
        
//         GameObject instance = Object.Instantiate(data.Prefab, position, rotation);
//         instance.name = $"{data.UnitName}_{Time.frameCount}";
        
//         Unit unit = instance.GetComponent<Unit>();
//         if (unit == null) 
//         {
//             Debug.LogError($"Prefab {data.Prefab.name} does not have a Unit component");
//             return null;
//         }
        
//         // Set data through field or property
//         var dataField = unit.GetType().GetField("_unitData", 
//             System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
//         if (dataField != null) dataField.SetValue(unit, data);
        
//         return unit;
//     }
// }