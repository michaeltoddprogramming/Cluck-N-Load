using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    // Static methods that handle null instance gracefully
    public static void ShowOneProductionBonus(string animal)
    {
        Debug.Log($"[AnimalProductionIndicator] Static ShowOneProductionBonus called for: {animal}");
        if (Instance != null)
        {
            Instance.oneProductionBonus(animal);
        }
        else
        {
            Debug.LogWarning("AnimalProductionIndicator.Instance is null, trying to find and call with delay...");
            // Try to find the instance and call with a small delay
            var indicator = FindFirstObjectByType<AnimalProductionIndicator>();
            if (indicator != null)
            {
                indicator.StartCoroutine(indicator.DelayedOneProductionBonus(animal));
            }
            else
            {
                Debug.LogError("No AnimalProductionIndicator found in scene!");
            }
        }
    }

    public static void ShowTwoProductionBonuses(string animal1, string animal2)
    {
        Debug.Log($"[AnimalProductionIndicator] Static ShowTwoProductionBonuses called for: {animal1}, {animal2}");
        if (Instance != null)
        {
            Instance.twoProductionBonuses(animal1, animal2);
        }
        else
        {
            Debug.LogWarning("AnimalProductionIndicator.Instance is null, trying to find and call with delay...");
            // Try to find the instance and call with a small delay
            var indicator = FindFirstObjectByType<AnimalProductionIndicator>();
            if (indicator != null)
            {
                indicator.StartCoroutine(indicator.DelayedTwoProductionBonuses(animal1, animal2));
            }
            else
            {
                Debug.LogError("No AnimalProductionIndicator found in scene!");
            }
        }
    }

    private IEnumerator DelayedOneProductionBonus(string animal)
    {
        Debug.Log($"[AnimalProductionIndicator] DelayedOneProductionBonus starting for: {animal}");
        yield return new WaitForEndOfFrame(); // Wait until end of frame to ensure everything is initialized
        Debug.Log($"[AnimalProductionIndicator] DelayedOneProductionBonus executing for: {animal}");
        oneProductionBonus(animal);
    }

    private IEnumerator DelayedTwoProductionBonuses(string animal1, string animal2)
    {
        Debug.Log($"[AnimalProductionIndicator] DelayedTwoProductionBonuses starting for: {animal1}, {animal2}");
        yield return new WaitForEndOfFrame(); // Wait until end of frame to ensure everything is initialized
        Debug.Log($"[AnimalProductionIndicator] DelayedTwoProductionBonuses executing for: {animal1}, {animal2}");
        twoProductionBonuses(animal1, animal2);
    }

    public static void HideAllBonuses()
    {
        if (Instance != null)
        {
            Instance.hideAllBonuses();
        }
    }

    public void hideAllBonuses()
    {
        Debug.Log("[AnimalProductionIndicator] Hiding all production bonus indicators");
        bonus1.SetActive(false);
        bonus2.SetActive(false);
    }

    public void oneProductionBonus(string animal)
    {
        Debug.Log($"[AnimalProductionIndicator] Showing ONE production bonus for animal: {animal}");
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
        Debug.Log($"[AnimalProductionIndicator] Showing TWO production bonuses for animals: {animal1}, {animal2}");
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
