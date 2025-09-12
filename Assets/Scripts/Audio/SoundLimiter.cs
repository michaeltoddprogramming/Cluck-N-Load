using UnityEngine;
using System.Collections;

public class SoundLimiter : MonoBehaviour
{
    public AudioSource source;

    [Header("Global Manager")]
    public SoundLimit globalManager; // must have maxAttack, maxHurt, maxDeath

    private static int currentAttackSounds = 0;
    private static int currentHurtSounds = 0;
    private static int currentDeathSounds = 0;

    // Call this for attack
    public void PlayAttack(AudioClip clip)
    {
        PlaySound(clip, globalManager.maxAttackSounds, "Attack");
    }

    // Call this for hurt
    public void PlayHurt(AudioClip clip)
    {
        PlaySound(clip, globalManager.maxHurtSounds, "Hurt");
    }

    // Call this for death
    // public void PlayDeath(AudioClip clip)
    // {

    //     if (clip == null || currentDeathSounds >= globalManager.maxDeathSounds) return;

    //     currentDeathSounds++;

    //     // Keep 3D spatial audio by instantiating a copy of the AudioSource
    //     AudioSource tempSource = Instantiate(source, transform.position, transform.rotation);
    //     tempSource.spatialBlend = source.spatialBlend;
    //     tempSource.minDistance = source.minDistance;
    //     tempSource.maxDistance = source.maxDistance;

    //     tempSource.PlayOneShot(clip);

    //     // Track limiter and destroy temp AudioSource after clip
    //     StartCoroutine(TrackSound("Death", clip.length, tempSource));
    //     // PlaySound(clip, globalManager.maxDeathSounds, "Death");
    // }

    public void PlayDeath(AudioClip clip)
    {
        if (clip == null || currentDeathSounds >= globalManager.maxDeathSounds) return;

        currentDeathSounds++;

        // Create a completely independent GameObject at the animal's position
        GameObject tempGO = new GameObject("DeathSound");
        tempGO.transform.position = transform.position;

        // Add an AudioSource to it
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        tempSource.spatialBlend = 1f; // 3D sound
        tempSource.minDistance = source.minDistance;
        tempSource.maxDistance = source.maxDistance;
        tempSource.volume = source.volume;

        // Play the clip
        tempSource.PlayOneShot(clip);

        Destroy(tempGO, clip.length);

        // Destroy the temp object after the clip finishes
        StartCoroutine(TrackSound("Death", clip.length, tempSource));
    }

    private void PlaySound(AudioClip clip, int max, string type)
    {
        if (clip == null) return;

        switch (type)
        {
            case "Attack":
                if (currentAttackSounds >= max) return;
                currentAttackSounds++;
                source.PlayOneShot(clip); // better than clip + Play() for overlapping
                StartCoroutine(TrackSound("Attack", clip.length));
                break;

            case "Hurt":
                if (currentHurtSounds >= max) return;
                currentHurtSounds++;
                source.PlayOneShot(clip);
                StartCoroutine(TrackSound("Hurt", clip.length));
                break;

                // case "Death":
                //     if (currentDeathSounds >= max) return;
                //     currentDeathSounds++;
                //     source.PlayOneShot(clip);
                //     StartCoroutine(TrackSound("Death", clip.length));
                //     break;
        }
    }

    private IEnumerator TrackSound(string type, float duration, AudioSource tempSource = null)
    {
        yield return new WaitForSeconds(duration);

        switch (type)
        {
            case "Attack": currentAttackSounds = Mathf.Max(0, currentAttackSounds - 1); break;
            case "Hurt": currentHurtSounds = Mathf.Max(0, currentHurtSounds - 1); break;
            case "Death": currentDeathSounds = Mathf.Max(0, currentDeathSounds - 1); break;
        }

        if (tempSource != null)
            Destroy(tempSource.gameObject);
    }
}
