// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class NightManager : MonoBehaviour
// {
//     [SerializeField] private Button startNightButton;
//     [SerializeField] private TextMeshProUGUI buttonText;
//     [SerializeField] private Light sceneLight;
//     [SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
//     [SerializeField] private float intensity = 0.3f;
//     [SerializeField] private Gradient dayToNightGradient;
//     [SerializeField] private Gradient nightToDayGradient;
//     [SerializeField] private WolfMovement wolfMovement;
//     [SerializeField] private ChickenMovement chicken;
//     [SerializeField] private Texture2D skyboxDay;
//     [SerializeField] private Texture2D skyboxNight;
//     private AudioSource source1;
//     private AudioSource source2;

//     private bool isDay = true;

//     private void Start()
//     {

//         SetListeners();


//         if (startNightButton != null && isDay == true)
//         {
//             Debug.Log("This will be night time now");
//             Debug.Log("This will be night time now");
//             color = new Color32(214, 239, 255, 255);
//             intensity = 0.3f;
//             // startNightButton.onClick.AddListener(StartNight);
//         }
//         // else if(startNightButton != null && isDay == false)
//         // {
//         //     Debug.Log("This will be day time now");
//         //     startNightButton.onClick.AddListener(StartDay);
//         // }

//         AudioSource[] sources = GetComponents<AudioSource>();

//         if (sources.Length >= 2)
//         {
//             source1 = sources[0];
//             source2 = sources[1];
//         }
//     }

//     void Update()
//     {
//         // if (startNightButton != null && isDay == true)
//         // {
//         //     Debug.Log("This will be night time now");
//         //     color = new Color32(214, 239, 255, 255);
//         //     intensity = 0.3f;
//         //     // startNightButton.onClick.AddListener(StartNight);
//         // }
//         if(startNightButton != null && isDay == false)
//         {
//             Debug.Log("This will be day time now");
//             color = new Color32(255, 246, 225, 255);
//             intensity = 1f;
//             // startNightButton.onClick.AddListener(StartDay);
//         }

//         // changeLightColor();

//     }

//     private void SetListeners()
//     {
//         startNightButton.onClick.RemoveAllListeners();

//         if(isDay)
//         {
//             startNightButton.onClick.AddListener(StartNight);
//         }
//         else
//         {
//             startNightButton.onClick.AddListener(StartDay);
//         }
//     }

//     private void StartNight()
//     {
//         //change bool isDay to false
//         isDay = false;

//         //set button text to End Night
//         buttonText.text = "End Night";



//         //change skybox day to night over 10 seconds
//         StartCoroutine(Skybox(skyboxDay, skyboxNight, 10f));
//         StartCoroutine(LightingChanges(dayToNightGradient, 10f));



//         //stop daytime song and play night time
//         if (source1 != null) source1.Play();
//         if (source2 != null) source2.Stop();

//         // GetComponent<AudioSource>().Play();


//         // Set scene to night
//         // sceneLight.color = color;
//         // sceneLight.intensity = 0.3f;
//         changeLightColor();

//         // Tell the WolfMovement to spawn and start moving
//         wolfMovement.SpawnAndMoveWolf();
//         chicken.SpawnAndMove();
//     }

//     private void StartDay()
//     {
//         //change bool isDay to true
//         isDay = true;

//         //set button text to Start Night
//         buttonText.text = "Start Night";

//         //change skybox night to day over 10 seconds
//         StartCoroutine(Skybox(skyboxNight, skyboxDay, 10f));
//         StartCoroutine(LightingChanges(nightToDayGradient, 10f));



//         //stop night time song and play day time
//         if (source1 != null) source1.Stop();
//         if (source2 != null) source2.Play();


//         // Set scene to day
//         changeLightColor();

//         // despawn wolf and chicken
//         wolfMovement.despawn();
//         chicken.despawn();
//     }

//     private IEnumerator Skybox(Texture2D a, Texture2D b, float time)
//     {
//         RenderSettings.skybox.SetTexture("_Texture1", a);
//         RenderSettings.skybox.SetTexture("_Texture2", b);
//         RenderSettings.skybox.SetFloat("_Blend", 0);

//         for(float k = 0; k < time; k ++)
//         {
//             RenderSettings.skybox.SetFloat("_Blend", k / time);
//             yield return null; 
//         }

//         RenderSettings.skybox.SetTexture("_Texture1", b);
//     }

//     private IEnumerator LightingChanges(Gradient lightGradient, float time)
//     {
//         for(float k = 0; k < time; k ++)
//         {
//             sceneLight.color = lightGradient.Evaluate(k / time);

//             yield return null; 
//         }
//     }

//     private void changeLightColor()
//     {
//         sceneLight.color = color;
//         // sceneLight.color = ;
//         sceneLight.intensity = intensity;
//     }
// }



using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NightManager : MonoBehaviour
{
    [SerializeField] private Button startNightButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Light sceneLight;
    [SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
    [SerializeField] private float intensity = 0.3f;
    [SerializeField] private Gradient dayToNightGradient;
    [SerializeField] private Gradient nightToDayGradient;
    [SerializeField] private WolfMovement wolfMovement;
    [SerializeField] private ChickenMovement chicken;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxNight;

    private AudioSource source1;
    private AudioSource source2;

    private bool isDay = true;

    private void Start()
    {
        SetListeners();
        SetInitialLight();

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            source1 = sources[0];
            source2 = sources[1];
        }
    }

    private void SetInitialLight()
    {
        if (isDay)
        {
            Debug.Log("Daytime setup");
            color = new Color32(214, 239, 255, 0);
            intensity = 1f;
            buttonText.text = "Start Night";
        }
        else
        {
            Debug.Log("Nighttime setup");
            color = new Color32(255, 246, 225, 0);
            intensity = 0.3f;
            buttonText.text = "End Night";
        }
        changeLightColor();
    }

    private void SetListeners()
    {
        startNightButton.onClick.RemoveAllListeners();

        if (isDay)
        {
            startNightButton.onClick.AddListener(StartNight);
        }
        else
        {
            startNightButton.onClick.AddListener(StartDay);
        }
    }

    private void StartNight()
    {
        isDay = false;
        buttonText.text = "End Night";
        SetListeners();

        StartCoroutine(Skybox(skyboxDay, skyboxNight, 10f));
        // StartCoroutine(LightingChanges(dayToNightGradient, 10f));

        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        // color = new Color32(255, 246, 225, 255);
        // intensity = 0.3f;
        changeLightColor();

        wolfMovement.SpawnAndMoveWolf();
        chicken.SpawnAndMove();
    }

    private void StartDay()
    {
        isDay = true;
        buttonText.text = "Start Night";
        SetListeners();

        StartCoroutine(Skybox(skyboxNight, skyboxDay, 10f));
        // StartCoroutine(LightingChanges(nightToDayGradient, 10f));

        if (source1 != null) source1.Stop();
        if (source2 != null) source2.Play();

        // color = new Color32(214, 239, 255, 255);
        // intensity = 1f;
        changeLightColor();

        wolfMovement.despawn();
        chicken.despawn();
    }

    private IEnumerator Skybox(Texture2D a, Texture2D b, float time)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        RenderSettings.skybox.SetFloat("_Blend", 0);

        for (float k = 0; k < time; k += Time.deltaTime)
        {
            RenderSettings.skybox.SetFloat("_Blend", k / time);
            yield return null;
        }

        RenderSettings.skybox.SetTexture("_Texture1", b);
    }

    private IEnumerator LightingChanges(Gradient lightGradient, float time)
    {
        for (float k = 0; k < time; k += Time.deltaTime)
        {
            sceneLight.color = lightGradient.Evaluate(k / time);
            yield return null;
        }
    }

    private void changeLightColor()
    {
        sceneLight.color = color;
        sceneLight.intensity = intensity;
    }
}


// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class NightManager : MonoBehaviour
// {
//     [SerializeField] private Button startNightButton;
//     [SerializeField] private TextMeshProUGUI buttonText;

//     [SerializeField] private Light sceneLight;
//     [SerializeField] private Color dayColor = Color.white;
//     [SerializeField] private float dayIntensity = 1f;
//     [SerializeField] private Color nightColor = new Color32(50, 60, 80, 255);
//     [SerializeField] private float nightIntensity = 0.3f;

//     [SerializeField] private Texture2D skyboxDay;
//     [SerializeField] private Texture2D skyboxNight;

//     [SerializeField] private WolfMovement wolfMovement;
//     [SerializeField] private ChickenMovement chicken;

//     private bool isDay = true;

//     private void Start()
//     {
//         SetDay(); // Start the game in day mode
//         startNightButton.onClick.AddListener(ToggleDayNight);
//     }

//     private void ToggleDayNight()
//     {
//         if (isDay)
//         {
//             SetNight();
//         }
//         else
//         {
//             SetDay();
//         }
//     }

//     private void SetDay()
//     {
//         isDay = true;
//         RenderSettings.skybox = skyboxDay;
//         sceneLight.color = dayColor;
//         sceneLight.intensity = dayIntensity;

//         buttonText.text = "Start Night";

//         wolfMovement.despawn();
//         chicken.despawn();
//     }

//     private void SetNight()
//     {
//         isDay = false;
//         RenderSettings.skybox = skyboxNight;
//         sceneLight.color = nightColor;
//         sceneLight.intensity = nightIntensity;

//         buttonText.text = "End Night";

//         wolfMovement.SpawnAndMoveWolf();
//         chicken.SpawnAndMove();
//     }
// }
