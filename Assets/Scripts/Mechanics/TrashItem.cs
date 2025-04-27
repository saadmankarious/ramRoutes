using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TrashItem : MonoBehaviour
{
    private Animator animator;
    public string name;
    public GameObject panel;
    public Text text;

    [Header("Replacement Prefab")]
    public GameObject replacementPrefab;

    private Vector2 finalRestPosition;
    private Rigidbody2D rb;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(TrackRestingPosition());
    }

    IEnumerator TrackRestingPosition()
    {
        while (true)
        {
            if (rb != null && rb.velocity.sqrMagnitude < 0.01f)
            {
                finalRestPosition = transform.position;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((other.CompareTag("Player") || other.CompareTag("Spaceship")) && gameObject.CompareTag("Eagle")) 
        {
            animator.SetTrigger("turnIntoCup"); 
            panel.SetActive(true);
            text.text = "Pick up using 'C' key to water trees.";
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if ((other.CompareTag("Player") || other.CompareTag("Spaceship")) && gameObject.CompareTag("Eagle")) 
        {
            panel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (replacementPrefab != null)
        {
            Instantiate(replacementPrefab, finalRestPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("No replacement prefab assigned to TrashItem.");
        }
    }
}
