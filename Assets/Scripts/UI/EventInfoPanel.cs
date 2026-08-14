using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Threading.Tasks;
using RamRoutes.Model;
using RamRoutes.Services;
using Firebase.Auth;

public class EventInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject tagPrefab;
    [SerializeField] private Transform tagsContainer;
    [SerializeField] private GameObject rsvpUserPrefab;

    private Text titleText;
    private Text descText;
    private Text dateText;
    private Button closeButton;

    private Button rsvpButton;
    private Text rsvpButtonText;
    private ScrollRect rsvpScrollView;
    private Transform rsvpContentParent;
    private Text rsvpEmptyText;

    private string currentEventId;
    private bool isRsvped;

    void Awake()
    {
        titleText = transform.FindDeepChild("title")?.GetComponent<Text>();
        descText = transform.FindDeepChild("desc")?.GetComponent<Text>();
        dateText = transform.FindDeepChild("date")?.GetComponent<Text>();
        closeButton = transform.FindDeepChild("close")?.GetComponent<Button>();

        rsvpButton = transform.FindDeepChild("rsvp-button")?.GetComponent<Button>();
        rsvpButtonText = rsvpButton != null ? rsvpButton.GetComponentInChildren<Text>() : null;
        rsvpScrollView = transform.FindDeepChild("rsvp-list")?.GetComponent<ScrollRect>();
        rsvpContentParent = rsvpScrollView != null ? rsvpScrollView.content : null;
        rsvpEmptyText = transform.FindDeepChild("empty")?.GetComponent<Text>();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (rsvpButton != null)
        {
            rsvpButton.onClick.AddListener(OnRsvpButtonClicked);
        }
    }

    public void ShowEventInfo(BuildingEvent evt)
    {
        if (evt == null)
        {
            return;
        }

        currentEventId = evt.eventId;

        if (titleText != null)
        {
            titleText.text = evt.eventName;
        }

        if (descText != null)
        {
            descText.text = evt.description;
        }

        if (dateText != null)
        {
            dateText.text = evt.date;
        }

        PopulateTags(evt.tags);
        PopulateRsvpList();
        RefreshRsvpButtonState();

        if (UIManager.Instance != null)
        {
            StartCoroutine(UIManager.Instance.AnimatePanelPopup(gameObject));
        }
    }

    private async void RefreshRsvpButtonState()
    {
        string eventId = currentEventId;
        if (string.IsNullOrEmpty(eventId)) return;

        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        isRsvped = await BuildingEventService.Instance.HasPlayerShownInterest(eventId, userId);
        if (this == null || eventId != currentEventId) return;

        UpdateRsvpButtonText();
    }

    private void UpdateRsvpButtonText()
    {
        if (rsvpButtonText != null)
        {
            rsvpButtonText.text = isRsvped ? "Unrsvp" : "RSVP";
        }
    }

    private void PopulateTags(System.Collections.Generic.List<string> tags)
    {
        if (tagsContainer == null) return;

        foreach (Transform child in tagsContainer)
        {
            Destroy(child.gameObject);
        }

        if (tagPrefab == null || tags == null) return;

        foreach (string tag in tags)
        {
            GameObject tagGO = Instantiate(tagPrefab, tagsContainer);
            Text tagText = tagGO.GetComponentInChildren<Text>();
            if (tagText != null)
            {
                tagText.text = tag;
            }
        }
    }

    private async void OnRsvpButtonClicked()
    {
        if (string.IsNullOrEmpty(currentEventId)) return;

        string eventId = currentEventId;
        string userId = FirebaseAuth.DefaultInstance.CurrentUser != null ? FirebaseAuth.DefaultInstance.CurrentUser.UserId : "unknown";
        var eventService = BuildingEventService.Instance;

        bool isAlreadyInterested = await eventService.HasPlayerShownInterest(eventId, userId);
        if (this == null || eventId != currentEventId) return;

        if (isAlreadyInterested)
        {
            await eventService.RemoveInterestAsync(eventId, userId);
            if (this == null || eventId != currentEventId) return;

            isRsvped = false;
            UpdateRsvpButtonText();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate("Unrsvp'd from event");
            }
        }
        else
        {
            await eventService.RecordInterestAsync(eventId, userId);
            if (this == null || eventId != currentEventId) return;

            isRsvped = true;
            UpdateRsvpButtonText();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowQuickUpdate("RSVP'd to event!");
            }
        }

        PopulateRsvpList();
    }

    private async void PopulateRsvpList()
    {
        if (rsvpContentParent == null || string.IsNullOrEmpty(currentEventId)) return;

        string eventId = currentEventId;

        foreach (Transform child in rsvpContentParent)
        {
            Destroy(child.gameObject);
        }

        var eventData = await BuildingEventService.Instance.GetBuildingEventByIdAsync(eventId);
        if (this == null || eventId != currentEventId) return;

        var rsvpList = eventData?.interestedUsers ?? new System.Collections.Generic.List<string>();

        if (rsvpEmptyText != null)
        {
            rsvpEmptyText.gameObject.SetActive(rsvpList.Count == 0);
        }

        // Fetch all RSVP'd users concurrently instead of one at a time - for an event
        // with N interested users this turns N sequential round trips into one batch.
        var userService = new UserService();
        var users = await Task.WhenAll(rsvpList.Select(id => userService.RetrieveUserById(id)));
        if (this == null || eventId != currentEventId) return;

        foreach (var user in users)
        {
            if (user == null || rsvpUserPrefab == null) continue;

            GameObject studentGO = Instantiate(rsvpUserPrefab, rsvpContentParent);
            Text userNameText = studentGO.GetComponentInChildren<Text>(true);
            // Image userImage = studentGO.GetComponentInChildren<Image>(true);

            if (userNameText != null)
            {
                userNameText.text = user.name;
            }

            // if (userImage != null && UIManager.Instance != null)
            // {
            //     // RetrieveUserById already returns coins/knowledgePoints on the User
            //     // object - no need for two more per-user round trips to fetch them again.
            //     userImage.sprite = UIManager.Instance.GetUserAvatarBasedOnPoints(user.coins, user.knowledgePoints);
            // }
        }
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
