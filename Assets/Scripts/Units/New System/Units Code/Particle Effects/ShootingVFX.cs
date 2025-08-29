using System.Collections;
using UnityEngine;

public class ShootingVFX : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;               // The point bullets appear from
    public GameObject muzzleFlashPrefab;      // Your muzzle flash prefab
    public GameObject impactPrefab;           // Your impact effect prefab
    public GameObject bulletTrailPrefab;      // Your bullet trail prefab

    public int bulletCount = 6;
    public float spreadAngle = 10f;
    public float visualRange = 20f;  // how far the trails go

    [Header("Shooting Settings")]
    public float range = 20f;                 // Max distance for the bullet

    // Call this function to shoot at a target
    // public void Shoot(Vector3 targetPosition)
    // {
    //     // 1. Muzzle Flash
    //     if (muzzleFlashPrefab)
    //     {
    //         GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
    //         Destroy(flash, 0.2f);
    //     }

    //     // 2. Raycast to detect hit
    //     if (Physics.Raycast(firePoint.position, (targetPosition - firePoint.position).normalized, out RaycastHit hit, range))
    //     {
    //         // Impact Effect
    //         if (impactPrefab && hit.collider != null)
    //         {
    //             // Get the enemy transform
    //             Transform enemy = hit.collider.transform;

    //             // Calculate center position
    //             // Assuming your enemy height is roughly equal to its collider height
    //             float enemyHeight = 1.5f; // replace with actual height of your prefab
    //             Vector3 centerPos = enemy.position + Vector3.up * (enemyHeight / 2f);

    //             // Spawn the hit effect
    //             GameObject impact = Instantiate(impactPrefab, centerPos, Quaternion.identity);


    //             // Transform enemyCenter = hit.collider.transform; // the enemy's main transform
    //             // GameObject impact = Instantiate(impactPrefab, enemyCenter.position, Quaternion.identity);
    //             // GameObject impact = Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
    //             // Destroy(impact, 0.5f);
    //         }

    //         // Bullet Trail
    //         if (bulletTrailPrefab)
    //         {
    //             GameObject trail = Instantiate(bulletTrailPrefab, firePoint.position, Quaternion.identity);
    //             StartCoroutine(MoveTrail(trail, targetPosition));
    //         }
    //     }
    // }


    // public void Shoot(Vector3 targetPosition)
    // {
    //     // 1. Muzzle Flash
    //     if (muzzleFlashPrefab)
    //     {
    //         GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
    //         Destroy(flash, 0.2f);
    //     }

    //     Vector3 direction = (targetPosition - firePoint.position).normalized;

    //     // Fire multiple visual bullets
    //     for (int i = 0; i < bulletCount; i++)
    //     {
    //         // Random spread for each trail
    //         Vector3 spreadDir = Quaternion.Euler(
    //             Random.Range(-spreadAngle, spreadAngle),
    //             Random.Range(-spreadAngle, spreadAngle),
    //             0
    //         ) * direction;

    //         // Calculate the visual target position
    //         Vector3 visualTarget = firePoint.position + spreadDir * visualRange;

    //         // Spawn the trail
    //         if (bulletTrailPrefab)
    //         {
    //             GameObject trail = Instantiate(bulletTrailPrefab, firePoint.position, Quaternion.identity);
    //             StartCoroutine(MoveTrail(trail, visualTarget));
    //         }
    //     }

    //     // 2. Hit effect at target position (always plays once)
    //     if (impactPrefab)
    //     {
    //         GameObject impact = Instantiate(impactPrefab, targetPosition, Quaternion.identity);
    //         Destroy(impact, 0.5f); // adjust lifetime
    //     }
    // }

    // // Moves the bullet trail from firePoint to target
    // private IEnumerator MoveTrail(GameObject trail, Vector3 target)
    // {
    //     float duration = 0.2f;
    //     Vector3 startPos = trail.transform.position;
    //     float elapsed = 0f;

    //     while (elapsed < duration)
    //     {
    //         trail.transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }

    //     trail.transform.position = target;
    //     Destroy(trail, 0.1f);
    // }

    public void Shoot(Vector3 targetPosition)
    {
        // 1. Muzzle Flash
        if (muzzleFlashPrefab)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.2f);
        }

        Vector3 direction = (targetPosition - firePoint.position).normalized;

        // Fire multiple visual bullets toward target
        for (int i = 0; i < bulletCount; i++)
        {
            // Slightly spread each trail around the target direction
            Vector3 spreadDir = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0
            ) * direction;

            Vector3 visualTarget = firePoint.position + spreadDir * (targetPosition - firePoint.position).magnitude;

            // Spawn the trail
            if (bulletTrailPrefab)
            {
                GameObject trail = Instantiate(bulletTrailPrefab, firePoint.position, Quaternion.identity);
                StartCoroutine(MoveTrail(trail, visualTarget));
            }
        }

        // 2. Hit effect at the target position (always plays once)
        if (impactPrefab)
        {
            GameObject impact = Instantiate(impactPrefab, targetPosition, Quaternion.identity);
            Destroy(impact, 0.5f); // adjust lifetime
        }
    }

    // Moves trail smoothly from firePoint to target
    private IEnumerator MoveTrail(GameObject trail, Vector3 target)
    {
        float duration = 0.2f;
        Vector3 startPos = trail.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            trail.transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = target;
        Destroy(trail, 0.1f);
    }


}
