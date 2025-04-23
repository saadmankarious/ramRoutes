using System.Collections;
using UnityEngine;

public class ObjectThrower2D : MonoBehaviour
{
    public GameObject[] objectsToThrow;
    public float throwForce = 10f;
    public float spawnDistance = 2f;
    public float throwInterval = 1f;
    public float randomSpread = 0.2f;

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
            // Select random object
            GameObject objectToThrow = objectsToThrow[Random.Range(0, objectsToThrow.Length)];

            // Get random angle between 0 and 360 degrees
            float randomAngle = Random.Range(0f, 360f);

            // Calculate spawn position in random direction
            Vector2 spawnDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                                                Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            Vector2 spawnPosition = (Vector2)transform.position + (spawnDirection * spawnDistance);

            // Instantiate with 2D physics
            GameObject thrownObject = Instantiate(objectToThrow, spawnPosition, Quaternion.identity);
            Rigidbody2D rb = thrownObject.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                // Apply force in the same random direction with slight spread variation
                Vector2 throwDirection = spawnDirection +
                    new Vector2(
                        Random.Range(-randomSpread, randomSpread),
                        Random.Range(-randomSpread, randomSpread)
                    ).normalized;

                rb.AddForce(throwDirection.normalized * throwForce, ForceMode2D.Impulse);
            }
        }
    }
}