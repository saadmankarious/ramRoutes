using UnityEngine;
using UnityEngine.UI;
using RamRoutes.Model;

public class EventInfoPanel : MonoBehaviour
{
    private Text titleText;
    private Text descText;
    private Button closeButton;
    private Button dismissButton;

    void Awake()
    {
        titleText = transform.FindDeepChild("title")?.GetComponent<Text>();
        descText = transform.FindDeepChild("desc")?.GetComponent<Text>();
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

        if (UIManager.Instance != null)
        {
            StartCoroutine(UIManager.Instance.AnimatePanelPopup(gameObject));
        }
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}
