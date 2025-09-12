using UnityEngine;
using System.Collections;

public abstract class BaseUnit : MonoBehaviour
{
    [SerializeField] public AudioSource soundEffectSource;
    [SerializeField] public AudioSource backgroundAudioSource;
    [SerializeField] private Animator animator;

    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 3f;
    [SerializeField] private float minVolume = 0.8f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;



    private SoundLimiter limiter;

    // protected int currentHealth;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        limiter = GetComponent<SoundLimiter>();
    }

    protected void PlaySound(AudioClip clip, char type)
    {
        if (clip != null && soundEffectSource != null)
        {
            if (type == 'a') // attack
                limiter.PlayAttack(clip);
            else if (type == 'h') // hurt
                limiter.PlayHurt(clip);
            else if (type == 'd') // death
                limiter.PlayDeath(clip);
            // soundEffectSource.PlayOneShot(clip);
            else if (type == 's') // simple sound, no limit
                soundEffectSource.PlayOneShot(clip);
        }
    }
    protected void PlayBackgroundSound(AudioClip clip)
    {
        if (clip != null && backgroundAudioSource != null)
            StartCoroutine(PlayBackgroundClipRandomly(clip));
    }

    private IEnumerator PlayBackgroundClipRandomly(AudioClip clip)
    {
        while (true)
        {
            // Randomize pitch and volume
            float targetVolume = Random.Range(minVolume, maxVolume);
            backgroundAudioSource.pitch = Random.Range(minPitch, maxPitch);

            backgroundAudioSource.clip = clip;
            backgroundAudioSource.volume = 0f;
            backgroundAudioSource.Play();

            // Fade in
            float fadeInTime = 0.5f;
            for (float t = 0; t < fadeInTime; t += Time.deltaTime)
            {
                backgroundAudioSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeInTime);
                yield return null;
            }
            backgroundAudioSource.volume = targetVolume;

            // Wait for clip duration minus fade times
            float delay = clip.length - fadeInTime;
            yield return new WaitForSeconds(delay);

            // Fade out
            float fadeOutTime = 0.5f;
            for (float t = 0; t < fadeOutTime; t += Time.deltaTime)
            {
                backgroundAudioSource.volume = Mathf.Lerp(targetVolume, 0f, t / fadeOutTime);
                yield return null;
            }
            backgroundAudioSource.Stop();

            // Wait random delay before next loop
            float randomDelay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(randomDelay);
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    protected void PlayAnimation(string animName)
    {
        if (animator != null)
        {
            animator.Play(animName);
        }
    }

    protected void SetBool(string name, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(name, value);
        }
    }

    protected void SetTrigger(string name)
    {
        if (animator != null)
        {
            animator.SetTrigger(name);
        }
    }

    protected void SetFloat(string name, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(name, value);
        }
    }

    // Each child class must return its own data type
    protected abstract UnitData GetData();
}
