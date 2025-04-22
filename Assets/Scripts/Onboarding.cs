using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OnboardingManager : MonoBehaviour
{
    public GameObject[] panels;
    public AudioSource backgroundMusic;
    public AudioSource tickSound;
    public float narrationSpeed = 1.0f;
    
    private int currentPanelIndex = 0;
    public Button advanceButton;
    private Text[] panelTexts;
    private Coroutine[] narrationCoroutines;

    private void Start()
    {
        panelTexts = new Text[panels.Length];
        narrationCoroutines = new Coroutine[panels.Length];
        
        for (int i = 0; i < panels.Length; i++)
        {
            panelTexts[i] = panels[i].GetComponentInChildren<Text>();
        }

        ShowPanel(currentPanelIndex);
        
        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(AdvanceToNextPanel);
        }

        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }

    private void ShowPanel(int index)
    {
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }

        if (index >= 0 && index < panels.Length)
        {
            panels[index].SetActive(true);
            
            if (narrationCoroutines[index] != null)
            {
                StopCoroutine(narrationCoroutines[index]);
            }
            
            if (panelTexts[index] != null)
            {
                narrationCoroutines[index] = StartCoroutine(NarrateText(panelTexts[index]));
            }
        }
    }

    private IEnumerator NarrateText(Text textComponent)
    {
        string fullText = textComponent.text;
        textComponent.text = "";
        
        foreach (char c in fullText)
        {
            textComponent.text += c;
            if (tickSound != null)
            {
                tickSound.Play();
            }
            yield return new WaitForSeconds(0.1f / narrationSpeed);
        }
    }

    private void AdvanceToNextPanel()
    {
        currentPanelIndex++;
        
        if (currentPanelIndex >= panels.Length)
        {
            currentPanelIndex = 0;
        }

        ShowPanel(currentPanelIndex);
    }

    public void SetNarrationSpeed(float speed)
    {
        narrationSpeed = speed;
    }
}