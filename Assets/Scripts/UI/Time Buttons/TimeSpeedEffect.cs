using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimeSpeedEffect : MonoBehaviour
{
    [SerializeField] private Image borderImage;
    [SerializeField] private float pulseDuration = 3f; // seconds for a full fade cycle
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.6f;

    private Coroutine pulseCoroutine;

    public void StartSpeedEffect()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseBorder());
    }

    public void StopSpeedEffect()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = null;
        SetAlpha(0f);
    }

    private IEnumerator PulseBorder()
    {
        while (true)
        {
            // Calculate time in the range 0-1 over the pulse duration
            float t = (Mathf.Sin(Time.time / pulseDuration * Mathf.PI * 2f) + 1f) / 2f;

            // Remap to min/max alpha
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            SetAlpha(alpha);
            yield return null;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (borderImage == null) return;
        Color c = borderImage.color;
        c.a = alpha;
        borderImage.color = c;
    }
}
