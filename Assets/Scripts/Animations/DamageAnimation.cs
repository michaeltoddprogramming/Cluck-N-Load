using UnityEngine;

public class DamageAnimation : MonoBehaviour
{
    // public void PlayDamageHitEffect()
    // {
    //     transform.localScale = Vector3.one;

    //     // Try to get SpriteRenderer (for 2D) or MeshRenderer (for 3D)
    //     SpriteRenderer sr = GetComponent<SpriteRenderer>();
    //     Color originalColor = Color.white;

    //     if (sr != null)
    //     {
    //         originalColor = sr.color;
    //         sr.color = Color.red; // Flash red
    //         LeanTween.delayedCall(gameObject, 0.1f, () => sr.color = originalColor); // Revert color
    //     }

    //     // Bounce animation
    //     LeanTween.scale(gameObject, new Vector3(0.9f, 0.9f, 1f), 0.05f)
    //         .setEase(LeanTweenType.easeInQuad)
    //         .setOnComplete(() => {
    //             LeanTween.scale(gameObject, new Vector3(1.05f, 1.05f, 1f), 0.1f)
    //                 .setEase(LeanTweenType.easeOutQuad)
    //                 .setOnComplete(() => {
    //                     LeanTween.scale(gameObject, Vector3.one, 0.05f)
    //                         .setEase(LeanTweenType.easeOutBounce);
    //                 });
    //         });
    // }

    
    
public void PlayDamageHitEffect()
{
    transform.localScale = Vector3.one;

    SpriteRenderer sr = GetComponent<SpriteRenderer>();
    MeshRenderer mr = GetComponent<MeshRenderer>();

    if (sr != null)
    {
        Color originalColor = sr.color;
        sr.color = Color.red;
        LeanTween.delayedCall(gameObject, 0.1f, () => sr.color = originalColor);
    }
    else if (mr != null)
    {
        Material mat = mr.material;
        Color originalColor = mat.color;
        mat.color = Color.red;
        LeanTween.delayedCall(gameObject, 0.1f, () => mat.color = originalColor);
    }

    // Bounce animation
    LeanTween.scale(gameObject, new Vector3(0.9f, 0.9f, 1f), 0.05f)
        .setEase(LeanTweenType.easeInQuad)
        .setOnComplete(() => {
            LeanTween.scale(gameObject, new Vector3(1.05f, 1.05f, 1f), 0.1f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    LeanTween.scale(gameObject, Vector3.one, 0.05f)
                        .setEase(LeanTweenType.easeOutBounce);
                });
        });
}



}
