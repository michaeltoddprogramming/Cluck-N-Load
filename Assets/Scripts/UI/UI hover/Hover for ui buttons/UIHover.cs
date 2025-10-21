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


