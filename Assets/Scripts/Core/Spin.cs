using UnityEngine;

public class Spin : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 90f; // degrees per second
    public float rotationInterval = 2f; // seconds between rotations
    private float timer = 0f;
    private bool rotating = false;
    private float accumulatedRotation = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!rotating && timer < rotationInterval)
        {
            timer += Time.deltaTime;
            return;
        }
        if (!rotating)
        {
            accumulatedRotation = 0f;
            rotating = true;
        }
        if (rotating)
        {
            float step = rotationSpeed * Time.deltaTime;
            transform.Rotate(rotationAxis, step, Space.World);
            accumulatedRotation += step;
            if (accumulatedRotation >= 360f)
            {
                rotating = false;
                timer = 0f;
            }
        }
    }
}
