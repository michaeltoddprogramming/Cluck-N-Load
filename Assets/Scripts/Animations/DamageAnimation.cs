using UnityEngine;
public class DamageAnimation : MonoBehaviour
{
    public AudioClip hitSound;
    private AudioSource audioSource;
    private AudioSource damageAudioSource;
    
    private Color originalSpriteColor;
    private Color originalMeshColor;
    private bool hasInitializedColors = false;
    
    // Cached component references for performance
    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        AudioSource existingAudioSource = GetComponent<AudioSource>();

        // Load hitSound if not set already
        if (hitSound == null)
        {
            hitSound = Resources.Load<AudioClip>("Sounds/SFX/Damage/StructureDamage1");
        }

        // Add a NEW AudioSource for damage sounds
        damageAudioSource = gameObject.AddComponent<AudioSource>();

        if (existingAudioSource != null)
        {
            // Copy all relevant AudioSource properties exactly (except clip)
            damageAudioSource.outputAudioMixerGroup = existingAudioSource.outputAudioMixerGroup;
            damageAudioSource.playOnAwake = false;
            damageAudioSource.loop = false;
            damageAudioSource.mute = existingAudioSource.mute;
            damageAudioSource.bypassEffects = existingAudioSource.bypassEffects;
            damageAudioSource.bypassListenerEffects = existingAudioSource.bypassListenerEffects;
            damageAudioSource.bypassReverbZones = existingAudioSource.bypassReverbZones;
            damageAudioSource.priority = 0;
            // damageAudioSource.volume = existingAudioSource.volume;
            damageAudioSource.volume = 200f;
            damageAudioSource.pitch = existingAudioSource.pitch;
            damageAudioSource.panStereo = existingAudioSource.panStereo;
            damageAudioSource.spatialBlend = 1f;
            damageAudioSource.reverbZoneMix = existingAudioSource.reverbZoneMix;
            damageAudioSource.dopplerLevel = existingAudioSource.dopplerLevel;
            damageAudioSource.spread = existingAudioSource.spread;
            damageAudioSource.rolloffMode = existingAudioSource.rolloffMode;
            damageAudioSource.minDistance = existingAudioSource.minDistance;
            damageAudioSource.maxDistance = existingAudioSource.maxDistance;
            damageAudioSource.velocityUpdateMode = existingAudioSource.velocityUpdateMode;
            damageAudioSource.ignoreListenerPause = existingAudioSource.ignoreListenerPause;
            damageAudioSource.ignoreListenerVolume = existingAudioSource.ignoreListenerVolume;
        }
        else
        {
            // Default settings if no existing AudioSource
            damageAudioSource.playOnAwake = false;
            damageAudioSource.spatialBlend = 1f;
            damageAudioSource.rolloffMode = AudioRolloffMode.Linear;
            damageAudioSource.minDistance = 2f;
            damageAudioSource.maxDistance = 125f;
            damageAudioSource.dopplerLevel = 0f;
            damageAudioSource.spread = 0f;
            damageAudioSource.reverbZoneMix = 1f;
            damageAudioSource.loop = false;
        }

        // Assign the clip only once, after hitSound is loaded/set
        damageAudioSource.clip = hitSound;
        
        // Cache component references for performance
        spriteRenderer = GetComponent<SpriteRenderer>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void PlayDamageHitEffect()
    {
        LeanTween.cancel(gameObject);

        transform.localScale = Vector3.one;

        if (!hasInitializedColors)
        {
            if (spriteRenderer != null)
            {
                originalSpriteColor = spriteRenderer.color;
            }
            else if (meshRenderer != null && meshRenderer.material != null)
            {
                originalMeshColor = meshRenderer.material.color;
            }
            hasInitializedColors = true;
        }

        CancelInvoke("RestoreOriginalColor");

        if (spriteRenderer != null)
        {
            if (spriteRenderer.color != Color.red)
            {
                originalSpriteColor = spriteRenderer.color;
            }
            spriteRenderer.color = Color.red;
        }
        else if (meshRenderer != null)
        {
            Material mat = meshRenderer.material;
            if (mat.color != Color.red)
            {
                originalMeshColor = mat.color;
            }
            mat.color = Color.red;
        }

        Invoke("RestoreOriginalColor", 0.1f);

        if (hitSound != null && damageAudioSource != null)
        {
            Debug.Log("the damage sound should have played");
            // damageAudioSource.PlayOneShot(hitSound);
            damageAudioSource.Play();
        }

        // Bounce animation
        LeanTween.scale(gameObject, new Vector3(0.9f, 0.9f, 1f), 0.05f)
            .setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, new Vector3(1.05f, 1.05f, 1f), 0.1f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setOnComplete(() =>
                    {
                        LeanTween.scale(gameObject, Vector3.one, 0.05f)
                            .setEase(LeanTweenType.easeOutBounce);
                    });
            });
    }

    private void RestoreOriginalColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalSpriteColor;
        }
        else if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = originalMeshColor;
        }
    }
}
