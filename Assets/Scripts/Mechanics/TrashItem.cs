using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashItem : MonoBehaviour
{
    void Start()
    {
        // Set tag to "Trash" so player can identify it
        // gameObject.tag = "Trash";
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TrashCan"))
        {
            Debug.Log("Trash placed in trash can");
            Destroy(gameObject); // Remove trash from the scene
        }
    }
}
