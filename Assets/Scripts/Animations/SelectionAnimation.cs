using UnityEngine;

public class SelectionAnimation : MonoBehaviour
{
    public void AnimateSelect()
    {
        // Store original scale
        Vector3 originalScale = transform.localScale;

        // Squash
        LeanTween.scale(gameObject, Vector3.Scale(originalScale, new Vector3(1.1f, 0.8f, 1.1f)), 0.1f)
            .setEaseOutCubic()
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                // Stretch
                LeanTween.scale(gameObject, Vector3.Scale(originalScale, new Vector3(0.95f, 1.2f, 0.95f)), 0.1f)
                .setEaseInOutCubic()
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    // Return to normal
                    LeanTween.scale(gameObject, originalScale, 0.1f).setEaseOutBack().setIgnoreTimeScale(true);
                });
            });
    }
}
