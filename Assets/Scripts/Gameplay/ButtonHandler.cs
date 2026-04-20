using UnityEngine;
using UnityEngine.UI;
public class ButtonHandler : MonoBehaviour
{
    public Button button;
    private System.Action onClickCallback;

    void Awake()
    {
        // Try to get button component if not assigned
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        // Only add listener if button exists
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        // else
        // {
        //     Debug.LogWarning("ButtonHandler: No Button component found. This component should be attached to a GameObject with a Button component.");
        // }
    }

    public void Initialize(string data, System.Action onClick)
    {
        // Set up your item here
        onClickCallback = onClick;
    }

    void OnButtonClick()
    {
        if (onClickCallback != null)
        {
            onClickCallback.Invoke();
        }
    }
}