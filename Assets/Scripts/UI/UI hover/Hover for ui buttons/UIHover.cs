// using UnityEngine;
// using TMPro;

// public class UIHover : MonoBehaviour
// {
//     [SerializeField] private GameObject tooltipPanel;
//     [SerializeField] private TextMeshProUGUI Title;
//     [SerializeField] private TextMeshProUGUI Description;
//     [SerializeField] private Vector2 offset = new Vector2(0, -200); // adjust vertical/horizontal offset


//     void Awake()
//     {
//         tooltipPanel.SetActive(false);
//     }

//     public void Show(string title, string description, RectTransform target)
//     {
//         tooltipPanel.SetActive(true);

//         tooltipPanel.SetActive(true);
//         Title.text = title;
//         Description.text = description;

//         // Position next to target UI element
//         Vector3 worldPos = target.position + (Vector3)offset;
//         tooltipPanel.transform.position = worldPos;
//     }

//     public void Hide()
//     {
//         tooltipPanel.SetActive(false);
//     }
// }



// using UnityEngine;
// using TMPro;
// using System.Collections;

// public class UIHover : MonoBehaviour
// {
//     [SerializeField] private GameObject tooltipPanel;
//     [SerializeField] private TextMeshProUGUI Title;
//     [SerializeField] private TextMeshProUGUI Description;
//     [SerializeField] private Vector2 offset = new Vector2(0, -200); // adjust vertical/horizontal offset
//     [SerializeField] private float fadeDuration = 0.25f;
//     [SerializeField] private float slideDistance = 20f; // pixels to slide from
//     private CanvasGroup canvasGroup;
//     private Coroutine currentCoroutine;

//     void Awake()
//     {
//         if (tooltipPanel == null)
//         {
//             Debug.LogError("Tooltip Panel not assigned!");
//             return;
//         }

//         tooltipPanel.SetActive(true); // Keep active to manipulate CanvasGroup
//         canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
//         if (canvasGroup == null)
//             canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();

//         canvasGroup.alpha = 0;
//         tooltipPanel.SetActive(false);
//     }

//     public void Show(string title, string description, RectTransform target)
//     {
//         Title.text = title;
//         Description.text = description;

//         // Position next to target UI element
//         Vector3 worldPos = target.position + (Vector3)offset;
//         tooltipPanel.transform.position = worldPos;

//         // Stop any previous animation
//         if (currentCoroutine != null)
//             StopCoroutine(currentCoroutine);

//         tooltipPanel.SetActive(true);
//         currentCoroutine = StartCoroutine(FadeAndSlideIn());
//     }

//     public void Hide()
//     {
//         if (currentCoroutine != null)
//             StopCoroutine(currentCoroutine);

//         currentCoroutine = StartCoroutine(FadeAndSlideOut());
//     }

//     private IEnumerator FadeAndSlideIn()
//     {
//         Vector3 startPos = tooltipPanel.transform.position + Vector3.down * slideDistance;
//         Vector3 endPos = tooltipPanel.transform.position;
//         tooltipPanel.transform.position = startPos;

//         float elapsed = 0f;
//         while (elapsed < fadeDuration)
//         {
//             elapsed += Time.unscaledDeltaTime;
//             float t = Mathf.Clamp01(elapsed / fadeDuration);
//             canvasGroup.alpha = t;
//             tooltipPanel.transform.position = Vector3.Lerp(startPos, endPos, t);
//             yield return null;
//         }

//         canvasGroup.alpha = 1;
//         tooltipPanel.transform.position = endPos;
//     }

//     private IEnumerator FadeAndSlideOut()
//     {
//         Vector3 startPos = tooltipPanel.transform.position;
//         Vector3 endPos = startPos + Vector3.down * slideDistance;

//         float elapsed = 0f;
//         while (elapsed < fadeDuration)
//         {
//             elapsed += Time.unscaledDeltaTime;
//             float t = Mathf.Clamp01(elapsed / fadeDuration);
//             canvasGroup.alpha = 1 - t;
//             tooltipPanel.transform.position = Vector3.Lerp(startPos, endPos, t);
//             yield return null;
//         }

//         canvasGroup.alpha = 0;
//         tooltipPanel.SetActive(false);
//     }
// }




// using UnityEngine;
// using TMPro;
// using System.Collections;

// public class UIHover : MonoBehaviour
// {
//     [SerializeField] private GameObject tooltipPanel;
//     [SerializeField] private TextMeshProUGUI Title;
//     [SerializeField] private TextMeshProUGUI Description;
//     [SerializeField] private Vector2 offset = new Vector2(0, -200); 
//     [SerializeField] private float animationDuration = 0.25f;
//     [SerializeField] private float startScale = 0f;
//     [SerializeField] private float targetScale = 1f;

//     private Coroutine currentCoroutine;

//     void Awake()
//     {
//         if (tooltipPanel == null)
//         {
//             Debug.LogError("Tooltip Panel not assigned!");
//             return;
//         }

//         tooltipPanel.SetActive(true); // keep active to manipulate scale
//         tooltipPanel.transform.localScale = Vector3.one * startScale;
//         tooltipPanel.SetActive(false);
//     }

//     public void Show(string title, string description, RectTransform target)
//     {
//         Title.text = title;
//         Description.text = description;

//         // Position next to target UI element
//         tooltipPanel.transform.position = (Vector3)target.position + (Vector3)offset;

//         // Stop any previous animation
//         if (currentCoroutine != null)
//             StopCoroutine(currentCoroutine);

//         tooltipPanel.SetActive(true);
//         currentCoroutine = StartCoroutine(ScaleIn());
//     }

//     public void Hide()
//     {
//         if (currentCoroutine != null)
//             StopCoroutine(currentCoroutine);

//         currentCoroutine = StartCoroutine(ScaleOut());
//     }

//     private IEnumerator ScaleIn()
//     {
//         float elapsed = 0f;
//         Vector3 initialScale = Vector3.one * startScale;
//         Vector3 finalScale = Vector3.one * targetScale;

//         while (elapsed < animationDuration)
//         {
//             elapsed += Time.unscaledDeltaTime;
//             float t = Mathf.Clamp01(elapsed / animationDuration);
//             t = Mathf.SmoothStep(0, 1, t); // smooth easing
//             tooltipPanel.transform.localScale = Vector3.Lerp(initialScale, finalScale, t);
//             yield return null;
//         }

//         tooltipPanel.transform.localScale = finalScale;
//     }

//     private IEnumerator ScaleOut()
//     {
//         float elapsed = 0f;
//         Vector3 initialScale = tooltipPanel.transform.localScale;
//         Vector3 finalScale = Vector3.one * startScale;

//         while (elapsed < animationDuration)
//         {
//             elapsed += Time.unscaledDeltaTime;
//             float t = Mathf.Clamp01(elapsed / animationDuration);
//             t = Mathf.SmoothStep(0, 1, t);
//             tooltipPanel.transform.localScale = Vector3.Lerp(initialScale, finalScale, t);
//             yield return null;
//         }

//         tooltipPanel.transform.localScale = finalScale;
//         tooltipPanel.SetActive(false);
//     }
// }

// using UnityEngine;
// using TMPro;
// using System.Collections;

// public class UIHover : MonoBehaviour
// {
//     [SerializeField] private GameObject tooltipPanel;
//     [SerializeField] private TextMeshProUGUI Title;
//     [SerializeField] private TextMeshProUGUI Description;
//     [SerializeField] private Vector2 offset = new Vector2(0, -200);

//     [Header("Pop Animation Settings")]
//     [SerializeField] private float animationDuration = 0.25f;
//     [SerializeField] private float popScale = 1.2f; // how much bigger it pops
//     [SerializeField] private float normalScale = 1f; // normal size
//     [SerializeField] private float hideScale = 0.8f; // scale before hiding

//     private Coroutine currentCoroutine;

//     void Awake()
//     {
//         if (tooltipPanel == null)
//         {
//             Debug.LogError("Tooltip Panel not assigned!");
//             return;
//         }

//         tooltipPanel.SetActive(true); // keep active to manipulate scale
//         tooltipPanel.transform.localScale = Vector3.zero;
//         tooltipPanel.SetActive(false);
//     }

//     public void Show(string title, string description, RectTransform target)
//     {
//         Title.text = title;
//         Description.text = description;

//         tooltipPanel.transform.position = (Vector3)target.position + (Vector3)offset;

//         if (currentCoroutine != null)
//             StopCoroutine(currentCoroutine);

//         tooltipPanel.SetActive(true);
//         currentCoroutine = StartCoroutine(PopIn());
//     }

//     public void Hide()
//     {
//         if (currentCoroutine != null)
//             StopCoroutine(currentCoroutine);

//         currentCoroutine = StartCoroutine(PopOut());
//     }

//     private IEnumerator PopIn()
//     {
//         float elapsed = 0f;
//         Vector3 start = Vector3.zero;
//         Vector3 overshoot = Vector3.one * popScale;
//         Vector3 target = Vector3.one * normalScale;

//         while (elapsed < animationDuration)
//         {
//             elapsed += Time.unscaledDeltaTime;
//             float t = Mathf.Clamp01(elapsed / animationDuration);
//             // Ease with overshoot: first go past target, then settle
//             if (t < 0.7f)
//             {
//                 tooltipPanel.transform.localScale = Vector3.Lerp(start, overshoot, t / 0.7f);
//             }
//             else
//             {
//                 tooltipPanel.transform.localScale = Vector3.Lerp(overshoot, target, (t - 0.7f) / 0.3f);
//             }
//             yield return null;
//         }

//         tooltipPanel.transform.localScale = target;
//     }

//     private IEnumerator PopOut()
//     {
//         float elapsed = 0f;
//         Vector3 start = tooltipPanel.transform.localScale;
//         Vector3 undershoot = Vector3.one * hideScale;
//         Vector3 end = Vector3.zero;

//         while (elapsed < animationDuration)
//         {
//             elapsed += Time.unscaledDeltaTime;
//             float t = Mathf.Clamp01(elapsed / animationDuration);
//             // Ease with undershoot: shrink slightly smaller, then disappear
//             if (t < 0.7f)
//             {
//                 tooltipPanel.transform.localScale = Vector3.Lerp(start, undershoot, t / 0.7f);
//             }
//             else
//             {
//                 tooltipPanel.transform.localScale = Vector3.Lerp(undershoot, end, (t - 0.7f) / 0.3f);
//             }
//             yield return null;
//         }

//         tooltipPanel.transform.localScale = end;
//         tooltipPanel.SetActive(false);
//     }
// }

using UnityEngine;
using TMPro;
using System.Collections;

public class UIHover : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI Title;
    [SerializeField] private TextMeshProUGUI Description;
    [SerializeField] private Vector2 offset = new Vector2(0, -200);

    [Header("Pop Animation Settings")]
    [SerializeField] private float animationDuration = 0.15f; // snappy speed
    [SerializeField] private float popScale = 1.2f; // scale bigger than normal
    [SerializeField] private float normalScale = 1f;

    private Coroutine currentCoroutine;

    void Awake()
    {
        if (tooltipPanel == null)
        {
            Debug.LogError("Tooltip Panel not assigned!");
            return;
        }

        tooltipPanel.SetActive(true); 
        tooltipPanel.transform.localScale = Vector3.zero;
        tooltipPanel.SetActive(false);
    }

    public void Show(string title, string description, RectTransform target)
    {
        Title.text = title;
        Description.text = description;

        tooltipPanel.transform.position = (Vector3)target.position + (Vector3)offset;

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        tooltipPanel.SetActive(true);
        currentCoroutine = StartCoroutine(SnappyPop(Vector3.zero, normalScale));
    }

    public void Hide()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(SnappyPop(tooltipPanel.transform.localScale, 0, true));
    }

    private IEnumerator SnappyPop(Vector3 start, float end, bool deactivateOnEnd = false)
    {
        float elapsed = 0f;
        Vector3 overshoot = Vector3.one * popScale;
        Vector3 target = Vector3.one * end;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            // Simple snappy pop using smoothstep for smooth animation
            if (t < 0.5f)
            {
                tooltipPanel.transform.localScale = Vector3.Lerp(start, overshoot, t / 0.5f);
            }
            else
            {
                tooltipPanel.transform.localScale = Vector3.Lerp(overshoot, target, (t - 0.5f) / 0.5f);
            }

            yield return null;
        }

        tooltipPanel.transform.localScale = target;

        if (deactivateOnEnd)
            tooltipPanel.SetActive(false);
    }
}


