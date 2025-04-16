// using UnityEngine;

// public class WolfMovement : MonoBehaviour
// {
//     private Animator animator;
//     private Vector3 targetPosition;
//     private float speed;
//     private bool isMoving = false;

//     public void Setup(Vector3 targetPos, float moveSpeed)
//     {
//         animator = GetComponent<Animator>();
//         targetPosition = targetPos;
//         speed = moveSpeed;

//         animator.SetBool("isRunning", true);
//         isMoving = true;
//     }

//     private void Update()
//     {
//         light.color = new Color(0.1f, 0.1f, 0.3f);
//         light.intensity = 0.3f;

//         Vector3 spawnPos = grid.GetCell(0, grid.GetGridHeight() - 1).worldPosition;
//         spawnPos.y += spawnHeight;

//         GameObject wolf = Instantiate(wolfPrefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));

//         // Calculate center cell for target
//         int centerX = grid.GetGridWidth() / 2;
//         int centerY = grid.GetGridHeight() / 2;
//         Vector3 targetPos = grid.GetCell(centerX, centerY).worldPosition;
//         targetPos.y += spawnHeight;

//         // Setup wolf movement
//         wolf.GetComponent<WolfMovement>().Setup(targetPos, speed);
//     }
// }


using UnityEngine;

public class WolfMovement : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private float spawnHeight = 1f;
    [SerializeField] private float speed = 10f;

    private GridDataGenerator grid;

    public void SpawnAndMoveWolf()
    {
        if (grid == null)
            grid = FindObjectOfType<GridDataGenerator>();

        // Top-left corner
        Vector3 spawnPos = grid.GetCell(0, grid.GetGridHeight() - 1).worldPosition;
        spawnPos.y += spawnHeight;

        // Center of grid
        int centerX = grid.GetGridWidth() / 2;
        int centerY = grid.GetGridHeight() / 2;
        Vector3 targetPos = grid.GetCell(centerX, centerY).worldPosition;
        targetPos.y += spawnHeight;

        // Spawn wolf
        GameObject wolf = Instantiate(wolfPrefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));
        Animator animator = wolf.GetComponent<Animator>();
        animator.SetBool("isRunning", false);

        // Start movement coroutine
        StartCoroutine(MoveWolf(wolf, targetPos, animator));
    }

    private System.Collections.IEnumerator MoveWolf(GameObject wolf, Vector3 targetPos, Animator animator)
    {
        while (Vector3.Distance(wolf.transform.position, targetPos) > 0.1f)
        {
            wolf.transform.position = Vector3.MoveTowards(wolf.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        animator.SetBool("isRunning", true);
    }
}
