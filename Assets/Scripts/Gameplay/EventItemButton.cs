using UnityEngine;
using UnityEngine.UI;

public class EventItemButton : MonoBehaviour
{
    private EventCheckin eventCheckin;
    private int eventIndex;
    private Button checkInButton;

    void Awake()
    {
        checkInButton = GetComponent<Button>();
    }

    public void Setup(EventCheckin checkinManager, int index)
    {
        eventCheckin = checkinManager;
        eventIndex = index;
        
        if (checkInButton != null)
        {
            checkInButton.onClick.RemoveAllListeners();
            checkInButton.onClick.AddListener(OnButtonClick);
        }
    }

    public void OnButtonClick()
    {
        if (eventCheckin != null)
        {
            eventCheckin.CheckInToEventByIndex(eventIndex);
        }
    }
}
