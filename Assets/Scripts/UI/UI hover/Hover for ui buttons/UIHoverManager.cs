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

    public float magnatude = 7.5f;

    private bool isPulsing = false;

    private bool isMoneyAnimating = false;

    private void Awake()
    {
        if (uiHover == null)
            uiHover = FindFirstObjectByType<UIHover>();

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
                    pulseScale = smallPulseScale;
                }
                else
                {
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

    public void ShowHoverOnGameObject(GameObject button, string title, string description, bool newScale = false, Vector2? newOffset = null, GameObject pulseTarget = null)
    {
        if (uiHover == null || button == null)
            return;

        // if (!button.)
        // {
            uiHover.Show(title, description, button.GetComponent<RectTransform>(), newScale, newOffset);

            // Pulse the text if provided and can't afford
            if (pulseTarget != null && !isPulsing)
            {
                Vector3 targetScale = pulseTarget.transform.localScale;
                if (Mathf.Approximately(targetScale.x, 0.25f) &&
                    Mathf.Approximately(targetScale.y, 0.25f) &&
                    Mathf.Approximately(targetScale.z, 0.25f))
                {
                    pulseScale = smallPulseScale;
                }
                else
                {
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
        // }
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
                AudioManager.Instance.PlayInsufficientFundsSound();
                if(button != null)
                {
                    FlyMoney(button.GetComponent<RectTransform>());
                }
            }
        }
    }

    public void PlayErrorFeedbackForGameObject(bool isMoney, GameObject button)
    {
        if (button == null)
            return;


        // if(!button.interactable)
        // {
            if(!isMoney)
            {
                AudioManager.Instance.PlayErrorSound();
            }
            else
            {
                AudioManager.Instance.PlayInsufficientFundsSound();
                if(button != null)
                {
                    FlyMoney(button.GetComponent<RectTransform>());
                }
            }
        // }
    }

// This is perfect ------------------------------------------------------------------------------
// public void FlyMoney(RectTransform targetButton)
// {
//     if (moneyUI == null || targetButton == null)
//     {
//         Debug.LogWarning("FlyMoney: moneyUI or targetButton is null");
//         return;
//     }

//     // Make sure it's rendered above everything
//     moneyUI.SetAsLastSibling();

//     // Force start at anchored original
//     moneyUI.anchoredPosition = moneyOriginalPos;
//     Debug.Log($"FlyMoney: Starting at {moneyOriginalPos}");

//     // Get target button's world position
//     Vector3 buttonWorldPos = targetButton.position;

//     // Convert target to local (works perfectly)
//     Vector3 localTargetPos = moneyUI.parent.InverseTransformPoint(buttonWorldPos);
//     Debug.Log($"FlyMoney: Target button = {targetButton.name}, localTargetPos = {localTargetPos}");

//     // Convert the original anchored position to local position for the return trip
//     Vector3 originalWorldPos = moneyUI.TransformPoint(Vector3.zero);
//     Vector3 originalLocalPos = moneyUI.parent.InverseTransformPoint(originalWorldPos);

//     // Animate to button
//     LeanTween.moveLocal(moneyUI.gameObject, localTargetPos, duration)
//         .setEase(LeanTweenType.easeInOutSine)
//         .setOnComplete(() =>
//         {
//             Debug.Log("FlyMoney: Reached button, moving back to original position");

//             // Animate back using proper local position
//             LeanTween.moveLocal(moneyUI.gameObject, originalLocalPos, duration)
//                 .setEase(LeanTweenType.easeInOutSine)
//                 .setOnComplete(() =>
//                 {
//                     moneyUI.anchoredPosition = moneyOriginalPos;
//                     Debug.Log("FlyMoney: Returned to exact original position");
//                 });
//         });
// }

    public void FlyMoney(RectTransform targetButton)
    {
        if (moneyUI == null || targetButton == null || isMoneyAnimating == true)
        {
            Debug.LogWarning("FlyMoney: moneyUI or targetButton is null or animation is playing");
            return; 
        }

        ShakeCamera(magnatude, 0.2f);

        isMoneyAnimating = true;

        // Ensure the money UI is in a top-level overlay Canvas to be on top of everything
        Canvas canvas = moneyUI.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 9999; // very high so it's on top of everything
        }

        // Make sure it's rendered above all siblings in this Canvas
        moneyUI.SetAsLastSibling();

        // Force start at anchored original
        moneyUI.anchoredPosition = moneyOriginalPos;

        // Get target button's world position
        Vector3 buttonWorldPos = targetButton.position;

        // Convert target to local (works perfectly)
        Vector3 localTargetPos = moneyUI.parent.InverseTransformPoint(buttonWorldPos);

        // Convert the original anchored position to local position for the return trip
        Vector3 originalWorldPos = moneyUI.TransformPoint(Vector3.zero);
        Vector3 originalLocalPos = moneyUI.parent.InverseTransformPoint(originalWorldPos);

        // Animate to button
        LeanTween.moveLocal(moneyUI.gameObject, localTargetPos, duration)
            .setEase(LeanTweenType.easeInOutSine)
            .setOnComplete(() =>
            {

                // Animate back using proper local position
                LeanTween.moveLocal(moneyUI.gameObject, originalLocalPos, duration)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setOnComplete(() =>
                    {
                        moneyUI.anchoredPosition = moneyOriginalPos;
                        isMoneyAnimating = false;
                    });
            });
    }

    public void ShakeCamera(float magnitude = 0.1f, float duration = 0.2f)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 originalPos = cam.transform.position;

        LeanTween.value(cam.gameObject, 0f, 1f, duration)
            .setEase(LeanTweenType.easeShake)
            .setOnUpdate((float val) =>
            {
                float x = Random.Range(-magnitude, magnitude) * val;
                float y = Random.Range(-magnitude, magnitude) * val;
                cam.transform.position = originalPos + new Vector3(x, y, 0);
            })
            .setOnComplete(() =>
            {
                cam.transform.position = originalPos; // restore
            });
    }
}
