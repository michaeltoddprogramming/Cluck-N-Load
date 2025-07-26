using UnityEngine;

public class DestroyStructure : MonoBehaviour
{
    public void PlayDestructionAnimationAndDestroy()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 squashedScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 0.5f, originalScale.z * 1.2f);
        Vector3 finalScale = Vector3.zero;

        // Step 1: Quick squash
        LeanTween.scale(gameObject, squashedScale, 0.1f).setEaseInOutQuad().setOnComplete(() =>
        {
            // Step 2: Shrink to nothing with slight delay
            LeanTween.scale(gameObject, finalScale, 0.25f).setEaseInBack().setOnComplete(() =>
            {
                Destroy(gameObject);
            });
        });
    }
}
