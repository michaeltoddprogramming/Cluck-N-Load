using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Units/Enemy Data")]
public class EnemyData : UnitData
{
    [Header("Enemy Type")]
    public EnemyType Type;

    [Header("Attack Type")]
    public AttType AttType;

    [Header("Combat Stats")]
    public int Health = 100;
    public int AttackDamage = 10;
    public int AttackRange = 10;
    public float AttackCooldown = 1f;

    [Header("Audio")]
    public AudioClip AttackSound;
    public AudioClip DeathSound;
    public AudioClip HurtSound;

    [Header("Enemy Spawn Settings")]
    public int maxSpawnAmount = 5;
    public int minSpawnAmount = 1;
    public int nightlySpawnMultiplier = 2;
    public float seasonSpawnMultiplier = 1.2f;

}