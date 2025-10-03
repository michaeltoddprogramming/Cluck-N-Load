// using System.Collections;
// using UnityEngine;

// [RequireComponent(typeof(Rigidbody))]
// public class CivilianUnit : MonoBehaviour
// {
//     [SerializeField] private CivilianData data;
//     [SerializeField] private Animator animator;

//     public CivilianSpawner spawner; // assign on spawn


//     public float speed;
//     public float minWait;
//     public float maxWait;
//     public float stopThreshold;
//     public float spawnY;

//     float stuckTimer = 0f;
//     float stuckThreshold = 3f; // seconds before picking a new target

//     private Rigidbody rb;
//     private Vector3 target;
//     private MeshCollider currentPane;
//     private bool isChoosingIdle = false;
    

//     void Awake()
//     {
//         rb = GetComponent<Rigidbody>();
//         animator = GetComponent<Animator>();

//         if (!TryGetComponent<Collider>(out _))
//         {
//             Debug.LogWarning($"{name} has no collider! Please add a BoxCollider or CapsuleCollider.");
//         }

//         // Freeze rotations, lock Y position
//         rb.constraints = RigidbodyConstraints.FreezeRotationX |
//                          RigidbodyConstraints.FreezeRotationY |
//                          RigidbodyConstraints.FreezeRotationZ |
//                          RigidbodyConstraints.FreezePositionY;
//         rb.useGravity = false;
//         rb.interpolation = RigidbodyInterpolation.Interpolate;
//         rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
//         rb.isKinematic = false;
//     }

//     public void Initialize(GameObject floor)
//     {
//         MeshCollider[] panels = floor.GetComponentsInChildren<MeshCollider>();
//         if (panels.Length == 0)
//         {
//             Debug.LogError("No walkable MeshColliders found!");
//             return;
//         }

//         foreach (var p in panels)
//         {
//             if (p.bounds.Contains(transform.position))
//             {
//                 currentPane = p;
//                 break;
//             }
//         }

//         if (currentPane == null)
//             currentPane = panels[Random.Range(0, panels.Length)];

//         speed = data.MovementSpeed;
//         minWait = data.minWait;
//         maxWait = data.maxWait;
//         stopThreshold = data.stopThreshold;
//         spawnY = transform.position.y;

//         PickNewTarget();
//         rb.MovePosition(new Vector3(transform.position.x, spawnY, transform.position.z));
//         StartCoroutine(WanderRoutine());
//     }

//     void Update()
//     {
//         // if (rb.linearVelocity.sqrMagnitude > 0.001f) // adjust threshold if needed
//         // {
//         //     animator.SetFloat("speed", 1f);
//         // }
//     }

//     void DespawnAndRespawn()
//     {
//         if (spawner != null)
//         {
//             // Tell spawner to remove this civilian
//             spawner.RemoveSpecificAnimal(gameObject);

//             // Spawn a new civilian using spawner
//             spawner.SpawnSingleAnimal();
//         }

//         Destroy(gameObject); // remove this one
//     }

//     Vector3 GetRandomPointOnPanel(MeshCollider pane)
//     {
//         Mesh mesh = pane.sharedMesh;
//         Vector3[] verts = mesh.vertices;
//         int[] tris = mesh.triangles;

//         for (int attempt = 0; attempt < 100; attempt++)
//         {
//             int triIndex = Random.Range(0, tris.Length / 3) * 3;
//             Vector3 v0 = pane.transform.TransformPoint(verts[tris[triIndex]]);
//             Vector3 v1 = pane.transform.TransformPoint(verts[tris[triIndex + 1]]);
//             Vector3 v2 = pane.transform.TransformPoint(verts[tris[triIndex + 2]]);

//             float r1 = Random.Range(0f, 1f);
//             float r2 = Random.Range(0f, 1f);
//             if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }

//             Vector3 point = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);
//             point.y = spawnY;
//             return point;
//         }

//         return rb.position;
//     }

//     void PickNewTarget()
//     {
//         animator.SetFloat("speed", 1f);
//         target = GetRandomPointOnPanel(currentPane);

//         // Rotate once to face new target
//         Vector3 dir = (target - transform.position).normalized;
//         dir.y = 0f; // keep rotation horizontal
//         if (dir.sqrMagnitude > 0.001f)
//             transform.rotation = Quaternion.LookRotation(dir);
//     }

// //     IEnumerator WanderRoutine()
// //     {
// //         while (true)
// //         {
// // #if UNITY_EDITOR
// //             Debug.DrawLine(rb.position, target, Color.red, 1f);
// // #endif
// //             Vector3 flatTarget = new Vector3(target.x, 0f, target.z);

// //             while (Vector3.Distance(new Vector3(rb.position.x, 0f, rb.position.z), flatTarget) > stopThreshold)
// //             {
// //                 Vector3 flatPos = new Vector3(rb.position.x, 0f, rb.position.z);
// //                 Vector3 moveDir = (flatTarget - flatPos).normalized;

// //                 float step = speed * Time.fixedDeltaTime;
// //                 Vector3 nextPos = rb.position + moveDir * step;
// //                 nextPos.y = spawnY;

// //                 Ray ray = new Ray(nextPos + Vector3.up * 5f, Vector3.down);
// //                 if (currentPane.Raycast(ray, out RaycastHit hit, 10f))
// //                 {
// //                     nextPos = hit.point;
// //                     nextPos.y = spawnY;

// //                     float distanceMoved = Vector3.Distance(rb.position, nextPos);
// //                     if (distanceMoved < 0.001f)
// //                         stuckTimer += Time.fixedDeltaTime; // not moving, increase timer
// //                     else
// //                         stuckTimer = 0f; // reset if moving

// //                     rb.MovePosition(nextPos);

// //                     // Set walking speed for animation
// //                     // animator.SetFloat("speed", speed);
// //                     float currentSpeed = ((nextPos - rb.position) / Time.fixedDeltaTime).magnitude;
// //                     // animator.SetFloat("speed", currentSpeed);

// //                     // Reset idle choice if moving
// //                     if (isChoosingIdle)
// //                     {
// //                         animator.SetBool("isFeeding", false);
// //                         animator.SetBool("isSleeping", false);
// //                         isChoosingIdle = false;
// //                     }
// //                 }
// //                 else
// //                 {
// //                     // PickNewTarget();
// //                     // break;

// //                     // If outside the pane, respawn
// //                     DespawnAndRespawn();
// //                     yield break;
// //                 }

// //                 yield return new WaitForFixedUpdate();
// //             }

// //             // Stopped → pick idle/eat/sleep
// //             animator.SetFloat("speed", 0f);
// //             if (!isChoosingIdle)
// //             {
// //                 isChoosingIdle = true;
// //                 StartCoroutine(PickIdleAnimation());
// //             }

// //             yield return new WaitForSeconds(Random.Range(minWait, maxWait));
// //             PickNewTarget();
// //         }
// //     }


// IEnumerator WanderRoutine()
// {
//     while (true)
//     {
//         Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
//         stuckTimer = 0f;

//         while (Vector3.Distance(new Vector3(rb.position.x, 0f, rb.position.z), flatTarget) > stopThreshold)
//         {
//             Vector3 moveDir = (flatTarget - new Vector3(rb.position.x, 0f, rb.position.z)).normalized;
//             Vector3 nextPos = rb.position + moveDir * speed * Time.fixedDeltaTime;
//             nextPos.y = spawnY;

//             if (currentPane.Raycast(new Ray(nextPos + Vector3.up * 5f, Vector3.down), out RaycastHit hit, 10f))
//             {
//                 rb.MovePosition(hit.point);

//                 // Use Rigidbody velocity to check movement
//                 if (rb.linearVelocity.sqrMagnitude < 0.001f)
//                     stuckTimer += Time.fixedDeltaTime;
//                 else
//                     stuckTimer = 0f;

//                 // Animator
//                 // animator.SetFloat("speed", rb.linearVelocity.magnitude);

//                 // Reset idle flags if moving
//                 if (rb.linearVelocity.magnitude > 0.01f && isChoosingIdle)
//                 {
//                     animator.SetBool("isFeeding", false);
//                     animator.SetBool("isSleeping", false);
//                     isChoosingIdle = false;
//                 }

//                 // Pick a new target if stuck too long
//                 if (stuckTimer > stuckThreshold)
//                 {
//                     PickNewTarget();
//                     flatTarget = new Vector3(target.x, 0f, target.z);
//                     stuckTimer = 0f;
//                 }
//             }
//             else
//             {
//                 DespawnAndRespawn();
//                 yield break;
//             }

//             yield return new WaitForFixedUpdate();
//         }

//         // Stopped → pick idle/eat/sleep
//         animator.SetFloat("speed", 0f);
//         if (!isChoosingIdle)
//         {
//             isChoosingIdle = true;
//             StartCoroutine(PickIdleAnimation());
//         }

//         yield return new WaitForSeconds(Random.Range(minWait, maxWait));
//         PickNewTarget();
//     }
// }




//     private IEnumerator PickIdleAnimation()
//     {

//         float rand = Random.value;
//         if (rand < 0.33f)
//         {
//             animator.SetBool("isFeeding", true);
//             animator.SetBool("isSleeping", false);
//         }
//         else if (rand < 0.66f)
//         {
//             animator.SetBool("isSleeping", true);
//             animator.SetBool("isFeeding", false);
//         }
//         else
//         {
//             animator.SetBool("isFeeding", false);
//             animator.SetBool("isSleeping", false);
//         }
//         yield return new WaitForSeconds(0.1f); // small delay
//     }

// #if UNITY_EDITOR
//     void OnDrawGizmos()
//     {
//         if (currentPane == null || !(currentPane is MeshCollider meshCol) || meshCol.sharedMesh == null)
//             return;

//         Gizmos.color = Color.green;
//         Mesh mesh = meshCol.sharedMesh;
//         Vector3[] vertices = mesh.vertices;
//         int[] triangles = mesh.triangles;

//         for (int i = 0; i < triangles.Length; i += 3)
//         {
//             Vector3 v0 = meshCol.transform.TransformPoint(vertices[triangles[i]]);
//             Vector3 v1 = meshCol.transform.TransformPoint(vertices[triangles[i + 1]]);
//             Vector3 v2 = meshCol.transform.TransformPoint(vertices[triangles[i + 2]]);

//             Gizmos.DrawLine(v0, v1);
//             Gizmos.DrawLine(v1, v2);
//             Gizmos.DrawLine(v2, v0);
//         }

//         Gizmos.color = Color.red;
//         Gizmos.DrawLine(rb.position, target);
//     }
// #endif
// }





using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CivilianUnit : MonoBehaviour
{
    [SerializeField] private CivilianData data;
    [SerializeField] private Animator animator;

    public CivilianSpawner spawner; // assign on spawn

    public float speed;
    public float minWait;
    public float maxWait;
    public float stopThreshold;
    public float spawnY;

    private float stuckTimer = 0f;
    private float stuckThreshold = 3f; // seconds before picking a new target

    private Rigidbody rb;
    private Vector3 target;
    private MeshCollider currentPane;
    private bool isChoosingIdle = false;
    private Vector3 lastPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (animator == null)
            animator = GetComponent<Animator>();

        if (!TryGetComponent<Collider>(out _))
        {
            Debug.LogWarning($"{name} has no collider! Please add a BoxCollider or CapsuleCollider.");
        }

        // Freeze rotations, lock Y position
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationY |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = false;
    }

    public void Initialize(GameObject floor)
    {
        MeshCollider[] panels = floor.GetComponentsInChildren<MeshCollider>();
        if (panels.Length == 0)
        {
            Debug.LogError("No walkable MeshColliders found!");
            return;
        }

        foreach (var p in panels)
        {
            if (p.bounds.Contains(transform.position))
            {
                currentPane = p;
                break;
            }
        }

        if (currentPane == null)
            currentPane = panels[Random.Range(0, panels.Length)];

        speed = data.MovementSpeed;
        minWait = data.minWait;
        maxWait = data.maxWait;
        stopThreshold = data.stopThreshold;
        spawnY = transform.position.y;

        lastPosition = transform.position;
        PickNewTarget();
        
        Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
        rb.position = spawnPos;
        transform.position = spawnPos;
        
        StartCoroutine(WanderRoutine());
    }

    void DespawnAndRespawn()
    {
        if (spawner != null)
        {
            spawner.RemoveSpecificAnimal(gameObject);
            spawner.SpawnSingleAnimal();
        }

        Destroy(gameObject);
    }

    Vector3 GetRandomPointOnPanel(MeshCollider pane)
    {
        Mesh mesh = pane.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            int triIndex = Random.Range(0, tris.Length / 3) * 3;
            Vector3 v0 = pane.transform.TransformPoint(verts[tris[triIndex]]);
            Vector3 v1 = pane.transform.TransformPoint(verts[tris[triIndex + 1]]);
            Vector3 v2 = pane.transform.TransformPoint(verts[tris[triIndex + 2]]);

            float r1 = Random.Range(0f, 1f);
            float r2 = Random.Range(0f, 1f);
            if (r1 + r2 > 1f) 
            { 
                r1 = 1f - r1; 
                r2 = 1f - r2; 
            }

            Vector3 point = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);
            point.y = spawnY;
            return point;
        }

        return rb.position;
    }

    void PickNewTarget()
    {
        if (animator != null)
            animator.SetFloat("speed", 1f);
            
        target = GetRandomPointOnPanel(currentPane);

        // Rotate to face new target
        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
            stuckTimer = 0f;

            while (Vector3.Distance(new Vector3(rb.position.x, 0f, rb.position.z), flatTarget) > stopThreshold)
            {
                Vector3 moveDir = (flatTarget - new Vector3(rb.position.x, 0f, rb.position.z)).normalized;
                Vector3 nextPos = rb.position + moveDir * speed * Time.fixedDeltaTime;
                nextPos.y = spawnY;

                Ray ray = new Ray(nextPos + Vector3.up * 5f, Vector3.down);
                
                if (currentPane.Raycast(ray, out RaycastHit hit, 10f))
                {
                    Vector3 hitPos = hit.point;
                    hitPos.y = spawnY;
                    
                    // Check if actually moved
                    float distMoved = Vector3.Distance(lastPosition, hitPos);
                    if (distMoved < 0.001f)
                        stuckTimer += Time.fixedDeltaTime;
                    else
                        stuckTimer = 0f;

                    lastPosition = hitPos;
                    rb.MovePosition(hitPos);

                    // Update animator
                    if (animator != null)
                    {
                        float currentSpeed = distMoved / Time.fixedDeltaTime;
                        animator.SetFloat("speed", currentSpeed);
                    }

                    // Reset idle flags if moving
                    if (distMoved > 0.01f && isChoosingIdle)
                    {
                        if (animator != null)
                        {
                            animator.SetBool("isFeeding", false);
                            animator.SetBool("isSleeping", false);
                        }
                        isChoosingIdle = false;
                    }

                    // Pick a new target if stuck too long
                    if (stuckTimer > stuckThreshold)
                    {
                        PickNewTarget();
                        flatTarget = new Vector3(target.x, 0f, target.z);
                        stuckTimer = 0f;
                    }
                }
                else
                {
                    DespawnAndRespawn();
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            // Reached target - stop and idle
            if (animator != null)
                animator.SetFloat("speed", 0f);
                
            if (!isChoosingIdle)
            {
                isChoosingIdle = true;
                StartCoroutine(PickIdleAnimation());
            }

            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            PickNewTarget();
        }
    }

    private IEnumerator PickIdleAnimation()
    {
        if (animator == null)
            yield break;

        float rand = Random.value;
        if (rand < 0.33f)
        {
            animator.SetBool("isFeeding", true);
            animator.SetBool("isSleeping", false);
        }
        else if (rand < 0.66f)
        {
            animator.SetBool("isSleeping", true);
            animator.SetBool("isFeeding", false);
        }
        else
        {
            animator.SetBool("isFeeding", false);
            animator.SetBool("isSleeping", false);
        }
        
        yield return new WaitForSeconds(0.1f);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (rb == null || currentPane == null)
            return;
            
        if (!(currentPane is MeshCollider meshCol) || meshCol.sharedMesh == null)
            return;

        Gizmos.color = Color.green;
        Mesh mesh = meshCol.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = meshCol.transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v1 = meshCol.transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v2 = meshCol.transform.TransformPoint(vertices[triangles[i + 2]]);

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v0);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(rb.position, target);
    }
#endif
}