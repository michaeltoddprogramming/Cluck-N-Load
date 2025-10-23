using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHoverManager : MonoBehaviour
{
    [Header("References")]
    public UIHover uiHover;               // Tooltip system
    public GameObject currentPulseTarget;
    [SerializeField] private Vector2 moneyOriginalPos; // assign in inspector or Awake

    [Header("Pulse Settings")]
    public float pulseScale = 1.2f;
    public float smallPulseScale = 0.4f;
    public float pulseDuration = 0.5f;
    private Vector3 originalScale;

    [Header("Money flying animation")]
    [SerializeField] private RectTransform moneyUI;
    [SerializeField] float duration = 0.5f;
    [SerializeField] float delay = 0f;

    private bool isPulsing = false;

    private void Awake()
    {
        if (uiHover == null)
            uiHover = FindObjectOfType<UIHover>();

        if (moneyUI != null)
            moneyOriginalPos = moneyUI.anchoredPosition;
    }

    public void ShowHover(Button button, string title, string description, bool newScale = false, Vector2? newOffset = null, GameObject pulseTarget = null)
    {
        if (uiHover == null || button == null)
            return;

        if (!button.interactable)
        {
            uiHover.Show(title, description, button.GetComponent<RectTransform>(), newScale, newOffset);

            // Pulse the text if provided and can't afford
            if (pulseTarget != null && !isPulsing)
            {
                Vector3 targetScale = pulseTarget.transform.localScale;
                if (Mathf.Approximately(targetScale.x, 0.25f) &&
                    Mathf.Approximately(targetScale.y, 0.25f) &&
                    Mathf.Approximately(targetScale.z, 0.25f))
                {
                    Debug.Log("it is the progress bar");
                    pulseScale = smallPulseScale;
                }
                else
                {
                    Debug.Log("it is not the progress bar");
                    pulseScale = 1.2f;
                }


                currentPulseTarget = pulseTarget;
                isPulsing = true;

                RectTransform rt = currentPulseTarget.GetComponent<RectTransform>();
                if (rt != null)
                    rt.pivot = new Vector2(0.5f, 0.5f);

                originalScale = currentPulseTarget.transform.localScale;

                // start LeanTween pulse (loops ping-pong)
                LeanTween.scale(currentPulseTarget, Vector3.one * pulseScale, pulseDuration)
                    .setEaseInOutSine()
                    .setLoopPingPong(); // infinite ping-pong until canceled
            }
        }
    }

    public void HideHover()
    {
        if (uiHover != null)
            uiHover.Hide();

        if (isPulsing && currentPulseTarget != null)
        {
            LeanTween.cancel(currentPulseTarget, true);

            currentPulseTarget.transform.localScale = originalScale;
            // currentPulseTarget.transform.localScale = Vector3.one;

            isPulsing = false;
            currentPulseTarget = null;
        }
    }

    public void PlayErrorFeedback(bool isMoney, Button button)
    {
        if (button == null)
            return;

            
        if(!button.interactable)
        {
            if(!isMoney)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            else
            {
                AudioManager.Instance.PlayErrorSound();
                if(button != null)
                {
                    Debug.Log("it should be playing the money effect");
                    FlyMoney(button.GetComponent<RectTransform>());
                }
            }
        }
    }

// This is perfect ------------------------------------------------------------------------------
public void FlyMoney(RectTransform targetButton)
{
    if (moneyUI == null || targetButton == null)
    {
        Debug.LogWarning("FlyMoney: moneyUI or targetButton is null");
        return;
    }

    // Make sure it's rendered above everything
    moneyUI.SetAsLastSibling();

    // Force start at anchored original
    moneyUI.anchoredPosition = moneyOriginalPos;
    Debug.Log($"FlyMoney: Starting at {moneyOriginalPos}");

    // Get target button's world position
    Vector3 buttonWorldPos = targetButton.position;

    // Convert target to local (works perfectly)
    Vector3 localTargetPos = moneyUI.parent.InverseTransformPoint(buttonWorldPos);
    Debug.Log($"FlyMoney: Target button = {targetButton.name}, localTargetPos = {localTargetPos}");

    // Convert the original anchored position to local position for the return trip
    Vector3 originalWorldPos = moneyUI.TransformPoint(Vector3.zero);
    Vector3 originalLocalPos = moneyUI.parent.InverseTransformPoint(originalWorldPos);

    // Animate to button
    LeanTween.moveLocal(moneyUI.gameObject, localTargetPos, duration)
        .setEase(LeanTweenType.easeInOutSine)
        .setOnComplete(() =>
        {
            Debug.Log("FlyMoney: Reached button, moving back to original position");

            // Animate back using proper local position
            LeanTween.moveLocal(moneyUI.gameObject, originalLocalPos, duration)
                .setEase(LeanTweenType.easeInOutSine)
                .setOnComplete(() =>
                {
                    moneyUI.anchoredPosition = moneyOriginalPos;
                    Debug.Log("FlyMoney: Returned to exact original position");
                });
        });
}

















}
