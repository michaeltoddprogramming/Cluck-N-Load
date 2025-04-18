using UnityEngine;

public class WolfMovement : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject wolfPrefab;
    [SerializeField] private float spawnHeight = 0.25f;
    [SerializeField] private float speed = 10f;

    private GridDataGenerator grid;
    public ChickenMovement chicken;
    GameObject wolf;

    

    public void SpawnAndMoveWolf()
    {
        if (grid == null)
            grid = FindObjectOfType<GridDataGenerator>();

        // Top-left corner
        Vector3 spawnPos = grid.GetCell(0, grid.GetGridHeight() - 1).worldPosition;
        spawnPos.y += spawnHeight;

        // Center of grid
        int centerX = grid.GetGridWidth() / 2 - 2;
        int centerY = grid.GetGridHeight() / 2 + 1;
        Vector3 targetPos = grid.GetCell(centerX, centerY).worldPosition;
        targetPos.y += spawnHeight;

        // Spawn wolf
        wolf = Instantiate(wolfPrefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));
        Animator animator = wolf.GetComponent<Animator>();
        animator.SetBool("isRunning", false);

        // Start movement coroutine
        StartCoroutine(MoveWolf(wolf, targetPos, animator));
    }

    private System.Collections.IEnumerator MoveWolf(GameObject wolf, Vector3 targetPos, Animator animator)
    {
        if(wolf == null)
        {
            yield break;
        }


        while (wolf != null && Vector3.Distance(wolf.transform.position, targetPos) > 0.1f)
        {
            wolf.transform.position = Vector3.MoveTowards(wolf.transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        if(wolf != null)
        {
            chicken.shoot();
            animator.SetBool("isRunning", true);
        }
        // Destroy(wolf, 0.5f);
    }

    public void kill()
    {
        GetComponent<AudioSource>().Play();
        Destroy(wolf);
    }
    public void despawn()
    {
        Destroy(wolf);
    }
}
