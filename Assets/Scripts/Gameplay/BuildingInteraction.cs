using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingInteraction : MonoBehaviour
{
    public GameObject dialogPanel;
    public Text dialogText;
    public string[] dialogLines;
    public KeyCode interactKey = KeyCode.J;
    public GameObject saplingPrefab;
    public AudioClip rewardSound;
    public bool isPlanet;
    [SerializeField] private bool hasSapling = false;

    private bool isPlayerInRange = false;
    private int currentLineIndex = 0;
    private bool saplingSpawned = false;
    private bool extraLineShown = false;
    private AudioSource audioSource;

    void Start()
    {
        dialogPanel.SetActive(false);
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            if (!dialogPanel.activeInHierarchy)
            {
                dialogPanel.SetActive(true);
                currentLineIndex = 0;
                extraLineShown = false;
                ShowDialog(dialogLines[currentLineIndex]);
            }
            else
            {
                currentLineIndex++;
                if (currentLineIndex < dialogLines.Length)
                {
                    ShowDialog(dialogLines[currentLineIndex]);
                }
                else if (hasSapling && !saplingSpawned &&  GameManager.Instance.currentTrial.trialNumber == 2 && !extraLineShown)
                {
                    dialogText.text = "You received a sapling!";
                    SpawnSapling();
                    saplingSpawned = true;
                    extraLineShown = true;
                }
                else
                {
                    dialogPanel.SetActive(false);
                }
            }
        }
    }

    void ShowDialog(string str)
    {
        dialogText.text = str;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if(!isPlanet && GameManager.Instance.currentTrial.trialNumber ==2)
            {
             ShowDialog("Hit V to interact");

            }

        } 
            if(other.CompareTag("Spaceship"))
            {
                            isPlayerInRange = true;

                dialogPanel.SetActive(true);
                ShowDialog(dialogLines[0]);

            }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            dialogPanel.SetActive(false);
        }
        if(other.CompareTag("Spaceship"))
            {
                dialogPanel.SetActive(false);

            }
    }

    private void SpawnSapling()
    {
        Vector3 spawnPosition = transform.position + transform.right * 1.5f;
        Instantiate(saplingPrefab, spawnPosition, Quaternion.identity);
        if (rewardSound && audioSource) audioSource.PlayOneShot(rewardSound);
    }
}
