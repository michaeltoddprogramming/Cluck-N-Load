using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class CivilianUnit : MonoBehaviour
{
    [Header("Pen setup")]
    public Transform floorParent; // Floor object of the pen prefab
    private Collider[] penAreas;

    [Header("Wander settings")]
    public float speed = 2f;
    public float minWait = 1f;
    public float maxWait = 3f;
    public float stopThreshold = 0.05f; // distance to consider "arrived"

    private Rigidbody rb;
    private Vector3 target;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Allow Y rotation so the animal can face its direction
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = false;

        // get all colliders inside the floor object
        penAreas = floorParent.GetComponentsInChildren<Collider>();
    }

    void Start()
    {
        PickNewTarget();
        StartCoroutine(WanderRoutine());
    }

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
