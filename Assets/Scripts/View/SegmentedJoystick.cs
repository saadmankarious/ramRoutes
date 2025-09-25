using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SegmentedJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public int segments = 8; // Number of sections
    public float deadZone = 0.2f;
    public float radius = 100f;
    
    [Header("Events")]
    public System.Action<int> OnSegmentChanged; // Segment index event
    
    public RectTransform handle;
    private Vector2 initialPosition;
    private int currentSegment = -1;
    
    void Start()
    {
        handle = transform.GetChild(0).GetComponent<RectTransform>();
        initialPosition = handle.anchoredPosition;
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
            
            // Trigger segment change
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
        handle.anchoredPosition = initialPosition;
        if (currentSegment != -1)
        {
            currentSegment = -1;
            OnSegmentChanged?.Invoke(-1);
        }
    }
}