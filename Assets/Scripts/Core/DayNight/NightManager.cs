using UnityEngine;
using UnityEngine.UI;

public class NightManager : MonoBehaviour
{
    [SerializeField] private Button startNightButton;
    [SerializeField] private Light sceneLight;
    [SerializeField] private WolfMovement wolfMovement;
    [SerializeField] private ChickenMovement chicken;

    private AudioSource source1;
    private AudioSource source2;

    private void Start()
    {
        if (startNightButton != null)
            startNightButton.onClick.AddListener(StartNight);

        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length >= 2)
        {
            source1 = sources[0];
            source2 = sources[1];
        }
    }

    private void StartNight()
    {
        //stop daytime song and play night time
        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        // GetComponent<AudioSource>().Play();


        // Set scene to night
        sceneLight.color = new Color(0.1f, 0.1f, 0.3f);
        sceneLight.intensity = 0.3f;

        // Tell the WolfMovement to spawn and start moving
        wolfMovement.SpawnAndMoveWolf();
        chicken.SpawnAndMove();
    }
}
