using UnityEngine;

public class SelectionAnimation : MonoBehaviour
{
    private Transform visual;

    void Awake()
    {
        // Find the first child with a MeshRenderer
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null)
            visual = mr.transform;
        else
            visual = transform; // fallback to self if no child
    }

    public void AnimateSelect()
    {
        Vector3 originalScale = visual.localScale;

        LeanTween.scale(visual.gameObject, Vector3.Scale(originalScale, new Vector3(1.1f, 0.8f, 1.1f)), 0.1f)
            .setEaseOutCubic()
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                LeanTween.scale(visual.gameObject, Vector3.Scale(originalScale, new Vector3(0.95f, 1.2f, 0.95f)), 0.1f)
                .setEaseInOutCubic()
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    LeanTween.scale(visual.gameObject, originalScale, 0.1f).setEaseOutBack().setIgnoreTimeScale(true);
                });
            });
    }
}
