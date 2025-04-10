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
        // Optional: Force stop after clip duration
        StartCoroutine(StopAnimationAfterDelay());
    }
}

IEnumerator StopAnimationAfterDelay()
{
    // Wait for the clip's length
    yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
    animator.enabled = false; // Disables the Animator (freezes on last frame)
    // OR: animator.Play("EmptyState"); // If you have an empty state
}

    
}
