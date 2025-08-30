using UnityEngine;

public class SheepExplodingVFX : MonoBehaviour
{
    [Header("References")]
    public GameObject explosionPrefab;   // Your particle effect prefab

    // Call this when the sheep dies or explodes
    public void Explode(Vector3 position)
    {
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);

            // Optional: if your particle system doesn’t auto-destroy
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(explosion, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(explosion, 2f); // fallback destroy time
            }
        }
    }
}
