using UnityEngine;
using UnityEngine.UI;
using RamRoutes.Model;

public class EventInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject tagPrefab;
    [SerializeField] private Transform tagsContainer;

    private Text titleText;
    private Text descText;
    private Text dateText;
    private Button closeButton;
    private Button dismissButton;

    void Awake()
    {
        titleText = transform.FindDeepChild("title")?.GetComponent<Text>();
        descText = transform.FindDeepChild("desc")?.GetComponent<Text>();
        dateText = transform.FindDeepChild("date")?.GetComponent<Text>();
        closeButton = transform.FindDeepChild("close")?.GetComponent<Button>();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        // No dedicated close button in the prefab yet, so tapping anywhere on the
        // panel dismisses it. A future "close" child still works alongside this.
        dismissButton = GetComponent<Button>();
        if (dismissButton == null)
        {
            dismissButton = gameObject.AddComponent<Button>();
        }
        dismissButton.onClick.AddListener(Close);
    }

    public void ShowEventInfo(BuildingEvent evt)
    {
        if (evt == null)
        {
            return;
        }

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
            dateText.text = evt.GetDisplayDate();
        }

        PopulateTags(evt.tags);

        if (UIManager.Instance != null)
        {
            StartCoroutine(UIManager.Instance.AnimatePanelPopup(gameObject));
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

    public void Close()
    {
        Destroy(gameObject);
    }
}
