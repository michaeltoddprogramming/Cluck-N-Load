// using UnityEngine;
// using UnityEngine.UI;
// using System.Collections;

// // Ensures an Image component is present
// [RequireComponent(typeof(Image))]
// public class TimeSpeedEffect : MonoBehaviour
// {
//     [SerializeField] private Image borderImage;
//     private void Awake()
//     {
//         // Only warn if borderImage is not assigned in Inspector
//         if (borderImage == null)
//         {
//             Debug.Log("Border image currently is: " + borderImage, this);
//             Debug.LogError("TimeSpeedEffect: borderImage is not assigned! Please assign the correct Image in the Inspector.");
//         }
//     }
//     [SerializeField] private float pulseDuration = 3f; // seconds for a full fade cycle
//     [SerializeField] private float minAlpha = 0.2f;
//     [SerializeField] private float maxAlpha = 0.6f;

//     private Coroutine pulseCoroutine;

//     public void StartSpeedEffect()
//     {
//         Debug.Log("laikojedrfb gbj ihgserhjk bghkjb gsdfkhbjl");
//         if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
//         pulseCoroutine = StartCoroutine(PulseBorder());
//     }

//     public void StopSpeedEffect()
//     {
//         if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
//         pulseCoroutine = null;
//         SetAlpha(0f);
//     }

//     private IEnumerator PulseBorder()
//     {
//         while (true)
//         {
//             // Calculate time in the range 0-1 over the pulse duration
//             float t = (Mathf.Sin(Time.time / pulseDuration * Mathf.PI * 2f) + 1f) / 2f;

//             // Remap to min/max alpha
//             float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

//             SetAlpha(alpha);
//             yield return null;
//         }
//     }

//     private void SetAlpha(float alpha)
//     {
//         Debug.Log("Alpha set to: " + alpha);
//         if (borderImage == null)
//         {
//             Debug.LogWarning("RUAN BLED FOR YOU, WHY ARE YOU LIKE THIS?");
//             return;
//         }
//         Color c = borderImage.color;
//         c.a = alpha;
//         borderImage.color = c;
//     }
// }



using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Ensures an Image component is present
[RequireComponent(typeof(Image))]
public class TimeSpeedEffect : MonoBehaviour
{
    private Image img;
    private void Awake()
    {
        img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogError("TimeSpeedEffect: No Image component found on this GameObject!");
        }
    }
    [SerializeField] private float pulseDuration = 3f; // seconds for a full fade cycle
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.6f;

    private Coroutine pulseCoroutine;

    public void StartSpeedEffect()
    {
        Debug.Log("laikojedrfb gbj ihgserhjk bghkjb gsdfkhbjl");
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
        Debug.Log("Alpha set to: " + alpha);
        if (img == null) {
            Debug.LogWarning("TimeSpeedEffect: No Image component found!");
            return;
        }
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
