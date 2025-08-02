using UnityEngine;

public enum UnitType 
{
    Civilian,
    Military,
    Hostile
}

// Define subtypes for hostile units
public enum HostileSubtype
{
    Regular,
    Fast,
    Strong
}

[CreateAssetMenu(fileName = "NewUnit", menuName = "Units/Unit Data")]
public class UnitData1 : ScriptableObject 
{
    [Header("Identity")]
    public string UnitName;
    public UnitType Type;
    // Only used when Type is Hostile
    public HostileSubtype HostileType;
    public GameObject Prefab;
    public Sprite Icon;
    
    [Header("Stats")]
    public int Health = 100; 
    public int AttackPower = 5;
    public float AttackRange = 1f;
    public float AttackCooldown = 1f;
    public float MovementSpeed = 3f;
    
    [Header("Economy")]
    public int Cost;
    
    [Header("Audio")]
    public AudioClip AttackSound;
    public AudioClip DeathSound;
    public AudioClip MoveSound;
    public AudioClip HurtSound;
}