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

    public void Initialize(Transform floor)
    {
        floorParent = floor;
        penAreas = floorParent.GetComponentsInChildren<Collider>();

        speed = data.MovementSpeed;
        minWait = data.minWait;
        maxWait = data.maxWait;
        stopThreshold = data.stopThreshold;
        spawnY = transform.position.y;

        PickNewTarget();
        StartCoroutine(WanderRoutine());
    }

    // void Start()
    // {
    //     float speed = data.MovementSpeed;
    //     float minWait = data.minWait;
    //     float maxWait = data.maxWait;
    //     float stopThreshold = data.stopThreshold;
    //     PickNewTarget();
    //     StartCoroutine(WanderRoutine());
    // }

    Vector3 GetRandomPointInPane(Collider pane)
    {
        Bounds b = pane.bounds;
        Vector3 point = new Vector3(
            Random.Range(b.min.x, b.max.x),
            0f, // force Y = 0
            Random.Range(b.min.z, b.max.z)
        );
        return point;
    }

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
                nextPos.y = 0f; // keep Y locked
                rb.MovePosition(nextPos);

                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            PickNewTarget();
        }
    }
}
