using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NightManager : MonoBehaviour
{
    //start night button
    [SerializeField] private Button startNightButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    //light
    [SerializeField] private Light sceneLight;
    [SerializeField] private Color color = new Color32(0xAA, 0xBB, 0xDD, 0xFF);
    [SerializeField] private float intensity = 0.3f;
    [SerializeField] private Gradient dayToNightGradient;
    [SerializeField] private Gradient nightToDayGradient;

    //skyboxes
    [SerializeField] private Texture2D skyboxMorning;
    [SerializeField] private Texture2D skyboxDay;
    [SerializeField] private Texture2D skyboxAfternoon;
    [SerializeField] private Texture2D skyboxNight;

    //animals
    [SerializeField] private WolfMovement wolfMovement;
    [SerializeField] private ChickenMovement chicken;

    //time indicator icons
    [SerializeField] private Image timeOfDayIcon; 
    [SerializeField] private Sprite dayIcon; 
    [SerializeField] private Sprite nightIcon;

    //songs
    private AudioSource source1;
    private AudioSource source2;

    //is day bool
    private bool isDay = true;


    //shop stuff
    [SerializeField] private Button shopButton;
    private Color dayShop = Color.white;
    private Color nightShop = Color.grey * 0.9f;
    public Image shopIcon;

    //time management
    //season notification
    [SerializeField] private TextMeshProUGUI seasonNotification;

    //pause, play, fast forward
    private float speedUp = 1f;
    private bool isPaused = false;

    //time of day indicator
    [SerializeField] private TextMeshProUGUI timeText;


    //time
    [SerializeField] private int minutes;
    [SerializeField] public int Minutes
    { 
        get { return minutes; } 
        set { minutes = value; OnMinutesChange(value); } 
    }

    [SerializeField] private int hours;
    [SerializeField] public int Hours
    { 
        get { return hours; } 
        set { hours = value; OnHoursChange(value); } 
    }

    [SerializeField] private int days;
    [SerializeField] public int Days
    { 
        get { return days; } 
        set { days = value; OnDayChange(value); } 
    }

    [SerializeField] private int years;
    [SerializeField] public int Years
    { 
        get { return years; } 
        set { years = value; } 
    }
    private float tempSecond;

    //season icons
    [SerializeField] private Image seasonIcon; 
    [SerializeField] private Sprite summer; 
    [SerializeField] private Sprite winter;
    [SerializeField] private Sprite spring;
    [SerializeField] private Sprite fall;



    private void Start()
    {
        // SetListeners();
        SetInitialLight();

        Hours = 7;
        Years = 1;
        seasonNotification.gameObject.SetActive(false);

        // StartDay(0);

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            source1 = sources[0];
            source2 = sources[1];
        }
    }

    public void Update()
    {
        if(isPaused)
        {
            return;
        }

        tempSecond += Time.deltaTime * speedUp;     

        timeText.text = $"{Hours:D2}:{Minutes:D2}";
   

        if(tempSecond >= 2f)
        {
            Minutes += 1;
            tempSecond = 0;
        }
    }

    public void pauseTime()
    {
        isPaused = true;
    }

    public void playTime()
    {
        isPaused = false;
        speedUp = 1f;
    }

    public void fastForwardTime()
    {
        isPaused = false;
        speedUp = 3f;
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
            StartNight(1);
        }
        else
        {
            Debug.Log("Gonna start the day now");
            color = new Color32(255, 244, 214, 255);
            intensity = 2f;
            buttonText.text = "End Night";
            changeLightColor();
            StartDay(1);
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

    private void StartNight(int flag)
    {
        //if night is triggered by user (button is clicked)
        if(flag == 1)
        {
            //disable button during night and make grey
            shopButton.interactable = false;
            shopIcon.color = nightShop;


            isDay = false;
            buttonText.text = "End Night";
            // SetListeners();

            StartCoroutine(Skybox(skyboxDay, skyboxNight, 5f));
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
        else  //night is triggered by game loop
        {
            //disable button during night and make grey
            shopButton.interactable = false;
            shopIcon.color = nightShop;


            isDay = false;
            buttonText.text = "End Night";
            // SetListeners();

            // StartCoroutine(Skybox(skyboxDay, skyboxNight, 5f));
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
    }

    private void StartDay(int flag)
    {
        //load day instantly if first load to always start game on day
        if(flag == 0)
        {
            //enable button during day and colour
            shopButton.interactable = true;
            shopIcon.color = dayShop;
            
            isDay = true;
            buttonText.text = "Start Night";
            // SetListeners();

            StartCoroutine(Skybox(skyboxNight, skyboxDay, 0f));
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
        else if(flag == 1)   //id user clicks start day 
        {
            //enable button during day and colour
            shopButton.interactable = true;
            shopIcon.color = dayShop;
            
            isDay = true;
            buttonText.text = "Start Night";
            // SetListeners();

            StartCoroutine(Skybox(skyboxNight, skyboxDay, 0f));
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
        else  //will handle if gameloop changes to day based of time not user input
        {
            //enable button during day and colour
            shopButton.interactable = true;
            shopIcon.color = dayShop;
            
            isDay = true;
            buttonText.text = "Start Night";
            // SetListeners();

            // StartCoroutine(Skybox(skyboxNight, skyboxDay, 5f));
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



    private void OnMinutesChange(int value)
    {
        // Debug.Log("OnMinuteChange1");

        if(value >= 1)
        {
            // Debug.Log("OnMinuteChange2");
            Hours ++;
            minutes = 0;
        }

        if(Hours >= 24)
        {
            // Debug.Log("OnMinuteChange3");
            Days ++;
            Hours = 0;
        }     

        if(Days == 5)
        {
            minutes = 0;
            hours = 0;
            days = 0;
            Years ++;
        }   
    }
    private void OnHoursChange(int value)
    {
        // Debug.Log("sediroufgeoiswuyrghfiouyesghrfiuyger: " + value);
        if(value == 5)
        {
            StartDay(2);
            
            Debug.Log("morning-----------------");
            StartCoroutine(Skybox(skyboxNight, skyboxMorning, 2f));
        }
        else if(value == 7)
        {
            Debug.Log("day-----------------");
            StartCoroutine(Skybox(skyboxMorning, skyboxDay, 2f));
        }
        else if(value == 16)
        {
            Debug.Log("afternoon-----------------");
            StartCoroutine(Skybox(skyboxDay, skyboxAfternoon, 2f));
        }
        else if(value == 20)
        {
            StartNight(2);

            Debug.Log("evening-----------------");
            StartCoroutine(Skybox(skyboxAfternoon, skyboxNight, 2f));
        }        
    }

    private void OnDayChange(int value)
    {
        if(value == 1)
        {
            setSeason(1);
        }
        else if(value == 2)
        {
            setSeason(2);
        }
        else if(value == 3)
        {
            setSeason(3);
        }
        else if(value == 4)
        {
            setSeason(4);
        }
        else if(value == 5)
        {
            setSeason(5);
        }
        else if(value == 5)
        {
            Debug.Log("Resetting the full time loop!");
            years ++;
            days = 0;
            hours = 7;
            minutes = 0;

            StartDay(0); // force reset to day state
            setSeason(1); // reset season if needed
        }
        // if(value == 1)
        // {
        //     setSeason(1);
        // }
        // else if(value == 6)
        // {
        //     setSeason(2);
        // }
        // else if(value == 11)
        // {
        //     setSeason(3);
        // }
        // else if(value == 16)
        // {
        //     setSeason(4);
        // }
        // else if(value == 20)
        // {
        //     setSeason(5);
        // }
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

    private void setSeason(int season)
    {
        if(season == 1)
        {
            StartCoroutine(showText("Spring!!", 5));
            seasonIcon.sprite = spring;
        }
        else if(season == 2)
        {
            StartCoroutine(showText("Summer!!", 5));
            seasonIcon.sprite = summer;
        }
        else if(season == 3)
        {
            StartCoroutine(showText("Fall!!", 5));
            seasonIcon.sprite = fall;
        }
        else if(season == 4)
        {
            StartCoroutine(showText("Winter!!", 5));
            seasonIcon.sprite = winter;
        }
        else if(season == 5)
        {
            string text = "Year " + Years.ToString() + " done!!";
            StartCoroutine(showText(text, 5));
        }
    }

    private IEnumerator showText(string message, float time)
    {
        seasonNotification.text = message;
        seasonNotification.gameObject.SetActive(true);

        yield return new WaitForSeconds(time);

        seasonNotification.gameObject.SetActive(false);
    }
}