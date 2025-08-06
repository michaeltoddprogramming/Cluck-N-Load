using UnityEngine;

public abstract class BaseUnit : MonoBehaviour
{
    [SerializeField] public AudioSource soundEffectSource;
    [SerializeField] public AudioSource backgroundAudioSource;
    [SerializeField] private Animator animator;

    // protected int currentHealth;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();       
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && soundEffectSource != null)
            soundEffectSource.PlayOneShot(clip);
    }
    protected void PlayBackgroundSound(AudioClip clip)
    {
        if (clip != null && backgroundAudioSource != null)
            backgroundAudioSource.clip = clip;
            backgroundAudioSource.loop = true;
            backgroundAudioSource.Play();
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
