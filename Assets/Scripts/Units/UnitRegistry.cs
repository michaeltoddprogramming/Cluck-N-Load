using System;
using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry 
{
    private static List<Unit> _allUnits = new List<Unit>();
    public static IReadOnlyList<Unit> AllUnits => _allUnits;
    
    // Events instead of direct references
    public static event Action<Unit> UnitRegistered;
    public static event Action<Unit> UnitUnregistered;
    
    public static void Register(Unit unit) 
    {
        if (!_allUnits.Contains(unit)) 
        {
            _allUnits.Add(unit);
            UnitRegistered?.Invoke(unit);
        }
    }
    
    public static void Unregister(Unit unit) 
    {
        if (_allUnits.Remove(unit)) 
        {
            UnitUnregistered?.Invoke(unit);
        }
    }
    
    public static List<Unit> GetUnitsOfType(UnitType type) =>
        _allUnits.FindAll(u => u.Data.Type == type);
    
    public static Unit GetClosestUnitOfType(Vector3 position, float maxDistance, UnitType type) 
    {
        Unit closest = null;
        float closestDist = maxDistance;
        
        foreach (Unit unit in _allUnits) 
        {
            if (unit.Data.Type != type) continue;
            
            float dist = Vector3.Distance(position, unit.transform.position);
            if (dist < closestDist) 
            {
                closestDist = dist;
                closest = unit;
            }
        }
        
        return closest;
    }
}