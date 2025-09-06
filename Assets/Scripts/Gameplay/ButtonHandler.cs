using UnityEngine;
using UnityEngine.UI;
public class ButtonHandler : MonoBehaviour
{
    public Button button;
    private System.Action onClickCallback;

    void Awake()
    {
        // button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }

    public void Initialize(string data, System.Action onClick)
    {
        // Set up your item here
        onClickCallback = onClick;
    }

    void OnButtonClick()
    {
        onClickCallback?.Invoke();
    }
}