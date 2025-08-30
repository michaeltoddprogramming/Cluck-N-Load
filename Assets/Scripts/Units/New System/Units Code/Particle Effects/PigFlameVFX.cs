using System.Collections;
using UnityEngine;

public class PigFlameVFX : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;              // The point flames appear from
    public Transform firePoint2;              // The point flames appear from
    public Transform firePoint3;              // The point flames appear from
    public GameObject flamePrefab;           // The flame visual
    public GameObject impactPrefab;          // The hit effect prefab

    public int flameCount = 6;
    public float spreadAngle = 10f;
    public float flameRange = 100f;           // How far the flames go visually

    // Call this to shoot flames at a target
    public void Shoot(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - firePoint.position).normalized;

        // Fire multiple flame visuals toward target
        for (int i = 0; i < flameCount; i++)
        {
            Vector3 spreadDir = Quaternion.Euler(
                Random.Range(-spreadAngle, spreadAngle),
                Random.Range(-spreadAngle, spreadAngle),
                0
            ) * direction;

            Vector3 visualTarget = firePoint.position + spreadDir * (targetPosition - firePoint.position).magnitude;

            if (flamePrefab)
            {
                GameObject flame = Instantiate(flamePrefab, firePoint.position, firePoint.rotation, firePoint);
                flame.SetActive(true);
                StartCoroutine(EndFlame(flame));



                GameObject flame2 = Instantiate(flamePrefab, firePoint2.position, firePoint2.rotation, firePoint2);
                flame2.SetActive(true);
                StartCoroutine(EndFlame(flame2));

                GameObject flame3 = Instantiate(flamePrefab, firePoint3.position, firePoint3.rotation, firePoint3);
                flame3.SetActive(true);
                StartCoroutine(EndFlame(flame3));
            }
        }

        // Hit effect at target (same as chicken)
        if (impactPrefab)
        {
            GameObject impact = Instantiate(impactPrefab, targetPosition, Quaternion.identity);
            Destroy(impact, 0.5f);
        }
    }

    // Moves flame smoothly from firePoint to target
    private IEnumerator EndFlame(GameObject flame)
    {
        float duration = 0.8f;
        // Vector3 startPos = flame.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // flame.transform.position/
            yield return null;
        }

        // flame.transform.position = target;
        Destroy(flame, 0.1f);
    }
}
