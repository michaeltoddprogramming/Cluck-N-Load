// using UnityEngine;
// using UnityEngine.UI;

// public class NightManager : MonoBehaviour
// {
//     [Header("Setup")]
//     [SerializeField] private GameObject wolfPrefab;
//     [SerializeField] private Button startNightButton;

//     public float spawnHeight = 1f;
//     public float speed = 10f;

//     public Light light;

//     private GridDataGenerator grid;
//     private GameObject spawnedWolf;
//     private Animator wolfAnimator;

//     private void Start()
//     {
//         grid = FindObjectOfType<GridDataGenerator>();
//         if (startNightButton != null)
//             startNightButton.onClick.AddListener(OnStartNight);
//     }

//     private void OnStartNight()
//     {
//         light.color = new Color(0.1f, 0.1f, 0.3f);
//         light.intensity = 0.3f;


//         Vector3 spawnPos = grid.GetCell(0, grid.GetGridHeight() - 1).worldPosition; // top-left
//         spawnPos.y += spawnHeight;

//         spawnedWolf = Instantiate(wolfPrefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));
//         wolfAnimator = spawnedWolf.GetComponent<Animator>();
//         wolfAnimator.SetBool("isRunning", false);

//         StartCoroutine(MoveWolfToBottomRight());
//     }

//     private System.Collections.IEnumerator MoveWolfToBottomRight()
//     {
//         // wolfAnimator.SetBool("isRunning", false);
//         Vector3 targetPos = grid.GetCell(grid.GetGridWidth() - 1, 0).worldPosition; // bottom-right

//         targetPos.y += spawnHeight;

//         // float speed = 10f;
//         while (Vector3.Distance(spawnedWolf.transform.position, targetPos) > 0.1f)
//         {
//             Debug.Log("Wolf is here");
//             spawnedWolf.transform.position = Vector3.MoveTowards(spawnedWolf.transform.position, targetPos, speed * Time.deltaTime);
//             yield return null;
//             // wolfAnimator.SetBool("isRunning", false);
//         }
//         Debug.Log("Wolf is here now");
//         wolfAnimator.SetBool("isRunning", true);

//     }
// }




// using UnityEngine;
// using UnityEngine.UI;

// public class NightManager : MonoBehaviour
// {
//     [Header("Setup")]
//     [SerializeField] private GameObject wolfPrefab;
//     [SerializeField] private Button startNightButton;
//     public float spawnHeight = 1f;
//     public float speed = 10f;
//     public Light light;

//     private GridDataGenerator grid;

//     private void Start()
//     {
//         grid = FindObjectOfType<GridDataGenerator>();
//         if (startNightButton != null)
//             startNightButton.onClick.AddListener(OnStartNight);
//     }

//     private void OnStartNight()
//     {
//         light.color = new Color(0.1f, 0.1f, 0.3f);
//         light.intensity = 0.3f;

//         Vector3 spawnPos = grid.GetCell(0, grid.GetGridHeight() - 1).worldPosition;
//         spawnPos.y += spawnHeight;

//         GameObject wolf = Instantiate(wolfPrefab, spawnPos, Quaternion.Euler(0f, 135f, 0f));

//         Vector3 targetPos = grid.GetCell(grid.GetGridWidth() - 1, 0).worldPosition;
//         targetPos.y += spawnHeight;

//         wolf.GetComponent<WolfMovement>().Setup(targetPos, speed);
//     }
// }





using UnityEngine;
using UnityEngine.UI;

public class NightManager : MonoBehaviour
{
    [SerializeField] private Button startNightButton;
    [SerializeField] private Light sceneLight;
    [SerializeField] private WolfMovement wolfMovement;
    [SerializeField] private ChickenMovement chicken;

    private void Start()
    {
        if (startNightButton != null)
            startNightButton.onClick.AddListener(StartNight);
    }

    private void StartNight()
    {
        // Set scene to night
        sceneLight.color = new Color(0.1f, 0.1f, 0.3f);
        sceneLight.intensity = 0.3f;

        // Tell the WolfMovement to spawn and start moving
        wolfMovement.SpawnAndMoveWolf();
        chicken.SpawnAndMove();
    }
}
