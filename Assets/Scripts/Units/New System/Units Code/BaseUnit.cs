using UnityEngine;

public abstract class BaseUnit : MonoBehaviour
{
    protected AudioSource audioSource;
    // protected int currentHealth;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // public virtual void TakeDamage(int damage)
    // {
    //     currentHealth -= damage;
    //     if (currentHealth > 0)
    //     {
    //         PlaySound(GetData().HurtSound);
    //     }
    //     else
    //     {
    //         PlaySound(GetData().DeathSound);
    //         Die();
    //     }
    // }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    // Each child class must return its own data type
    protected abstract UnitData GetData();
}
