using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using RamRoutes.Services;
public class SegmentedJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public int segments = 8; // Number of sections
    public float deadZone = 0.2f;
    public float radius = 100f;
    
    [Header("Events")]
    public System.Action<int> OnSegmentChanged; // Segment index event
    
    [Header("Building Associations")]
    // Building names for each segment: 0: TC, 1: PR, 2: McWethy, 3: SAW, 4: Stoner, 5: Ebersole, 6: Library
    private string[] buildingNames = { "PR", "TC", "Ebersole", "Library", "Stoner", "McWethy", "SAW" };
    
    [Header("Audio")]
    public AudioClip teleportSound; // Sound to play when teleportation begins
    
    public RectTransform handle;
    private Vector2 initialPosition;
    private int currentSegment = -1;
    private int selectedSegment = -1; // The segment selected when released
    private bool isMoving = false; // Prevent multiple teleportations
    private float lastTeleportTime = 0f;
    private float teleportCooldown = 1.5f; // Minimum time between teleportations
    
    void Start()
    {
        handle = transform.GetChild(0).GetComponent<RectTransform>();
        initialPosition = handle.anchoredPosition;
        
        // Check game stage and show/hide wheel accordingly
        CheckGameStageVisibility();
        
        // Check stage periodically in case it changes during gameplay
        InvokeRepeating(nameof(CheckGameStageVisibility), 1f, 2f);
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 direction;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out direction
        );
        
        // Normalize direction
        direction = direction.normalized * Mathf.Clamp01(direction.magnitude / radius);
        
        if (direction.magnitude > deadZone)
        {
            // Calculate segment
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            
            int segment = Mathf.FloorToInt(angle / (360f / segments));
            
            // Update handle position
            handle.anchoredPosition = direction * radius;
            
            // Trigger segment change (but don't teleport yet)
            if (segment != currentSegment)
            {
                currentSegment = segment;
                OnSegmentChanged?.Invoke(currentSegment);
                Debug.Log($"Segment: {currentSegment}");
            }
        }
        else
        {
            handle.anchoredPosition = initialPosition;
            if (currentSegment != -1)
            {
                currentSegment = -1;
                OnSegmentChanged?.Invoke(-1); // No segment selected
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        // Store the selected segment before resetting
        selectedSegment = currentSegment;
        
        handle.anchoredPosition = initialPosition;
        
        // Teleport to building if a valid segment was selected and cooldown has passed
        if (selectedSegment >= 0 && selectedSegment < buildingNames.Length && 
            !isMoving && Time.time - lastTeleportTime >= teleportCooldown)
        {
            TeleportPlayerToBuilding(buildingNames[selectedSegment]);
            lastTeleportTime = Time.time;
        }
        
        if (currentSegment != -1)
        {
            currentSegment = -1;
            OnSegmentChanged?.Invoke(-1);
        }
    }
    
    /// <summary>
    /// Teleport player to the specified building
    /// </summary>
    private void TeleportPlayerToBuilding(string buildingName)
    {
        // Prevent multiple simultaneous teleportations
        if (isMoving) return;
        
        // Find the building by name
        BuildingInteraction building = FindBuildingByName(buildingName);
        if (building != null)
        {
            isMoving = true;
            
            // Play teleport sound when teleportation begins
            if (teleportSound != null)
            {
                AudioSource.PlayClipAtPoint(teleportSound, Camera.main.transform.position);
            }
            
            StartCoroutine(MovePlayerToBuildingSmooth(building));
            Debug.Log($"Teleporting player to building: {buildingName}");
        }
        else
        {
            Debug.LogWarning($"Building '{buildingName}' not found for teleportation");
        }
    }
    
    /// <summary>
    /// Find a BuildingInteraction component by building name
    /// </summary>
    private BuildingInteraction FindBuildingByName(string buildingName)
    {
        BuildingInteraction[] allBuildings = FindObjectsOfType<BuildingInteraction>();
        foreach (var building in allBuildings)
        {
            if (building.buildingName == buildingName)
            {
                return building;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Smoothly moves the player to the specified building (copied from BuildingInteraction)
    /// </summary>
    private System.Collections.IEnumerator MovePlayerToBuildingSmooth(BuildingInteraction building)
    {
        GameObject player = GameObject.FindWithTag("Player");
        
        if (player != null && building != null)
        {
            // Check if target point is assigned, otherwise fallback to building position
            Vector3 targetPosition = building.playerTargetPoint != null ? building.playerTargetPoint.transform.position : building.transform.position;
            Vector3 startPosition = player.transform.position;
            float duration = 2.0f; // 2 seconds for smooth movement
            float elapsedTime = 0f;
            
            string targetName = building.playerTargetPoint != null ? building.playerTargetPoint.name : building.buildingName;
            Debug.Log($"Starting smooth movement to target '{targetName}' from {startPosition} to {targetPosition}");
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                // Use smooth step for eased movement
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
                
                // Interpolate position
                player.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
                
                yield return null; // Wait one frame
            }
            
            // Ensure we end exactly at target position
            player.transform.position = targetPosition;
            Debug.Log($"Completed smooth movement to target '{targetName}' at position {targetPosition}");
        }
        else
        {
            Debug.LogWarning("Could not find player object with 'Player' tag to move");
        }
        
        // Reset movement flag
        isMoving = false;
    }
    
    /// <summary>
    /// Check current game stage and show/hide wheel accordingly
    /// </summary>
    private void CheckGameStageVisibility()
    {
        var currentStage = GameStageService.LoadStageFromPrefs();
        bool shouldShowWheel = currentStage != null && currentStage.area == RamRoutes.Model.Stage.Terminal;
        
        // Show/hide the entire joystick gameObject
        gameObject.SetActive(shouldShowWheel);
        
        if (shouldShowWheel)
        {
            Debug.Log("SegmentedJoystick: Showing wheel - Terminal stage active");
        }
        else
        {
            Debug.Log($"SegmentedJoystick: Hiding wheel - Current stage: {currentStage?.area}");
        }
    }
}