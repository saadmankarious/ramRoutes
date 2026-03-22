using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Platformer.Mechanics;

public class SegmentedJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float deadZone = 0.2f;
    public float radius = 100f;

    public RectTransform handle;
    private Vector2 initialPosition;

    private enum Direction { None, Up, Down, Left, Right }
    private Direction currentDirection = Direction.None;

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

        direction = direction.normalized * Mathf.Clamp01(direction.magnitude / radius);

        if (direction.magnitude > deadZone)
        {
            // Clamp handle to radius
            handle.anchoredPosition = direction * radius;

            // Determine 4-way direction (no diagonals)
            Direction newDir;
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                newDir = direction.x > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                newDir = direction.y > 0 ? Direction.Up : Direction.Down;
            }

            if (newDir != currentDirection)
            {
                ReleaseCurrentDirection();
                currentDirection = newDir;
                PressDirection(currentDirection);
            }
        }
        else
        {
            handle.anchoredPosition = initialPosition;
            ReleaseCurrentDirection();
            currentDirection = Direction.None;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = initialPosition;
        ReleaseCurrentDirection();
        currentDirection = Direction.None;
    }

    private void PressDirection(Direction dir)
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        switch (dir)
        {
            case Direction.Up:    player.OnMobileUpPressed();    break;
            case Direction.Down:  player.OnMobileDownPressed();  break;
            case Direction.Left:  player.OnMobileLeftPressed();  break;
            case Direction.Right: player.OnMobileRightPressed(); break;
        }
    }

    private void ReleaseCurrentDirection()
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        switch (currentDirection)
        {
            case Direction.Up:    player.OnMobileUpReleased();    break;
            case Direction.Down:  player.OnMobileDownReleased();  break;
            case Direction.Left:  player.OnMobileLeftReleased();  break;
            case Direction.Right: player.OnMobileRightReleased(); break;
        }
    }
}