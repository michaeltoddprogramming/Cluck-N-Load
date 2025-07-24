using UnityEngine;

public class DamageAnimation : MonoBehaviour
{
    public AudioClip hitSound;
    private AudioSource audioSource;
    
    private AudioSource damageAudioSource;

    // private void Awake()
    // {
    //     audioSource = GetComponent<AudioSource>();
    //     if (audioSource == null)
    //     {
    //         audioSource = gameObject.AddComponent<AudioSource>();
    //     }

    //     audioSource.playOnAwake = false;
    //     audioSource.spatialBlend = 1f; // 3D sound
    //     audioSource.rolloffMode = AudioRolloffMode.Linear; // or Logarithmic
    //     audioSource.minDistance = 2f;  // Full volume within this range
    //     audioSource.maxDistance = 10f; // Fades to 0 volume at this distance
    //     audioSource.dopplerLevel = 0f; // Optional: turn off Doppler effect
    //     audioSource.spread = 0f;       // Optional: how wide the sound is
    //     audioSource.reverbZoneMix = 1f;

    //     if (hitSound == null)
    //     {
    //         hitSound = Resources.Load<AudioClip>("Sounds/SFX/Damage/StructureDamage1");
    //     }
    // }

    // private void Awake()
    // {
    //      AudioSource existingAudioSource = GetComponent<AudioSource>();
    //     // Add a NEW AudioSource for damage sounds
    //     damageAudioSource = gameObject.AddComponent<AudioSource>();

    //     if (existingAudioSource != null)
    //     {
    //         // Copy all relevant AudioSource properties exactly
    //         damageAudioSource.clip = hitSound;
    //         damageAudioSource.outputAudioMixerGroup = existingAudioSource.outputAudioMixerGroup;
    //         damageAudioSource.playOnAwake = false;  // usually false for effects
    //         damageAudioSource.loop = false;          // damage sounds usually don't loop
    //         damageAudioSource.mute = existingAudioSource.mute;
    //         damageAudioSource.bypassEffects = existingAudioSource.bypassEffects;
    //         damageAudioSource.bypassListenerEffects = existingAudioSource.bypassListenerEffects;
    //         damageAudioSource.bypassReverbZones = existingAudioSource.bypassReverbZones;
    //         damageAudioSource.priority = existingAudioSource.priority;
    //         damageAudioSource.volume = existingAudioSource.volume;
    //         damageAudioSource.pitch = existingAudioSource.pitch;
    //         damageAudioSource.panStereo = existingAudioSource.panStereo;
    //         damageAudioSource.spatialBlend = existingAudioSource.spatialBlend;
    //         damageAudioSource.reverbZoneMix = existingAudioSource.reverbZoneMix;
    //         damageAudioSource.dopplerLevel = existingAudioSource.dopplerLevel;
    //         damageAudioSource.spread = existingAudioSource.spread;
    //         damageAudioSource.rolloffMode = existingAudioSource.rolloffMode;
    //         damageAudioSource.minDistance = existingAudioSource.minDistance;
    //         damageAudioSource.maxDistance = existingAudioSource.maxDistance;
    //         damageAudioSource.velocityUpdateMode = existingAudioSource.velocityUpdateMode;
    //         damageAudioSource.ignoreListenerPause = existingAudioSource.ignoreListenerPause;
    //         damageAudioSource.ignoreListenerVolume = existingAudioSource.ignoreListenerVolume;
    //         damageAudioSource.loop = false; // usually don't loop damage sounds
    //     }
    //     else
    //     {
    //         // Set some sane defaults if no existing AudioSource
    //         damageAudioSource.playOnAwake = false;
    //         damageAudioSource.spatialBlend = 1f;
    //         damageAudioSource.rolloffMode = AudioRolloffMode.Linear;
    //         damageAudioSource.minDistance = 2f;
    //         damageAudioSource.maxDistance = 10f;
    //         damageAudioSource.dopplerLevel = 0f;
    //         damageAudioSource.spread = 0f;
    //         damageAudioSource.reverbZoneMix = 1f;
    //         damageAudioSource.loop = false;
    //     }

    //     if (hitSound == null)
    //     {
    //         hitSound = Resources.Load<AudioClip>("Sounds/SFX/Damage/StructureDamage1");
    //     }

    //     // Assign the damage clip to the new AudioSource (optional if you want it assigned)
    //     damageAudioSource.clip = hitSound;
    // }

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
        damageAudioSource.priority = existingAudioSource.priority;
        damageAudioSource.volume = existingAudioSource.volume;
        damageAudioSource.pitch = existingAudioSource.pitch;
        damageAudioSource.panStereo = existingAudioSource.panStereo;
        damageAudioSource.spatialBlend = existingAudioSource.spatialBlend;
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
        damageAudioSource.maxDistance = 10f;
        damageAudioSource.dopplerLevel = 0f;
        damageAudioSource.spread = 0f;
        damageAudioSource.reverbZoneMix = 1f;
        damageAudioSource.loop = false;
    }

    // Assign the clip only once, after hitSound is loaded/set
    damageAudioSource.clip = hitSound;
}

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

        // Play sound
        if (hitSound != null)
        {
            damageAudioSource.PlayOneShot(hitSound);
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



}
