using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eagle : MonoBehaviour
{
    private bool filled;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {


    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Waterfall") )
        {
            animator.SetTrigger("cupFilling");
            filled = true;
            Debug.Log("filling cup + " + filled);

        }
        if (other.CompareTag("TreeSpot"))
        {

            Debug.Log("watering the tree + " + filled);
            animator.SetTrigger("cupUnFilling");
            Animator otherAnimator = other.GetComponent<Animator>();
            if (otherAnimator != null)
            {
                otherAnimator.SetTrigger("growTree");
                GameManager.Instance.WaterTree();

            }
            else
            {
                Debug.LogError("No Animator component found on the TreeSpot object");
            }            
        }
    }

}
