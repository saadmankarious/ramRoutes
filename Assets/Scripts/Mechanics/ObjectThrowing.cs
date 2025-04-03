using System.Collections;
using UnityEngine;

public class ObjectThrower : MonoBehaviour
{
    public GameObject objectToThrow; // Object prefab to throw
    public float throwForce = 1000f; // Increased throw force
    public float spawnDistance = 2f; // Distance to spawn object in front of thrower
    public float randomSpread = 10f; // Random variation in throw angle
    private float throwInterval = 10f; // Fixed time to 4 seconds

    private void Start()
    {
        StartCoroutine(ThrowObjectRoutine());
    }

    IEnumerator ThrowObjectRoutine()
    {
        while (true)
        {
            ThrowObject();
            yield return new WaitForSeconds(throwInterval); // Fixed at 4 seconds
        }
    }

    void ThrowObject()
    {
        if (objectToThrow != null)
        {
            // Spawn the object in front of the thrower to prevent self-collision
            Vector3 spawnPosition = transform.position + transform.forward * spawnDistance;

            GameObject thrownObject = Instantiate(objectToThrow, spawnPosition, transform.rotation);
            Rigidbody rb = thrownObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Add random variation to the throw direction
                Vector3 randomDirection = transform.forward +
                    new Vector3(
                        Random.Range(-randomSpread, randomSpread),
                        Random.Range(-randomSpread, randomSpread),
                        Random.Range(-randomSpread, randomSpread)
                    ).normalized;

                // Apply a strong force in the throw direction
                rb.AddForce(randomDirection * throwForce, ForceMode.Impulse);
            }
        }
    }
}
