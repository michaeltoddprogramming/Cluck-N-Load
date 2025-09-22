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
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezeRotationY;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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
        Vector3 point;
        int attempts = 0;

        do
        {
            Bounds b = pane.bounds;
            point = new Vector3(
                Random.Range(b.min.x, b.max.x),
                b.center.y,
                Random.Range(b.min.z, b.max.z)
            );

            attempts++;
        } while (!pane.bounds.Contains(point) && attempts < 20);

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
            while (Vector3.Distance(new Vector3(rb.position.x, 0, rb.position.z),
                                    new Vector3(target.x, 0, target.z)) > stopThreshold)
            {
                Vector3 dir = (target - rb.position).normalized;
                dir.y = 0f; // ✅ no vertical movement

                Vector3 nextPos = rb.position + dir * speed * Time.fixedDeltaTime;
                nextPos.y = 0f; // ✅ force onto floor

                rb.MovePosition(nextPos);

                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            PickNewTarget();
        }
    }

}
