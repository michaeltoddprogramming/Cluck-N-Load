// using System.Collections;
// using UnityEngine;

// public class CowShootingVFX : MonoBehaviour
// {
//     public Transform gunLeft;
//     public Transform gunRight;
//     public GameObject cowBulletTrailPrefab;
//     public GameObject muzzleFlashPrefab;
//     public GameObject impactPrefab;

//     public int bulletCount = 1;
//     public float spreadAngle = 5f;
//     public float visualRange = 20f;
//     public float attackDuration = 2.5f;   // attack length
//     public float fireRate = 0.1f;         // time between volleys

//     public void ShootCow(Vector3 targetPosition)
//     {
//         StartCoroutine(ShootRoutine(targetPosition));
//     }

//     private IEnumerator ShootRoutine(Vector3 targetPosition)
//     {
//         float elapsed = 0f;

//         // spawn muzzle flashes once at the start
//         if (muzzleFlashPrefab)
//         {
//             GameObject flashLeft = Instantiate(muzzleFlashPrefab, gunLeft.position, gunLeft.rotation);
//             GameObject flashRight = Instantiate(muzzleFlashPrefab, gunRight.position, gunRight.rotation);
//             Destroy(flashLeft, attackDuration);
//             Destroy(flashRight, attackDuration);
//         }

//         // keep shooting until attack duration is finished
//         while (elapsed < attackDuration)
//         {
//             FireGun(gunLeft, targetPosition);
//             FireGun(gunRight, targetPosition);

//             // optional impact per volley
//             if (impactPrefab)
//             {
//                 Vector3 center = targetPosition;
//                 GameObject impact = Instantiate(impactPrefab, center, Quaternion.identity);
//                 Destroy(impact, 0.5f);
//             }

//             yield return new WaitForSeconds(fireRate);
//             elapsed += fireRate;
//         }
//     }

//     private void FireGun(Transform gun, Vector3 targetPosition)
//     {
//         Vector3 direction = (targetPosition - gun.position).normalized;

//         for (int i = 0; i < bulletCount; i++)
//         {
//             Vector3 spreadDir = Quaternion.Euler(
//                 Random.Range(-spreadAngle, spreadAngle),
//                 Random.Range(-spreadAngle, spreadAngle),
//                 0
//             ) * direction;

//             Vector3 visualTarget = gun.position + spreadDir * visualRange;

//             if (cowBulletTrailPrefab)
//             {
//                 GameObject trail = Instantiate(cowBulletTrailPrefab, gun.position, Quaternion.identity);
//                 StartCoroutine(MoveTrail(trail, visualTarget));
//             }
//         }
//     }

//     private IEnumerator MoveTrail(GameObject trail, Vector3 target)
//     {
//         float duration = 0.15f;
//         Vector3 startPos = trail.transform.position;
//         float elapsed = 0f;

//         while (elapsed < duration)
//         {
//             trail.transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
//             elapsed += Time.deltaTime;
//             yield return null;
//         }

//         trail.transform.position = target;
//         Destroy(trail);
//     }
// }



using System.Collections;
using UnityEngine;

public class CowShootingVFX : MonoBehaviour
{
    public Transform gunLeft;
    public Transform gunRight;
    public GameObject cowBulletTrailPrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject impactPrefab;

    public float attackDuration = 2.5f;   // length of attack
    public float fireRate = 0.1f;         // interval between volleys
    public float visualRange = 20f;

    private Coroutine shootingRoutine;

    public void ShootCow(Vector3 targetPosition)
    {
        if (shootingRoutine != null)
            StopCoroutine(shootingRoutine);

        shootingRoutine = StartCoroutine(ShootRoutine(targetPosition));
    }

    private IEnumerator ShootRoutine(Vector3 targetPosition)
    {
        float elapsed = 0f;

        // Spawn muzzle flashes once at the start using pooling
        if (muzzleFlashPrefab)
        {
            GameObject flashLeft = Pool.Instance.Get(muzzleFlashPrefab);
            flashLeft.transform.position = gunLeft.position;
            flashLeft.transform.rotation = gunLeft.rotation;

            GameObject flashRight = Pool.Instance.Get(muzzleFlashPrefab);
            flashRight.transform.position = gunRight.position;
            flashRight.transform.rotation = gunRight.rotation;

            // Automatically release after duration
            StartCoroutine(ReleaseAfterTime(flashLeft, attackDuration));
            StartCoroutine(ReleaseAfterTime(flashRight, attackDuration));
        }

        while (elapsed < attackDuration)
        {
            FireGun(gunLeft, targetPosition);
            FireGun(gunRight, targetPosition);

            elapsed += fireRate;
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void FireGun(Transform gun, Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - gun.position).normalized;
        Vector3 visualTarget = gun.position + direction * visualRange;

        if (cowBulletTrailPrefab)
        {
            GameObject trail = Pool.Instance.Get(cowBulletTrailPrefab);
            trail.transform.position = gun.position;
            StartCoroutine(MoveTrail(trail, visualTarget));
        }

        if (impactPrefab)
        {
            GameObject impact = Pool.Instance.Get(impactPrefab);
            impact.transform.position = visualTarget;
            StartCoroutine(ReleaseAfterTime(impact, 0.5f));
        }
    }

    private IEnumerator MoveTrail(GameObject trail, Vector3 target)
    {
        float duration = 0.15f;
        Vector3 startPos = trail.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            trail.transform.position = Vector3.Lerp(startPos, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = target;
        Pool.Instance.Release(trail);
    }

    private IEnumerator ReleaseAfterTime(GameObject obj, float time)
    {
        yield return new WaitForSeconds(time);
        Pool.Instance.Release(obj);
    }
}
