using UnityEngine;

[CreateAssetMenu(fileName = "AnimalData", menuName = "Game/Animal Data")]
public class AnimalData : ScriptableObject
{
    public AnimalStructure.AnimalType animalType;
    public string targetAnimalType; // Should match animalType.ToString(), e.g., "Chicken"
    public int maxHealth = 100;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float attackRange = 2f;
    public float detectionRange = 10f;
    public int damage = 10;
    public float attackCooldown = 1f;
    public float moveSoundDelay = 2f;
    public float moveSoundChance = 0.5f;
    public float attackSoundChance = 0.8f;
    // New ambient sound fields
    public AudioClip[] ambientClips;
    public float ambientSoundDelayMin = 5f; // Minimum delay between ambient sounds
    public float ambientSoundDelayMax = 10f; // Maximum delay between ambient sounds
    public float ambientSoundChance = 0.7f; // Chance to play ambient sound
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    public AudioClip[] moveClips;
    public AudioClip[] attackClips;
    public AudioClip deathClip;
    public bool debugAttacks;
}