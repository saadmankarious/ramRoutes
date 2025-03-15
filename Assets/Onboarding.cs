using UnityEngine;
using UnityEngine.UI;

public class OnboardingManager : MonoBehaviour
{
    // Array to hold all onboarding panels
    public GameObject[] panels;

    // Index of the currently active panel
    private int currentPanelIndex = 0;

    // Reference to the "Advance" button
    public Button advanceButton;

    private void Start()
    {
        // Ensure only the first panel is active at the start
        ShowPanel(currentPanelIndex);

        // Add a listener to the "Advance" button
        if (advanceButton != null)
        {
            advanceButton.onClick.AddListener(AdvanceToNextPanel);
        }
    }

    // Method to show a specific panel
    private void ShowPanel(int index)
    {
        // Deactivate all panels
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }

        // Activate the panel at the specified index
        if (index >= 0 && index < panels.Length)
        {
            panels[index].SetActive(true);
        }
    }

    // Method to advance to the next panel
    private void AdvanceToNextPanel()
    {
        // Increment the current panel index
        currentPanelIndex++;

        // If we've reached the end of the panels, loop back to the first panel
        if (currentPanelIndex >= panels.Length)
        {
            currentPanelIndex = 0; // Loop back to the first panel
        }

        // Show the next panel
        ShowPanel(currentPanelIndex);
    }
}