// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;

// public class ArmyAnimal : MonoBehaviour
// {
//     [Header("Animal Configuration")]
//     [SerializeField] private AnimalStructure.AnimalType animalType;
//     [SerializeField] private AnimalDatabase animalDatabase;
//     private AnimalData animalData;

//     [Header("Animation")]
//     [SerializeField] private Animator animator;
//     [SerializeField] private string walkAnimParam = "isWalking";
//     [SerializeField] private string attackAnimParam = "Attack";

//     [Header("Audio")]
//     [SerializeField] private AudioSource attackAudioSource;
//     [SerializeField] private AudioSource moveAudioSource;
//     [SerializeField] private AudioSource deathAudioSource;
//     [SerializeField] private AudioSource ambientAudioSource;

//     [Header("Movement Settings")]
//     [SerializeField] private float separationRadius = 1.5f; // Distance to keep from other animals
//     [SerializeField] private float separationForce = 2f; // Strength of separation push
//     [SerializeField] private float targetOffsetRadius = 1f; // Random offset for target positions
//     [SerializeField] private int maxAnimalsPerEnemy = 2; // Max animals targeting one enemy

//     // Internal variables
//     private Vector3 guardPosition;
//     private float guardRadius;
//     private Vector3 targetPosition;
//     private bool isMoving = false;
//     private float lastAttackTime;
//     private BarracksStructure barracks;
//     private float idleTimer;
//     private float idleInterval = 2f;
//     private bool isNightTime = false;
//     private bool isReturningToBarracks = false;
//     private GameObject currentEnemy;
//     private bool isAttackingEnemy = false;
//     private Coroutine attackCoroutine;
//     private float lastMoveSoundTime;
//     private int currentHealth;
//     private float nextAmbientSoundTime;

//     public AnimalStructure.AnimalType AnimalType => animalType;

//     private void Awake()
//     {
//         if (animalDatabase == null)
//         {
//             Debug.LogError($"CRITICAL ERROR: {name} has no AnimalDatabase assigned in Inspector");
//             Destroy(gameObject);
//             return;
//         }

//         animalData = animalDatabase.GetAnimalByType(animalType);
//         if (animalData == null)
//         {
//             Debug.LogError($"CRITICAL ERROR: {name} could not find AnimalData for type {animalType} in AnimalDatabase");
//             Destroy(gameObject);
//             return;
//         }

//         if (!animalData.targetAnimalType.Equals(animalType.ToString(), System.StringComparison.OrdinalIgnoreCase))
//         {
//             Debug.LogWarning($"Animal {name} type {animalType} does not match AnimalData targetAnimalType {animalData.targetAnimalType}");
//         }

//         AudioSource[] audioSources = GetComponents<AudioSource>();
//         if (audioSources.Length < 4)
//         {
//             Debug.LogError($"CRITICAL ERROR: {name} requires at least 4 AudioSource components, found {audioSources.Length}");
//             Destroy(gameObject);
//             return;
//         }

//         attackAudioSource = audioSources[0];
//         moveAudioSource = audioSources[1];
//         deathAudioSource = audioSources[2];
//         ambientAudioSource = audioSources[3];

//         ConfigureAudioSource(attackAudioSource, "Attack");
//         ConfigureAudioSource(moveAudioSource, "Move");
//         ConfigureAudioSource(deathAudioSource, "Death");
//         ConfigureAudioSource(ambientAudioSource, "Ambient");

//         attackAudioSource.clip = animalData.attackClips != null && animalData.attackClips.Length > 0 ? animalData.attackClips[0] : null;
//         moveAudioSource.clip = animalData.moveClips != null && animalData.moveClips.Length > 0 ? animalData.moveClips[0] : null;
//         deathAudioSource.clip = animalData.deathClip;

//         if (attackAudioSource.clip == null)
//             if (moveAudioSource.clip == null)
//             if (deathAudioSource.clip == null)
//             if (animalData.ambientClips == null || animalData.ambientClips.Length == 0)
//             if (animator == null)
//         {
//             animator = GetComponent<Animator>();
//             if (animator == null)
//                 Debug.LogWarning($"{name} No Animator assigned+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
//         }
//     }

//     private void ConfigureAudioSource(AudioSource source, string sourceName)
//     {
//         if (source == null) return;
//         source.playOnAwake = false;
//         source.loop = false;
//         source.spatialBlend = 1f;
//         source.volume = 1f;
//         source.minDistance = 1f;
//         source.maxDistance = 20f;
//         }

//     private void Start()
//     {
//         currentHealth = animalData.maxHealth;
//         guardPosition = transform.position;
//         guardRadius = 5f;
//         // MoveToFlag();
//         // SetTimeOfDay(true);
//         nextAmbientSoundTime = Time.time + Random.Range(animalData.ambientSoundDelayMin, animalData.ambientSoundDelayMax);
//         }

// //     // Helper method to safely set animator parameters without errors
//     private void SafeSetAnimatorBool(string paramName, bool value)
//     {
//         if (animator == null) return;

//         Debug.Log($"Setting Animator Bool: {paramName} to {value}*****************************************************************");

//         // Try both variations of the parameter name (lowercase and capitalized)
//         string[] paramVariations = { paramName, char.ToUpper(paramName[0]) + paramName.Substring(1) };

//         foreach (string variation in paramVariations)
//         {
//             // Check if parameter exists in the animator controller
//             foreach (AnimatorControllerParameter param in animator.parameters)
//             {
//                 if (param.name == variation && param.type == AnimatorControllerParameterType.Bool)
//                 {
//                     animator.SetBool(variation, value);
//                     Debug.Log($"Successfully set parameter: {variation}__________________________________________________________________________");
//                     return; // Successfully set parameter, exit
//                 }
//             }
//         }
//         // Silently skip if parameter doesn't exist in any variation - no error logging needed
//         Debug.LogWarning($"Animator parameter {paramName} not found.--------------------------------------------------------------------");
//     }

//     private void Update()
//     {
//         if (isReturningToBarracks)
//         {
//             // ReturnToBarracks();
//             return;
//         }

//         if (!isNightTime)
//         {
//             return;
//         }

//         if (barracks != null)
//         {
//             guardPosition = barracks.GetFlagPosition;
//         }

//         idleTimer -= Time.deltaTime;

//         if (!isAttackingEnemy && Time.time >= nextAmbientSoundTime)
//         {
//             // PlayAmbientSound();
//             nextAmbientSoundTime = Time.time + Random.Range(animalData.ambientSoundDelayMin, animalData.ambientSoundDelayMax);
//         }


//     }



//     // public void SetBarracks(BarracksStructure barracksStructure)
//     // {
//     //     barracks = barracksStructure;
//     //     guardPosition = barracks != null ? barracks.GetFlagPosition : transform.position;
//     //     if (barracks != null && barracks.StructureData != null && barracks.StructureData.protectionRadius > 0)
//     //     {
//     //         guardRadius = barracks.StructureData.protectionRadius;
//     //     }
//     //     else
//     //     {
//     //         guardRadius = 5f;
//     //         Debug.LogWarning($"BarracksStructure {barracks?.name} has no valid StructureData or protectionRadius, using default guardRadius={guardRadius}");
//     //     }
//     // }

//     // public void SetGuardPosition(Vector3 position, float radius)
//     // {
//     //     guardPosition = position;
//     //     guardRadius = radius;
//     //     if (isNightTime)
//     //         MoveToFlag();
//     // }

//     // public void SetTimeOfDay(bool isNight)
//     // {
//     //     isNightTime = isNight;
//     //     if (isNight)
//     //     {
//     //         gameObject.SetActive(true);
//     //         MoveToFlag();
//     //         isReturningToBarracks = false;
//     //         if (attackCoroutine != null)
//     //         {
//     //             StopCoroutine(attackCoroutine);
//     //             attackCoroutine = null;
//     //             isAttackingEnemy = false;
//     //             currentEnemy = null;
//     //         }
//     //         nextAmbientSoundTime = Time.time + Random.Range(animalData.ambientSoundDelayMin, animalData.ambientSoundDelayMax);
//     //         }
//     //     else
//     //     {
//     //         if (attackCoroutine != null)
//     //         {
//     //             StopCoroutine(attackCoroutine);
//     //             attackCoroutine = null;
//     //         }
//     //         isAttackingEnemy = false;
//     //         currentEnemy = null;
//     //         gameObject.SetActive(false);
//     //         isReturningToBarracks = false;
//     //         }
//     // }

//     // public void StartReturnToBarracks()
//     // {
//     //     if (barracks == null)
//     //     {
//     //         Destroy(gameObject);
//     //         return;
//     //     }

//     //     if (isNightTime)
//     //         return;

//     //     if (attackCoroutine != null)
//     //     {
//     //         StopCoroutine(attackCoroutine);
//     //         attackCoroutine = null;
//     //     }

//     //     isAttackingEnemy = false;
//     //     currentEnemy = null;

//     //     isReturningToBarracks = true;
//     //     targetPosition = barracks.transform.position;
//     //     isMoving = true;

//     //     SafeSetAnimatorBool(walkAnimParam, true);
//     // }

//     // private void ReturnToBarracks()
//     // {
//     //     if (!isReturningToBarracks || barracks == null)
//     //         return;

//     //     if (isNightTime)
//     //     {
//     //         isReturningToBarracks = false;
//     //         MoveToFlag();
//     //         return;
//     //     }

//     //     Vector3 direction = targetPosition - transform.position;
//     //     direction.y = 0;

//     //     if (direction.magnitude < 1.5f)
//     //     {
//     //         barracks.ReturnAnimal(this);
//     //         return;
//     //     }

//     //     Quaternion targetRotation = Quaternion.LookRotation(direction);
//     //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, animalData.rotationSpeed * Time.deltaTime);
//     //     transform.position += transform.forward * animalData.moveSpeed * Time.deltaTime;
//     //     // PlayMoveSound();
//     // }

//     // public void MoveToFlag()
//     // {
//     //     // Add random offset to flag position
//     //     Vector2 offset = Random.insideUnitCircle * targetOffsetRadius;
//     //     targetPosition = guardPosition + new Vector3(offset.x, 0, offset.y);
//     //     isMoving = true;
//     //     SafeSetAnimatorBool(walkAnimParam, true);
//     //     // PlayMoveSound();
//     //     }
// //             private void PlayAmbientSound()
// // {
// //     if (ambientAudioSource == null)
// //     {
// //         return; // Just skip ambient sound if no audio source
// //     }
// //     if (animalData.ambientClips == null || animalData.ambientClips.Length == 0)
// //     {
// //         return; // Just skip ambient sound if no clips
// //     }
// //     if (Random.value > animalData.ambientSoundChance)
// //         return;

// //     AudioClip clip = animalData.ambientClips[Random.Range(0, animalData.ambientClips.Length)];
// //     if (clip == null)
// //     {
// //         return; // Just skip if clip is null
// //     }

// //     ambientAudioSource.clip = clip;
// //     ambientAudioSource.pitch = Random.Range(animalData.minPitch, animalData.maxPitch);
// //     ambientAudioSource.Play();
// // }

//     private void OnDrawGizmos()
//     {
//         if (animalData == null) return;
//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(transform.position, animalData.attackRange);
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, animalData.detectionRange);
//         Gizmos.color = Color.blue;
//         Gizmos.DrawWireSphere(guardPosition, guardRadius);
//         if (isMoving)
//         {
//             Gizmos.color = Color.green;
//             Gizmos.DrawLine(transform.position, targetPosition);
//             Gizmos.DrawWireSphere(targetPosition, 0.2f); // Show target point
//         }
//         if (currentEnemy != null && currentEnemy.activeInHierarchy)
//         {
//             Gizmos.color = Color.magenta;
//             Gizmos.DrawLine(transform.position, currentEnemy.transform.position);
//         }
//         Gizmos.color = Color.cyan;
//         Gizmos.DrawWireSphere(transform.position, separationRadius); // Show separation radius
//     }
// }