using Cinemachine;
using UnityEngine;

public class SwitchConfiner : MonoBehaviour
{
    public Collider2D[] confiners; // List of Collider2D shapes to switch between

    private CinemachineConfiner confiner; // Reference to the CinemachineConfiner component
    private int currentConfinerIndex = 0; // Track the current confiner index

    private void Start()
    {
        // Get the CinemachineConfiner component on this GameObject (the camera)
        confiner = GetComponent<CinemachineConfiner>();

        if (confiner == null)
        {
            Debug.LogError("CinemachineConfiner component not found on this GameObject!");
            return;
        }

        if (confiners == null || confiners.Length == 0)
        {
            Debug.LogError("No Collider2D shapes assigned to the confiners list!");
            return;
        }

        // Log the initial confiner
        Debug.Log("Initial confiner: " + confiners[0].name);

        // Set the initial confiner to the first Collider2D in the list
        SwitchToConfiner(0);
    }

    // Call this method to switch to a specific confiner
    public void SwitchToConfiner(int index)
    {
        if (index >= 0 && index < confiners.Length && confiners[index] != null)
        {
            Debug.Log("Switching confiner to index: " + index + " (" + confiners[index].name + ")");

            // Assign the new Collider2D to the CinemachineConfiner's Bounding Shape 2D
            confiner.m_BoundingShape2D = confiners[index];

            // Refresh the Cinemachine Confiner
            confiner.InvalidatePathCache();

            // Update the current confiner index
            currentConfinerIndex = index;
        }
        else
        {
            Debug.LogWarning("Invalid confiner index or confiner is null!");
        }
    }

    // Call this method to switch to the next confiner in the list
    public void SwitchToNextConfiner()
    {
        int nextIndex = (currentConfinerIndex + 1) % confiners.Length;
        SwitchToConfiner(nextIndex);
    }

    // Call this method to switch to the previous confiner in the list
    public void SwitchToPreviousConfiner()
    {
        int previousIndex = (currentConfinerIndex - 1 + confiners.Length) % confiners.Length;
        SwitchToConfiner(previousIndex);
    }
}