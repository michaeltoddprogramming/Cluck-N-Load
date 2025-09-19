using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SheepUnit : ArmyUnit
{
    [SerializeField] private ArmyData data1;
    [SerializeField] private int explosionRadius = 3;
    [SerializeField] private int explosionDamage = 50;
    [SerializeField] private int minEnemiesToExplode = 30;
    [SerializeField] public AudioClip beep1;
    [SerializeField] public AudioClip beep2;
    [SerializeField] public AudioClip beep3;

    private int lastBeepStage = 0;

    private bool hasExploded = false;
    public bool doIt = false;
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        explosionRadius = data1.AttackRange;
        explosionDamage = data1.AttackDamage;
        // Draw SheepUnit explosion range in yellow
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos(); // Draw the base unit gizmos (attack range, roam radius, etc.)

#if UNITY_EDITOR
    // Draw explosion range for the sheep
    Gizmos.color = Color.yellow; // Choose a color that stands out
    Gizmos.DrawWireSphere(transform.position, explosionRadius);
      // Draw lines to all enemies in range
    if (GridController.Instance != null)
    {
        List<EnemyUnit> enemies = GridController.Instance.GetEnemiesInRangeSheep(transform.position, explosionRadius);
        Gizmos.color = Color.red;
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                Gizmos.DrawLine(transform.position, enemy.transform.position);
        }
    }
    
#endif
    }

    public void Update()
    {
        base.Update();

        List<EnemyUnit> enemies = GridController.Instance.GetEnemiesInRangeSheep(transform.position, explosionRadius);

        int count = enemies.Count;
        if (GetTimeOfDay() == false)  // If it's night
        {
            hasExploded = false;
            count = 0;
            lastBeepStage = 0;
            // Optional: Disable sheep activity at night
            // e.g., Stop any ongoing actions or animations
        }

        if (count >= 1 && lastBeepStage < 1)
        {
            PlaySound(beep1, 's');
            lastBeepStage = 1;
        }
        else if (count >= 2 && lastBeepStage < 2)
        {
            PlaySound(beep2, 's');
            lastBeepStage = 2;
        }
        else if (count >= 3 && lastBeepStage < 3)
        {
            PlaySound(beep3, 's');
            lastBeepStage = 3;
        }
    }

    public override void Attack()
    {
        // if (fromBase) return;
        // Debug.Log("private sheep sodiruhoiuwertiuwehiughwreiughiuerthugheruhgiuerhtgiouerhiougheriouthgiuerhgiuerhtuig");
        List<EnemyUnit> enemies = GridController.Instance.GetEnemiesInRangeSheep(transform.position, explosionRadius);

        if (enemies.Count >= minEnemiesToExplode && !hasExploded)
        {
            explode(enemies);
        }
    }

    private void explode(List<EnemyUnit> enemies)
    {
        SheepExplodingVFX vfx = GetComponent<SheepExplodingVFX>();
        if (vfx != null)
            vfx.Explode(transform.position);
        PlaySound(data1.AttackSound, 'a');
        // CameraShake.Instance.TriggerShakeAtPosition(transform.position, 15f, 0.5f, 0.3f);
        // CameraShake.Instance.TriggerShake(0.3f, 0.5f);
        // CameraShake.Instance.Shake(1.5f, 0.4f);
        CameraShake.Instance.ShakeAtPosition(transform.position);
        foreach (var enemy in enemies)
        {
            // Debug.Log("I did damageqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq");
            enemy.TakeDamage(explosionDamage);
        }

        // gameObject.SetActive(false);
        hasExploded = true;
        Die();
    }
}