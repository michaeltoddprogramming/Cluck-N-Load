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

    // protected override void OnDrawGizmos()
    // {
    //     base.OnDrawGizmos(); // draw base ranges
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere(transform.position, explosionRadius);
    // }

    public void Update()
    {
        base.Update();

        if (doIt)
        {
            CameraShake.Instance.ShakeAtPosition(transform.position);
            doIt = false;
        }

        List<EnemyUnit> enemies = GridController.Instance.GetEnemiesInRange(transform.position, explosionRadius);

        int count = enemies.Count;

        if (count >= 1 && lastBeepStage < 1)
        {
            PlaySound(beep1);
            lastBeepStage = 1;
        }
        else if (count >= 2 && lastBeepStage < 2)
        {
            PlaySound(beep2);
            lastBeepStage = 2;
        }
        else if (count >= 3 && lastBeepStage < 3)
        {
            PlaySound(beep3);
            lastBeepStage = 3;
        }
    }

    public override void Attack()
    {
        Debug.Log("private sheep sodiruhoiuwertiuwehiughwreiughiuerthugheruhgiuerhtgiouerhiougheriouthgiuerhgiuerhtuig");
        List<EnemyUnit> enemies = GridController.Instance.GetEnemiesInRange(transform.position, explosionRadius);

        if (enemies.Count >= minEnemiesToExplode && !hasExploded)
        {
            explode(enemies);
        }

    }

    private void explode(List<EnemyUnit> enemies)
    {
        PlaySound(data1.AttackSound);
        // CameraShake.Instance.TriggerShakeAtPosition(transform.position, 15f, 0.5f, 0.3f);
        // CameraShake.Instance.TriggerShake(0.3f, 0.5f);
        // CameraShake.Instance.Shake(1.5f, 0.4f);
        CameraShake.Instance.ShakeAtPosition(transform.position);
        foreach (var enemy in enemies)
        {
            Debug.Log("I did damageqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq");
            enemy.TakeDamage(explosionDamage);
        }

        // gameObject.SetActive(false);
        hasExploded = true;
    }


}