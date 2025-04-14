using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashItem : MonoBehaviour
{

     private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player") && gameObject.CompareTag("Eagle")) 
    {
        animator.SetTrigger("turnIntoCup"); 
        //TODO: Play just cup animation whenever the cup is getting picked up
        //TODO: add if statement before setting turn into cup uncondintionally
    }
}


    
}
