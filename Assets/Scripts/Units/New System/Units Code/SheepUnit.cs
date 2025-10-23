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

    [Header("Explosion Knockback")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackDuration = 1f;

    [Header("Sheep Meshes")]
    [SerializeField] private Mesh meshStage1;
    [SerializeField] private Mesh meshStage2;
    [SerializeField] private Mesh meshStage3;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private int currentMeshStage = 1;

    [Header("Radius Indicator")]
    [SerializeField] private Sprite radiusIndicatorSprite;
    [SerializeField] private bool showRadiusIndicator = true;
    [SerializeField] private float indicatorAlpha = 0.5f;
    [SerializeField] private float indicatorSizeMultiplier = 1.0f;
    private GameObject radiusIndicatorObject;
    private SpriteRenderer radiusIndicatorRenderer;
    private bool ignoreSounds = false;

    private int lastBeepStage = 0;

    public bool doIt = false;
    protected override void Awake()
    {
        base.Awake();
        // Find SkinnedMeshRenderer in children
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            if (meshStage1 != null)
            {
                skinnedMeshRenderer.sharedMesh = meshStage1;
                currentMeshStage = 1;
            }
        }

        // Initialize radius indicator
        SetupRadiusIndicator();
    }

    private void Start()
    {
        explosionRadius = data1.AttackRange;
        explosionDamage = data1.AttackDamage;

        // Update radius indicator scale based on explosion radius
        UpdateRadiusIndicatorScale();

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

    public new void Update()
    {
        base.Update();

        // Mesh swap logic based on health percentage
        if (skinnedMeshRenderer != null && data1 != null)
        {
            float healthPercent = (float)CurrentHealth / (float)data1.Health;
            if (healthPercent <= 0.4f && currentMeshStage < 3 && meshStage3 != null)
            {
                skinnedMeshRenderer.sharedMesh = meshStage3;
                currentMeshStage = 3;
            }
            else if (healthPercent <= 0.7f && currentMeshStage < 2 && meshStage2 != null)
            {
                skinnedMeshRenderer.sharedMesh = meshStage2;
                currentMeshStage = 2;
            }
            else if (healthPercent > 0.7f && currentMeshStage != 1 && meshStage1 != null)
            {
                skinnedMeshRenderer.sharedMesh = meshStage1;
                currentMeshStage = 1;
            }
        }

        List<EnemyUnit> enemies = GridController.Instance.GetEnemiesInRangeSheep(transform.position, explosionRadius);

        int count = enemies.Count;
        if (GetTimeOfDay() == false)  // If it's night
        {
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

        if (enemies.Count >= minEnemiesToExplode)
        {
            explode(enemies);
        }
    }

    private void SetupRadiusIndicator()
    {
        if (radiusIndicatorSprite != null && showRadiusIndicator)
        {
            // Create a new GameObject for the radius indicator
            radiusIndicatorObject = new GameObject("RadiusIndicator");
            radiusIndicatorObject.transform.SetParent(transform);
            radiusIndicatorObject.transform.localPosition = Vector3.zero;
            radiusIndicatorObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face up

            // Add SpriteRenderer component
            radiusIndicatorRenderer = radiusIndicatorObject.AddComponent<SpriteRenderer>();
            radiusIndicatorRenderer.sprite = radiusIndicatorSprite;
            radiusIndicatorRenderer.sortingOrder = -1; // Render behind other objects

            // Set initial alpha
            Color color = radiusIndicatorRenderer.color;
            color.a = indicatorAlpha;
            radiusIndicatorRenderer.color = color;

            // Start with the indicator hidden by default
            radiusIndicatorObject.SetActive(false);
        }
    }

    private void UpdateRadiusIndicatorScale()
    {
        if (radiusIndicatorObject != null && radiusIndicatorRenderer != null)
        {
            // Calculate scale based on explosion radius and size multiplier
            // Assuming the sprite should cover the full radius diameter
            float diameter = explosionRadius * 2f * indicatorSizeMultiplier;
            radiusIndicatorObject.transform.localScale = Vector3.one * diameter;

            // Only show if both showRadiusIndicator is true AND it's been set to visible
            // (The SetRadiusIndicatorVisibility method will be called by the barracks)
            if (!showRadiusIndicator)
            {
                radiusIndicatorObject.SetActive(false);
            }
        }
    }

    public void SetRadiusIndicatorVisibility(bool visible)
    {
        if (radiusIndicatorObject != null)
        {
            radiusIndicatorObject.SetActive(visible);
        }
    }

    public void SetRadiusIndicatorAlpha(float alpha)
    {
        if (radiusIndicatorRenderer != null)
        {
            Color color = radiusIndicatorRenderer.color;
            color.a = alpha;
            radiusIndicatorRenderer.color = color;
        }
    }

    private void OnDestroy()
    {
        if (radiusIndicatorObject != null)
        {
            DestroyImmediate(radiusIndicatorObject);
        }
    }

    private void OnValidate()
    {
        // Update the radius indicator when values change in the inspector
        if (Application.isPlaying)
        {
            UpdateRadiusIndicatorScale();

            if (radiusIndicatorRenderer != null)
            {
                Color color = radiusIndicatorRenderer.color;
                color.a = indicatorAlpha;
                radiusIndicatorRenderer.color = color;
            }
        }
    }

    private void explode(List<EnemyUnit> enemies)
    {
        SheepExplodingVFX vfx = GetComponent<SheepExplodingVFX>();
        if (vfx != null)
            vfx.Explode(transform.position);
        PlaySound(data1.AttackSound, 'a');

        // Ensure CameraShake is working
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeAtPosition(transform.position);
        }
        else
        {
            Debug.LogWarning("CameraShake.Instance is null!");
        }

        // Apply knockback BEFORE taking self-damage to ensure it completes
        foreach (var enemy in enemies)
        {
            // Damage the enemy
            enemy.TakeDamage(explosionDamage);

            // Apply knockback immediately
            ApplyKnockback(enemy);
        }

        // Small delay before self-damage to ensure knockback forces are applied
        StartCoroutine(DelayedSelfDamage());
    }

    private System.Collections.IEnumerator DelayedSelfDamage()
    {
        // Wait a tiny bit to ensure knockback is applied
        yield return new WaitForFixedUpdate();

        // Damage the sheep AFTER knockback is applied
        int selfDamage = Mathf.CeilToInt(data1.Health * 0.34f);
        ignoreSounds = true;
        TakeDamage(selfDamage);
    }

    private void ApplyKnockback(EnemyUnit enemy)
    {
        if (enemy == null) return;

        // Calculate knockback direction (away from sheep)
        Vector3 knockbackDirection = (enemy.transform.position - transform.position).normalized;

        // Ensure knockback has some upward force to make it more visible
        knockbackDirection.y = Mathf.Max(knockbackDirection.y, 0.3f);
        knockbackDirection = knockbackDirection.normalized;

        // Apply knockback force
        Vector3 knockbackVelocity = knockbackDirection * knockbackForce;

        // Try to find Rigidbody on the enemy
        Rigidbody enemyRigidbody = enemy.GetComponent<Rigidbody>();
        if (enemyRigidbody != null)
        {
            // Apply impulse force for immediate knockback
            enemyRigidbody.AddForce(knockbackVelocity, ForceMode.Impulse);
        }
        else
        {
            // If no Rigidbody, apply immediate position-based knockback for the final explosion
            // This ensures knockback works even if sheep dies immediately after
            StartCoroutine(KnockbackWithoutRigidbody(enemy, knockbackVelocity));
        }
    }

    private System.Collections.IEnumerator KnockbackWithoutRigidbody(EnemyUnit enemy, Vector3 initialVelocity)
    {
        if (enemy == null) yield break;

        float timeElapsed = 0f;
        Vector3 velocity = initialVelocity;

        while (timeElapsed < knockbackDuration)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) yield break;

            // Apply gravity-like deceleration
            velocity.y -= 9.81f * Time.deltaTime;
            velocity *= (1f - Time.deltaTime * 2f); // General deceleration

            // Move the enemy
            enemy.transform.position += velocity * Time.deltaTime;

            timeElapsed += Time.deltaTime;
            yield return null;
        }
    }

    public override void TakeDamage(int damage)
    {
        if (currHealth <= 0 || currHealth - damage <= 0)
        {
            if (ignoreSounds)
            {
                currHealth = 0;
                UpdateHealthBar();
                barracks?.OnAnimalDied(this);
                handleDie();
            }
            else
            {
                PlaySound(data1.DeathSound, 'd');
                currHealth = 0;
                UpdateHealthBar();
                barracks?.OnAnimalDied(this);
                handleDie();
            }
        }
        else
        {
            if (ignoreSounds)
            {
                currHealth -= damage;
                UpdateHealthBar();
            }
            else
            {
                PlaySound(data1.HurtSound, 'h');
                currHealth -= damage;
                UpdateHealthBar();
            }
        }
    }

}