using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class CivilianUnit : MonoBehaviour
{
    [Header("Pen setup")]
    public Transform floorParent;
    private Collider[] penAreas;

    [Header("Wander settings")]
    [SerializeField] private CivilianData data;
    public float speed;
    public float minWait;
    public float maxWait;
    public float stopThreshold;
    public float spawnY;

    private Rigidbody rb;
    private Vector3 target;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = false;
    }

    // public void Initialize(Transform floor)
    // {
    //     floorParent = floor;
    //     penAreas = floorParent.GetComponentsInChildren<Collider>();

    //     speed = data.MovementSpeed;
    //     minWait = data.minWait;
    //     maxWait = data.maxWait;
    //     stopThreshold = data.stopThreshold;
    //     spawnY = transform.position.y;

    //     PickNewTarget();
    //     StartCoroutine(WanderRoutine());
    // }
    // public void Initialize(Transform floor)
    // {
    //     floorParent = floor;
    //     penAreas = floorParent.GetComponentsInChildren<Collider>();

    //     speed = data.MovementSpeed;
    //     minWait = data.minWait;
    //     maxWait = data.maxWait;
    //     stopThreshold = data.stopThreshold;

    //     // Set unit exactly at the spawn location
    //     // transform.position = spawnPosition;

    //     // Store Y for movement
    //     spawnY = transform.position.y;

    //     PickNewTarget();
    //     StartCoroutine(WanderRoutine());
    // }

    public void Initialize(Transform floor)
    {
        floorParent = floor;
        penAreas = floorParent.GetComponentsInChildren<Collider>();

        speed = data.MovementSpeed;
        minWait = data.minWait;
        maxWait = data.maxWait;
        stopThreshold = data.stopThreshold;

        // Lock the spawn Y
        spawnY = transform.position.y;

        // Make sure Rigidbody uses the correct Y and constraints
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;

        // Force Rigidbody to spawn position immediately
        rb.position = new Vector3(transform.position.x, spawnY, transform.position.z);
        rb.MovePosition(rb.position); // ensures physics sees it at correct Y

        PickNewTarget();
        StartCoroutine(WanderRoutine());
    }








    // public void Initialize(Transform floor)
    // {
    //     floorParent = floor;
    //     penAreas = floorParent.GetComponentsInChildren<Collider>();

    //     speed = data.MovementSpeed;
    //     minWait = data.minWait;
    //     maxWait = data.maxWait;
    //     stopThreshold = data.stopThreshold;

    //     // Spawn exactly on top of panel 0 (or your chosen panel)
    //     Collider panel = penAreas[0];
    //     CapsuleCollider col = GetComponent<CapsuleCollider>();
    //     float panelTopY = panel.bounds.max.y + col.height / 2f;

    //     transform.position = new Vector3(panel.bounds.center.x, panelTopY, panel.bounds.center.z);
    //     spawnY = panelTopY; // now spawnY matches the panel top

    //     PickNewTarget();
    //     StartCoroutine(WanderRoutine());
    // }

    Vector3 GetRandomPointInPane(Collider pane)
    {
        return new Vector3(
            Random.Range(pane.bounds.min.x, pane.bounds.max.x),
            spawnY, // fixed vertical position
            Random.Range(pane.bounds.min.z, pane.bounds.max.z)
        );
    }



    // Vector3 GetRandomPointInPane(Collider pane)
    // {
    //     Bounds b = pane.bounds;
    //     // float y = b.max.y + GetComponent<CapsuleCollider>().height / 2f; // top of panel + half capsule
    //     return new Vector3(
    //         Random.Range(b.min.x, b.max.x),
    //         spawnY,
    //         Random.Range(b.min.z, b.max.z)
    //     );
    // }



    void PickNewTarget()
    {
        Collider pane = penAreas[Random.Range(0, penAreas.Length)];
        target = GetRandomPointInPane(pane);
        Debug.Log($"New target: {target}, Unit pos: {rb.position}");
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            while (Vector3.Distance(new Vector3(rb.position.x, 0f, rb.position.z),
                                    new Vector3(target.x, 0f, target.z)) > stopThreshold)
            {
                Vector3 flatPos = new Vector3(rb.position.x, 0f, rb.position.z);
                Vector3 flatTarget = new Vector3(target.x, 0f, target.z);
                Vector3 dir = (flatTarget - flatPos).normalized;

                // Rotate to face the target every frame
                if (dir.sqrMagnitude > 0f)
                    transform.rotation = Quaternion.LookRotation(dir);

                Vector3 nextPos = rb.position + dir * speed * Time.fixedDeltaTime;
                nextPos.y = spawnY; // keep Y locked
                rb.MovePosition(nextPos);

                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            PickNewTarget();
        }
    }
}
