using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashCan : MonoBehaviour
{
    public int trashCount = 0; // Count of how many trash pieces have been dropped

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trash"))
        {
            trashCount++;
            Debug.Log("Trash added to trash can. Total: " + trashCount);
            Destroy(other.gameObject); // Destroy the trash object
            
        }
    }
}
