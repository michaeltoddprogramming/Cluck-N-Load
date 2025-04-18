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

    [SerializeField] private Image timeOfDayIcon; // Reference to the UI Image for the icon
    [SerializeField] private Sprite dayIcon; // Icon for daytime
    [SerializeField] private Sprite nightIcon; // Icon for nighttime

    private AudioSource source1;
    private AudioSource source2;

    private bool isDay = true;

    private void Start()
    {
        // SetListeners();
        SetInitialLight();

        StartDay();

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            source1 = sources[0];
            source2 = sources[1];
        }
    }

    public void buttonClicked()
    {
        if (isDay)
        {
            Debug.Log("Gonna start night now");
            color = new Color32(50, 70, 100, 255);
            intensity = 0.3f;
            buttonText.text = "Start Night";

            changeLightColor();
            StartNight();
        }
        else
        {
            Debug.Log("Gonna start the day now");
            color = new Color32(255, 244, 214, 255);
            intensity = 2f;
            buttonText.text = "End Night";
            changeLightColor();
            StartDay();
        }

    }








    private void SetInitialLight()
    {
        if (isDay)
        {
            Debug.Log("Daytime setup");
            // color = new Color32(255, 244, 214, 255);
            color = new Color32(255, 244, 214, 255);
            intensity = 2f;
            buttonText.text = "Start Night";
        }
        else
        {
            Debug.Log("Nighttime setup");
            color = new Color32(50, 70, 100, 255);
            intensity = 0.3f;
            buttonText.text = "End Night";
        }
        changeLightColor();
    }

    // private void SetListeners()
    // {
    //     startNightButton.onClick.RemoveAllListeners();

    //     if (isDay)
    //     {
    //         startNightButton.onClick.AddListener(StartNight);
    //     }
    //     else
    //     {
    //         startNightButton.onClick.AddListener(StartDay);
    //     }
    // }

    private void StartNight()
    {
        isDay = false;
        buttonText.text = "End Night";
        // SetListeners();

        StartCoroutine(Skybox(skyboxDay, skyboxNight, 10f));
        // StartCoroutine(LightingChanges(dayToNightGradient, 10f));

        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        // color = new Color32(255, 246, 225, 255);
        // intensity = 0.3f;
        changeLightColor();

        //change icon from day to night
        timeOfDayIcon.sprite = nightIcon;

        wolfMovement.SpawnAndMoveWolf();
        chicken.SpawnAndMove();
    }

    private void StartDay()
    {
        isDay = true;
        buttonText.text = "Start Night";
        // SetListeners();

        StartCoroutine(Skybox(skyboxNight, skyboxDay, 10f));
        // StartCoroutine(LightingChanges(nightToDayGradient, 10f));

        if (source1 != null) source1.Stop();
        if (source2 != null) source2.Play();

        // color = new Color32(214, 239, 255, 255);
        // intensity = 1f;
        changeLightColor();

        //change icon from day to night
        timeOfDayIcon.sprite = dayIcon;

        wolfMovement.despawn();
        chicken.despawn();
    }

    private IEnumerator Skybox(Texture2D a, Texture2D b, float time)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        // RenderSettings.skybox.SetFloat("_Blend", 0);

        // for (float k = 0; k < time; k += Time.deltaTime)
        // {
        //     RenderSettings.skybox.SetFloat("_Blend", k / time);
        // }

        RenderSettings.skybox.SetTexture("_Texture1", b);
        yield return null;
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