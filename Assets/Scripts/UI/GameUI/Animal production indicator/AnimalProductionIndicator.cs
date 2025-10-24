using UnityEngine;
using UnityEngine.UI;

public class AnimalProductionIndicator : MonoBehaviour
{
    public static AnimalProductionIndicator Instance { get; private set; }

    [SerializeField] private GameObject bonus1;
    [SerializeField] private GameObject bonus2;

    [SerializeField] private Image bonus1Image;

    [SerializeField] private Image bonus2Image1;
    [SerializeField] private Image bonus2Image2;

    [SerializeField] private Sprite chicken;
    [SerializeField] private Sprite cow;
    [SerializeField] private Sprite sheep;
    [SerializeField] private Sprite goat;
    [SerializeField] private Sprite pig;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optional: Uncomment if you want it to persist between scenes
        // DontDestroyOnLoad(gameObject);
    }

    public void oneProductionBonus(string animal)
    {
        bonus1.SetActive(true);
        bonus2.SetActive(false);
        
        switch(animal)
        {
            case "Ch":
                bonus1Image.sprite = chicken;
                break;
            case "C":
                bonus1Image.sprite = cow;
                break;
            case "S":
                bonus1Image.sprite = sheep;
                break;
            case "G":
                bonus1Image.sprite = goat;
                break;
            case "P":
                bonus1Image.sprite = pig;
                break;
        }
    }

    public void twoProductionBonuses(string animal1, string animal2)
    {
        bonus1.SetActive(false);
        bonus2.SetActive(true);

        if(animal1 == "Ch")
        {
            switch(animal2)
            {
                case "C":
                    bonus2Image1.sprite = cow;
                    break;
                case "S":
                    bonus2Image1.sprite = sheep;
                    break;
                case "G":
                    bonus2Image1.sprite = goat;
                    break;
                case "P":
                    bonus2Image1.sprite = pig;
                    break;
            }

            bonus2Image2.sprite = chicken;
        }
        else
        {
            switch(animal1)
            {
                case "Ch":
                    bonus2Image1.sprite = chicken;
                    break;
                case "C":
                    bonus2Image1.sprite = cow;
                    break;
                case "S":
                    bonus2Image1.sprite = sheep;
                    break;
                case "G":
                    bonus2Image1.sprite = goat;
                    break;
                case "P":
                    bonus2Image1.sprite = pig;
                    break;
            }

            switch(animal2)
            {
                case "Ch":
                    bonus2Image2.sprite = chicken;
                    break;
                case "C":
                    bonus2Image2.sprite = cow;
                    break;
                case "S":
                    bonus2Image2.sprite = sheep;
                    break;
                case "G":
                    bonus2Image2.sprite = goat;
                    break;
                case "P":
                    bonus2Image2.sprite = pig;
                    break;
            }
        }
    }
}
