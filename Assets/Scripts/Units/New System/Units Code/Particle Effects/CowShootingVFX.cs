using System.Collections;
using UnityEngine;

public class CowShootingVFX : MonoBehaviour
{
    public Transform gunLeft;
    public Transform gunRight;
    public GameObject cowBulletTrailPrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject impactPrefab;

    public int bulletCount = 10; // bullets per stream
    public float spreadAngle = 5f;
    public float visualRange = 20f;

    public void ShootCow(Vector3 targetPosition)
    {
        // Muzzle flashes
        if (muzzleFlashPrefab)
        {
            Instantiate(muzzleFlashPrefab, gunLeft.position, gunLeft.rotation);
            Instantiate(muzzleFlashPrefab, gunRight.position, gunRight.rotation);
        }

        // Fire from both guns
        FireGun(gunLeft, targetPosition);
        FireGun(gunRight, targetPosition);

        // Single impact effect at center of target
        if (impactPrefab)
        {
            Vector3 center = targetPosition; // can use enemy center calculation if you want
            Instantiate(impactPrefab, center, Quaternion.identity);
        }
    }

    private void FireGun(Transform gun, Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - gun.position).normalized;

        for (int i = 0; i < bulletCount; i++)
        {
            Vector3 spreadDir = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0
            ) * direction;

            Vector3 visualTarget = gun.position + spreadDir * visualRange;

            if (cowBulletTrailPrefab)
            {
                GameObject trail = Instantiate(cowBulletTrailPrefab, gun.position, Quaternion.identity);
                StartCoroutine(MoveTrail(trail, visualTarget));
            }
        }
    }

    private IEnumerator MoveTrail(GameObject trail, Vector3 target)
    {
        float duration = 0.15f; // faster for machine gun
        Vector3 startPos = trail.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            trail.transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = target;
        Destroy(trail, 0.05f);
    }
}

