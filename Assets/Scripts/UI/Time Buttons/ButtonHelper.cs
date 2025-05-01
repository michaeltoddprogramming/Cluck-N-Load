using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHelper : MonoBehaviour
{
    public void DeselectButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }
}