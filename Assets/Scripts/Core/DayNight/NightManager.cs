using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NightManager : MonoBehaviour
{
    [SerializeField] private Button startNightButton;
    [SerializeField] private Light sceneLight;
[SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
    [SerializeField] private Gradient dayToNightGradient;
    [SerializeField] private Gradient nightToDayGradient;
    [SerializeField] private WolfMovement wolfMovement;
    [SerializeField] private ChickenMovement chicken;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxNight;
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

    void Update()
    {

        // changeLightColor();

    }

    private void StartNight()
    {
        //change skybox day to night over 5 seconds
        StartCoroutine(Skybox(skyboxDay, skyboxNight, 10f));
        StartCoroutine(LightingChanges(dayToNightGradient, 10f));



        //stop daytime song and play night time
        if (source1 != null) source1.Play();
        if (source2 != null) source2.Stop();

        // GetComponent<AudioSource>().Play();


        // Set scene to night
        // sceneLight.color = color;
        // sceneLight.intensity = 0.3f;
        changeLightColor();

        // Tell the WolfMovement to spawn and start moving
        wolfMovement.SpawnAndMoveWolf();
        chicken.SpawnAndMove();
    }

    private IEnumerator Skybox(Texture2D a, Texture2D b, float time)
    {
        RenderSettings.skybox.SetTexture("_Texture1", a);
        RenderSettings.skybox.SetTexture("_Texture2", b);
        RenderSettings.skybox.SetFloat("_Blend", 0);

        for(float k = 0; k < time; k ++)
        {
            RenderSettings.skybox.SetFloat("_Blend", k / time);
            yield return null; 
        }

        RenderSettings.skybox.SetTexture("_Texture1", b);
    }

    private IEnumerator LightingChanges(Gradient lightGradient, float time)
    {
        for(float k = 0; k < time; k ++)
        {
            sceneLight.color = lightGradient.Evaluate(k / time);

            yield return null; 
        }

    }

    private void changeLightColor()
    {
        sceneLight.color = color;
        sceneLight.intensity = 0.3f;

    }
}
