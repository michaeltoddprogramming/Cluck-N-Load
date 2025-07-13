using System.Collections.Generic;
using UnityEngine;

public class ToggleButtonGroup : MonoBehaviour
{
    public List<ToggleButtonVisual> buttons;

    private ToggleButtonVisual selectedButton;

    private void Start()
    {
        foreach (var btn in buttons)
        {
            btn.Initialize(this);
        }
    }

    public void OnButtonSelected(ToggleButtonVisual clickedButton)
    {
        if (selectedButton != null)
            selectedButton.SetSelected(false);

        selectedButton = clickedButton;
        selectedButton.SetSelected(true);
    }
}

// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.EventSystems;

// public class ToggleButtonGroup : MonoBehaviour
// {
//     public List<ToggleButtonVisual> buttons;
//     private ToggleButtonVisual selectedButton;

//     private void Start()
//     {
//         foreach (var btn in buttons)
//         {
//             btn.Initialize(this);
//         }

//         // Set up the Event System listener for detecting clicks outside buttons
//         EventSystem.current.gameObject.AddComponent<OutsideClickListener>().OnOutsideClick += DeselectAllButtons;
//     }

//     // This method is called when a button is clicked
//     public void OnButtonSelected(ToggleButtonVisual clickedButton)
//     {
//         if (selectedButton != null)
//             selectedButton.SetSelected(false);  // Deselect previous button

//         selectedButton = clickedButton;
//         selectedButton.SetSelected(true);  // Select the clicked button
//     }

//     // Method to deselect all buttons when a click happens outside the button group
//     private void DeselectAllButtons()
//     {
//         if (selectedButton != null)
//         {
//             selectedButton.SetSelected(false);
//             selectedButton = null;
//         }
//     }
// }

// // This class listens for clicks outside the button group
// public class OutsideClickListener : MonoBehaviour, IPointerDownHandler
// {
//     public delegate void OutsideClickAction();
//     public event OutsideClickAction OnOutsideClick;

//     // Detect a click anywhere in the game scene
//     public void OnPointerDown(PointerEventData eventData)
//     {
//         if (OnOutsideClick != null && !eventData.pointerEnter.GetComponent<ToggleButtonVisual>())
//         {
//             OnOutsideClick.Invoke();
//         }
//     }
// }

// using UnityEngine;

// public class ToggleButtonGroup : MonoBehaviour
// {
//     public ToggleButton playButton;
//     public ToggleButton pauseButton;

//     public void SwitchToPlay()
//     {
//         playButton.ResetButton(); // Reset play button
//         pauseButton.ResetButton(); // Reset pause button
//         playButton.ToggleButtonSelection(); // Set play button to selected
//     }

//     public void SwitchToPause()
//     {
//         playButton.ResetButton(); // Reset play button
//         pauseButton.ResetButton(); // Reset pause button
//         pauseButton.ToggleButtonSelection(); // Set pause button to selected
//     }
// }
