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
            NotificationManager.ShowError("Error!", "Something went wrong!");
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
    }
}