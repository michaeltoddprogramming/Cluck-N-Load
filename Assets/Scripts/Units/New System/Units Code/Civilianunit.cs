using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class CivilianUnit : MonoBehaviour
{
    [SerializeField] private CivilianData data;
    [SerializeField] private Animator animator;

    public float speed;
    public float minWait;
    public float maxWait;
    public float stopThreshold;
    public float spawnY;

    private Rigidbody rb;
    private Vector3 target;
    private MeshCollider currentPane;
    private bool isChoosingIdle = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

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

        PickNewTarget();
        rb.MovePosition(new Vector3(transform.position.x, spawnY, transform.position.z));
        StartCoroutine(WanderRoutine());
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
            if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }

            Vector3 point = v0 + r1 * (v1 - v0) + r2 * (v2 - v0);
            point.y = spawnY;
            return point;
        }

        return rb.position;
    }

    void PickNewTarget()
    {
        target = GetRandomPointOnPanel(currentPane);

        // Rotate once to face new target
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f; // keep rotation horizontal
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
#if UNITY_EDITOR
            Debug.DrawLine(rb.position, target, Color.red, 1f);
#endif
            Vector3 flatTarget = new Vector3(target.x, 0f, target.z);

            while (Vector3.Distance(new Vector3(rb.position.x, 0f, rb.position.z), flatTarget) > stopThreshold)
            {
                Vector3 flatPos = new Vector3(rb.position.x, 0f, rb.position.z);
                Vector3 moveDir = (flatTarget - flatPos).normalized;

                float step = speed * Time.fixedDeltaTime;
                Vector3 nextPos = rb.position + moveDir * step;
                nextPos.y = spawnY;

                Ray ray = new Ray(nextPos + Vector3.up * 5f, Vector3.down);
                if (currentPane.Raycast(ray, out RaycastHit hit, 10f))
                {
                    nextPos = hit.point;
                    nextPos.y = spawnY;

                    rb.MovePosition(nextPos);

                    // Set walking speed for animation
                    animator.SetFloat("speed", speed);

                    // Reset idle choice if moving
                    if (isChoosingIdle)
                    {
                        animator.SetBool("isFeeding", false);
                        animator.SetBool("isSleeping", false);
                        isChoosingIdle = false;
                    }
                }
                else
                {
                    PickNewTarget();
                    break;
                }

                yield return new WaitForFixedUpdate();
            }

            // Stopped → pick idle/eat/sleep
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
        yield return new WaitForSeconds(0.1f); // small delay

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
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (currentPane == null || !(currentPane is MeshCollider meshCol) || meshCol.sharedMesh == null)
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
