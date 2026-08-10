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
            // Determine 4-way direction (no diagonals)
            Direction newDir;
            Vector2 snappedDirection;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                newDir = direction.x > 0 ? Direction.Right : Direction.Left;
                snappedDirection = new Vector2(Mathf.Sign(direction.x), 0f);
            }
            else
            {
                newDir = direction.y > 0 ? Direction.Up : Direction.Down;
                snappedDirection = new Vector2(0f, Mathf.Sign(direction.y));
            }

            // Snap handle to cardinal direction only
            handle.anchoredPosition = snappedDirection * radius;

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
        var camera = FreeCameraController.Instance;

        switch (dir)
        {
            case Direction.Up:
                player?.OnMobileUpPressed();
                camera?.OnMobileUpPressed();
                break;
            case Direction.Down:
                player?.OnMobileDownPressed();
                camera?.OnMobileDownPressed();
                break;
            case Direction.Left:
                player?.OnMobileLeftPressed();
                camera?.OnMobileLeftPressed();
                break;
            case Direction.Right:
                player?.OnMobileRightPressed();
                camera?.OnMobileRightPressed();
                break;
        }
    }

    private void ReleaseCurrentDirection()
    {
        var player = PlayerController.Instance;
        var camera = FreeCameraController.Instance;

        switch (currentDirection)
        {
            case Direction.Up:
                player?.OnMobileUpReleased();
                camera?.OnMobileUpReleased();
                break;
            case Direction.Down:
                player?.OnMobileDownReleased();
                camera?.OnMobileDownReleased();
                break;
            case Direction.Left:
                player?.OnMobileLeftReleased();
                camera?.OnMobileLeftReleased();
                break;
            case Direction.Right:
                player?.OnMobileRightReleased();
                camera?.OnMobileRightReleased();
                break;
        }
    }
}