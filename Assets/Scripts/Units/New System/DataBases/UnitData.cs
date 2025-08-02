using UnityEngine;

public abstract class UnitData : ScriptableObject
{
    [Header("Identity")]
    public string UnitName;
    // public Sprite Icon;
    public GameObject Prefab;


    [Header("Movement")]
    public float MovementSpeed = 3f;
}