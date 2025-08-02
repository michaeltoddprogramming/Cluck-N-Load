using System.Collections.Generic;
using UnityEngine;

public class ArmyUnit : BaseUnit
{
    [SerializeField] private ArmyData data;
    private int currHealth;
    private float lastAttackTime = 0f;
    private EnemyUnit currentTarget;

    private BarracksStructure barracks;
    private Vector3 guardPosition;
    private bool isNightTime;

    private Vector3 targetAttackPoint;
    private Transform target;

    private Vector3 targetPosition;
private bool isMoving = false;


    protected override void Awake()
    {
        base.Awake();
        currHealth = data.Health;
    }

    private void Update()
    {
        if (isMoving)
        {
            MoveToTargetPosition();
        }
    }

    protected override UnitData GetData() => data;

    public void Attack()
    {
        //apply cooldown
        if (Time.time < lastAttackTime + data.AttackCooldown)
        {
            return;
        }

        if (currentTarget == null || currentTarget.IsDead())
        {
            var enemies = GetNearbyEnemies();
            if (enemies.Count > 0)
                currentTarget = enemies[0]; // TODO: Add smarter target selection later------------------------------------------------------------------
            else
                return; // No enemies to attack
        }

        lastAttackTime = Time.time;
        PlaySound(data.AttackSound);

        currentTarget.TakeDamage(data.AttackDamage);
    }

    public List<EnemyUnit> GetNearbyEnemies()
    {
        return GridController.Instance.GetEnemiesInRange(transform.position, data.AttackRange);


        // List<EnemyUnit> nearbyUnits = new List<EnemyUnit>();
        // Collider[] colliders = Physics.OverlapSphere(transform.position, range);

        // foreach (Collider collider in colliders)
        // {
        //     EnemyUnit unit = collider.GetComponent<EnemyUnit>();
        //     if (unit != null && unit != this)
        //     {
        //         nearbyUnits.Add(unit);
        //     }
        // }

        // return nearbyUnits;
    }

    public void TakeDamage(int damage)
    {
        if (currHealth <= 0 || currHealth - damage <= 0)
        {
            PlaySound(data.DeathSound);
            Die();
        }
        else
        {
            PlaySound(data.HurtSound);
            currHealth -= damage;
        }
    }

    public bool IsDead()
    {
        return currHealth <= 0;
    }

    // protected override UnitData GetData()
    // {
    //     return data;
    // }

    public void SetBarracks(BarracksStructure source)
    {
        barracks = source;
    }

    public void SetGuardPosition(Vector3 position, float radius)
    {
        guardPosition = position;
    }

    public void SetTimeOfDay(bool isNight)
    {
        isNightTime = isNight;
    }

    public void MoveToFlag()
    {
        if (!isNightTime) return;

        targetPosition = guardPosition;
        isMoving = true;
    }

    private void MoveToTargetPosition()
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;

        if (direction.magnitude < 0.2f)
        {
            isMoving = false;
            return;
        }

        Vector3 moveDir = direction.normalized;
        transform.position += moveDir * data.MovementSpeed * Time.deltaTime;

        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            float rotationSpeed = 5f; // fallback
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, data.rotationSpeed * Time.deltaTime);
        }
    }
}