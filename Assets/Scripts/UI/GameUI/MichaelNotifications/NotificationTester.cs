using UnityEngine;

public class NotificationTester : MonoBehaviour
{
    [Header("Quick Tests - Check to Trigger")]
    public bool testSuccess = false;
    public bool testWarning = false;
    public bool testError = false;
    public bool testInfo = false;
    public bool testAchievement = false;
    public bool testNew = false; // NEW theme like your upgrade screen!
    public bool testBlocking = false; // Test blocking notification
    [Header("Season Tests")]
    public bool testSeason1 = false;
    public bool testSeason2 = false;
    public bool testSeason3 = false;
    public bool testSeason4 = false;
    [Header("Test Bonus")]
    // Optional sprite you can assign in the inspector for testing bonus icons
    public Sprite testBonusIcon;

    private void Update()
    {
        if (testSuccess)
        {
            testSuccess = false;
            NotificationManager.ShowSuccess("Success!", "Everything works!");
        }
        
        if (testWarning)
        {
            testWarning = false;
            NotificationManager.ShowWarning("Warning!", "Pay attention!");
        }
        
        if (testError)
        {
            testError = false;
            // NotificationManager.ShowError("Error!", "Something went wrong!");
        }
        
        if (testInfo)
        {
            testInfo = false;
            NotificationManager.ShowNotification("Info", "Here's some info", "Info");
        }
        
        if (testAchievement)
        {
            testAchievement = false;
            NotificationManager.ShowAchievement("Achievement!", "You did it!");
        }
        
        if (testNew)
        {
            testNew = false;
            NotificationManager.ShowNotification("NEW ITEM!", "Damage Tome acquired!", "New");
        }
        
        if (testBlocking)
        {
            testBlocking = false;
            // Use the default Blocking theme
            NotificationManager.ShowBlockingNotification("Important!", "This is a blocking notification. Click to continue.");
        }

        if (testSeason1)
        {
            testSeason1 = false;
            // Inject fake bonus info for testing
            if (NotificationManager.Instance != null && NotificationManager.Instance.seasonalInfos != null && NotificationManager.Instance.seasonalInfos.Length >= 1)
            {
                NotificationManager.Instance.seasonalInfos[0].bonusText = "Test Bonus: +50% XP";
                NotificationManager.Instance.seasonalInfos[0].bonusIcon = testBonusIcon;
            }
            NotificationManager.ShowSeasonalBlocking(1);
        }

        if (testSeason2)
        {
            testSeason2 = false;
            if (NotificationManager.Instance != null && NotificationManager.Instance.seasonalInfos != null && NotificationManager.Instance.seasonalInfos.Length >= 2)
            {
                NotificationManager.Instance.seasonalInfos[1].bonusText = "Test Bonus: Free Seeds";
                NotificationManager.Instance.seasonalInfos[1].bonusIcon = testBonusIcon;
            }
            NotificationManager.ShowSeasonalBlocking(2);
        }

        if (testSeason3)
        {
            testSeason3 = false;
            if (NotificationManager.Instance != null && NotificationManager.Instance.seasonalInfos != null && NotificationManager.Instance.seasonalInfos.Length >= 3)
            {
                NotificationManager.Instance.seasonalInfos[2].bonusText = "Test Bonus: +1 Rare Drop";
                NotificationManager.Instance.seasonalInfos[2].bonusIcon = testBonusIcon;
            }
            NotificationManager.ShowSeasonalBlocking(3);
        }

        if (testSeason4)
        {
            testSeason4 = false;
            if (NotificationManager.Instance != null && NotificationManager.Instance.seasonalInfos != null && NotificationManager.Instance.seasonalInfos.Length >= 4)
            {
                NotificationManager.Instance.seasonalInfos[3].bonusText = "Test Bonus: +Warmth";
                NotificationManager.Instance.seasonalInfos[3].bonusIcon = testBonusIcon;
            }
            NotificationManager.ShowSeasonalBlocking(4);
        }
    }
}