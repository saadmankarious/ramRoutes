using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Import for UI manipulation

public class BuildingInteraction : MonoBehaviour
{
    public GameObject dialogPanel; // The panel where the text will be displayed
    public Text dialogText; // The text field to show the dialogue
    public string[] dialogLines; // Array to store the information displayed
    public KeyCode interactKey = KeyCode.J; // Key used to interact with buildings

    private bool isPlayerInRange = false; // Is the player in range of a building
    private int currentLineIndex = 0; // Keeps track of which part of the dialog is being displayed

    void Start()
    {
        dialogPanel.SetActive(false); // Hide dialog panel at start
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            if (!dialogPanel.activeInHierarchy)
            {
                // Start the dialog
                dialogPanel.SetActive(true);
                currentLineIndex = 0;
                ShowDialog();
            }
            else
            {
                // If dialog is active, move to the next line
                currentLineIndex++;
                if (currentLineIndex < dialogLines.Length)
                {
                    ShowDialog();
                }
                else
                {
                    // End the dialog when all lines have been shown
                    dialogPanel.SetActive(false);
                }
            }
        }
    }

    // Displays the current line of dialog
    void ShowDialog()
    {
        if (dialogLines.Length > 0 && currentLineIndex < dialogLines.Length)
        {
            dialogText.text = dialogLines[currentLineIndex];
        }
    }


private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player"))
    {
        isPlayerInRange = true;
        Debug.Log("in range");
    }
}

private void OnTriggerExit2D(Collider2D other)
{
    if (other.CompareTag("Player") )
    {
        isPlayerInRange = false;
        Debug.Log("out of range");
        dialogPanel.SetActive(false); // Hide dialog when player walks away
    }
}

}
