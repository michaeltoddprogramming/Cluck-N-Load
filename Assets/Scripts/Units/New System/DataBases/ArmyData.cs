using UnityEngine;

[CreateAssetMenu(fileName = "NewArmyData", menuName = "Units/Army Data")]
public class ArmyData : UnitData
{
    [Header("Army Type")]
    public ArmyType Type;

    [Header("Combat Stats")]
    public int Health = 100;
    public int AttackDamage = 10;
    public int AttackRange = 10;
    public float AttackCooldown = 1f;

    [Header("Audio")]
    public AudioClip AttackSound;
    public AudioClip DeathSound;
    public AudioClip HurtSound;
}