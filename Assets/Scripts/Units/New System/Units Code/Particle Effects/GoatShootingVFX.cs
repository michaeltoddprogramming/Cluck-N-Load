using System.Collections;
using UnityEngine;

public class GoatShootingVFX : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;               // Where bullets spawn
    public GameObject muzzleFlashPrefab;      // Muzzle flash effect
    public GameObject impactPrefab;           // Hit effect
    public GameObject bulletTrailPrefab;      // Trail for sniper bullet

    [Header("Sniper Settings")]
    public float visualRange = 50f;           // Trail visual distance
    public float trailDuration = 0.3f;        // How long the trail takes to reach target

    public void ShootSniper(Vector3 targetPosition)
    {
        // 1. Muzzle flash
        if (muzzleFlashPrefab)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(flash, 0.2f);
        }

        // 2. Single, precise bullet trail
        if (bulletTrailPrefab)
        {
            GameObject trail = Instantiate(bulletTrailPrefab, firePoint.position, Quaternion.identity);
            StartCoroutine(MoveTrail(trail, targetPosition));
        }

        // 3. Impact at the target
        if (impactPrefab)
        {
            GameObject impact = Instantiate(impactPrefab, targetPosition, Quaternion.identity);
            Destroy(impact, 0.5f);
        }
    }

    private IEnumerator MoveTrail(GameObject trail, Vector3 target)
    {
        Vector3 startPos = trail.transform.position;
        float elapsed = 0f;

        while (elapsed < trailDuration)
        {
            trail.transform.position = Vector3.Lerp(startPos, target, elapsed / trailDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = target;
        Destroy(trail, 0.1f);
    }
}
