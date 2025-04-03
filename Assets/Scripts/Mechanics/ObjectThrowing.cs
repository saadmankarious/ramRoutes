using System.Collections;
using UnityEngine;

public class ObjectThrower : MonoBehaviour
{
    public GameObject[] objectsToThrow; // Array of object prefabs to throw
    public float throwForce = 1000f;
    public float spawnDistance = 2f;
    public float randomSpread = 10f;
    public float throwInterval = 1f;

    private void Start()
    {
        StartCoroutine(ThrowObjectRoutine());
    }

    IEnumerator ThrowObjectRoutine()
    {
        while (true)
        {
            ThrowObject();
            yield return new WaitForSeconds(throwInterval);
        }
    }

    void ThrowObject()
    {
        if (objectsToThrow.Length > 0)
        {
            GameObject objectToThrow = objectsToThrow[Random.Range(0, objectsToThrow.Length)];
            Vector3 spawnPosition = transform.position + transform.forward * spawnDistance;

            GameObject thrownObject = Instantiate(objectToThrow, spawnPosition, transform.rotation);
            Rigidbody rb = thrownObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDirection = transform.forward +
                    new Vector3(
                        Random.Range(-randomSpread, randomSpread),
                        Random.Range(-randomSpread, randomSpread),
                        Random.Range(-randomSpread, randomSpread)
                    ).normalized;

                rb.AddForce(randomDirection * throwForce, ForceMode.Impulse);
            }
        }
    }
}