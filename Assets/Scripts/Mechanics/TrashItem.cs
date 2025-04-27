using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TrashItem : MonoBehaviour
{

     private Animator animator;
     public string name;
     public GameObject panel;
     public Text text;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player") && gameObject.CompareTag("Eagle") || other.CompareTag("Spaceship") && gameObject.CompareTag("Eagle")) 
    {
        animator.SetTrigger("turnIntoCup"); 
        panel.SetActive(true);
        text.text = "Pick up using 'C' key to water trees.";
        //TODO: Play just cup animation whenever the cup is getting picked up
        //TODO: add if statement before setting turn into cup uncondintionally
    }
}
void OnTriggerExit2D(Collider2D other)
{
    if (other.CompareTag("Player") && gameObject.CompareTag("Eagle") || other.CompareTag("Spaceship") && gameObject.CompareTag("Eagle")) 
    {
        panel.SetActive(false);
      
    }
}


    
}
