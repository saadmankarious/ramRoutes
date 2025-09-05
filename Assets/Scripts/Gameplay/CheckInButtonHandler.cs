using UnityEngine;
using UnityEngine.UI;

public class CheckInButtonHandler : MonoBehaviour
{
    public EventCheckin eventCheckin;
    private Button button;
    
    public void SetupButton()
    {
        // Get the button component
        button = GetComponent<Button>();
        if (button != null)
        {
            // Clear existing listeners
            button.onClick.RemoveAllListeners();
            
            // Add the onClick listener that will call HandleClick
            button.onClick.AddListener(HandleClick);
            Debug.Log("Button listener set up successfully");
        }
        else
        {
            Debug.LogError("No Button component found on this GameObject");
        }
    }
    
    public void HandleClick()
    {
        Debug.Log("Button clicked!");
        
        // Get the event index from the parent item
        EventItemData itemData = GetComponentInParent<EventItemData>();
        if (itemData != null && eventCheckin != null)
        {
            int index = itemData.eventIndex;
            Debug.Log($"Handling click for event index: {index}");
            eventCheckin.CheckInToEventByIndex(index);
        }
        else
        {
            Debug.LogError("Missing EventItemData or EventCheckin reference");
        }
    }
}
