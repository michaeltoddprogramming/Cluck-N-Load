// using UnityEngine;

// public class ChickenMovement : MonoBehaviour
// {
//     [Header("Setup")]
//     [SerializeField] private GameObject chickenPrefab;
//     [SerializeField] private float spawnHeight = 1f;
//     [SerializeField] private float speed = 10f;

//     private GridDataGenerator grid;

//     public void SpawnAndMove()
//     {
//         if (grid == null)
//             grid = FindObjectOfType<GridDataGenerator>();

//         // Top-left corner
//         Vector3 spawnPos = grid.GetCell(0, grid.GetGridHeight() - 1).worldPosition;
//         spawnPos.y += spawnHeight;

//         // Center of grid
//         int centerX = grid.GetGridWidth() / 2;
//         int centerY = grid.GetGridHeight() / 2;
//         Vector3 targetPos = grid.GetCell(centerX, centerY).worldPosition;
//         targetPos.y += spawnHeight;

//         // Spawn chicken
//         GameObject chicken = Instantiate(chickenPrefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));
//         Animator animator = chicken.GetComponent<Animator>();
//         animator.SetBool("isWalking", false);

//         // Start movement coroutine
//         StartCoroutine(MoveChicken(chicken, targetPos, animator));
//     }

//     private System.Collections.IEnumerator MoveChicken(GameObject chicken, Vector3 targetPos, Animator animator)
//     {
//         while (Vector3.Distance(chicken.transform.position, targetPos) > 0.1f)
//         {
//             chicken.transform.position = Vector3.MoveTowards(chicken.transform.position, targetPos, speed * Time.deltaTime);
//             yield return null;
//         }

//         animator.SetBool("isRunning", true);
//     }
// }



using UnityEngine;

public class ChickenMovement : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject chickenPrefab;
    [SerializeField] private float spawnHeight = 0.35f;
    [SerializeField] private float speed = 5f;

    public float rotation = 0f;

    // public int placement = 2;

    private GridDataGenerator grid;

    public Vector3 targetPos;

    Animator animator;
    GameObject chicken;

    public WolfMovement wolf;

 

    void Update()
    {
    }

    public void SpawnAndMove()
    {
        if (grid == null)
            grid = FindObjectOfType<GridDataGenerator>();

        // Center of grid (spawn point)
        int centerX = grid.GetGridWidth() / 2;
        int centerY = grid.GetGridHeight() / 2;
        Vector3 spawnPos = grid.GetCell(centerX, centerY).worldPosition;
        spawnPos.y += spawnHeight;

        // Set a new target (example: bottom-right corner)
        // Vector3 targetPos = grid.GetCell(centerX-1, centerY-1).worldPosition;
        targetPos = grid.GetCell(centerX+1, centerY-1).worldPosition;
        targetPos.y += spawnHeight;

        // Spawn chicken
        // GameObject chicken = Instantiate(chickenPrefab, spawnPos, Quaternion.Euler(0f, rotation, 0f));
        chicken = Instantiate(chickenPrefab, spawnPos, Quaternion.Euler(0f, rotation, 0f));
        // Animator animator = chicken.GetComponent<Animator>();
        // animator.SetBool("isWalking", true);
        animator = chicken.GetComponent<Animator>();
        animator.SetBool("isWalking", true);
        // animator.SetBool("isWalking", false);

        //shoot backwards
        // StartCoroutine(ShootnMove(chicken, targetPos, animator));
        // animator.SetBool("isWalking", false);

        
    }

    public void shoot()
    {
        //shoot
        GetComponent<AudioSource>().Play();
        StartCoroutine(delay(1.3f));
    }

    private System.Collections.IEnumerator delay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StartCoroutine(ShootnMove(chicken, targetPos, animator));
        animator.SetBool("isWalking", false);
        wolf.despawnWolf();
    }

    private System.Collections.IEnumerator ShootnMove(GameObject chicken, Vector3 targetPos, Animator animator)
    // private IEnumerator ShootnMove(GameObject chicken, Vector3 targetPos, Animator animator)
    {
        // animator.SetBool("isWalking", false);
        while (Vector3.Distance(chicken.transform.position, targetPos) > 0.1f)
        {
            chicken.transform.position = Vector3.MoveTowards(chicken.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        animator.SetBool("isWalking", true);
    }
}
