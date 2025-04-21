using UnityEngine;

public class Unit : MonoBehaviour 
{
    // Basic identity and references only
    [SerializeField] private UnitData _unitData;
    public UnitData Data => _unitData;
    
    // Components - reference them, don't inherit
    [HideInInspector] public UnitHealth Health { get; private set; }
    [HideInInspector] public UnitMovement Movement { get; private set; }
    [HideInInspector] public UnitCombat Combat { get; private set; }

    private void Awake()
    {
        // Get required components
        Health = GetComponent<UnitHealth>();
        Movement = GetComponent<UnitMovement>();
        Combat = GetComponent<UnitCombat>();
        
        // Register with manager
        UnitRegistry.Register(this);
    }

    private void OnDestroy() 
    {
        UnitRegistry.Unregister(this);
    }
}