using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    public float maxIntensity = 0.4f;
    public float duration = 0.4f;
    public float maxDistance = 55f;
    public float cutoffDistance = 55f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // public void ShakeAtPosition(Vector3 explosionPosition, float maxIntensity = 1f, float duration = 0.4f, float maxDistance = 10f)
    // {
    //     float distance = Vector3.Distance(Camera.main.transform.position, explosionPosition);
    //     float intensity = Mathf.Clamp01(1 - (distance / maxDistance)) * maxIntensity;
    //     if (intensity > 0)
    //         StartCoroutine(ShakeRoutine(intensity, duration));
    // }

    public void ShakeAtPosition(Vector3 explosionPosition)
    {
        float distance = Vector3.Distance(Camera.main.transform.position, explosionPosition);
        if (distance > cutoffDistance) return; // hard cutoff

        // Smooth falloff using inverse square (like 3D audio)
        float intensity = maxIntensity / (1f + (distance / maxDistance) * (distance / maxDistance));

        if (intensity > 0.01f) // only shake if noticeable
            StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        Vector3 originalPos = transform.localPosition;
        Quaternion originalRot = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            float zRot = Random.Range(-1f, 1f) * intensity * 5f; // small rotation for more impact

            transform.localPosition = originalPos + new Vector3(x, y, 0f);
            transform.localRotation = originalRot * Quaternion.Euler(0, 0, zRot);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        transform.localRotation = originalRot;
    }

    // public float debugShakeRadius = 10f; // for visualization only

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, cutoffDistance);
    }
#endif
}
