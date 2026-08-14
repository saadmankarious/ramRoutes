using Cinemachine;
using UnityEngine;

/// <summary>
/// Moves the vcam around independently of any Follow/LookAt target, using keyboard
/// input to pan and the mouse scroll wheel to zoom. Attach this to the same GameObject
/// as the CinemachineVirtualCamera (and CinemachineConfiner, if you have one) — the
/// Cinemachine Brain on the Main Camera will pick up the resulting position/lens
/// changes automatically, and the confiner (if present) keeps it inside the bounds.
///
/// Setup notes:
/// - Set the vcam's Body to "Do Nothing" so it doesn't fight this script for control
///   of the transform.
/// - Clear m_Follow / m_LookAt (or just don't assign them) so nothing else repositions
///   the vcam while this script is driving it.
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class FreeCameraController : MonoBehaviour
{
    // Singleton instance, so UI (e.g. the mobile joystick) can reach this without a scene reference
    public static FreeCameraController Instance { get; private set; }

    [Header("Pan")]
    public float panSpeed = 10f;
    public bool controlEnabled = true;

    [Header("Zoom (mouse wheel)")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;

    [Header("Zoom (mobile pinch)")]
    public float pinchZoomSpeed = 0.01f;

    private CinemachineVirtualCamera vcam;
    private Vector2 moveInput;

    // Mobile joystick input variables (driven by SegmentedJoystick's OnMobile*Pressed/Released calls)
    private bool mobileLeftPressed = false;
    private bool mobileRightPressed = false;
    private bool mobileUpPressed = false;
    private bool mobileDownPressed = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        vcam = GetComponent<CinemachineVirtualCamera>();

        if (vcam.m_Follow != null || vcam.m_LookAt != null)
        {
            Debug.LogWarning("FreeCameraController: vcam still has a Follow/LookAt target assigned. " +
                "Clear them and set the vcam's Body to 'Do Nothing' or this script's movement will be overridden.");
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (!controlEnabled)
        {
            moveInput = Vector2.zero;
            return;
        }

        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");

        // Override with mobile joystick input if pressed
        if (mobileLeftPressed || mobileRightPressed || mobileUpPressed || mobileDownPressed)
        {
            inputX = 0f;
            inputY = 0f;

            if (mobileLeftPressed) inputX = -1f;
            if (mobileRightPressed) inputX = 1f;
            if (mobileDownPressed) inputY = -1f;
            if (mobileUpPressed) inputY = 1f;
        }

        moveInput = new Vector2(inputX, inputY).normalized;

        if (moveInput.sqrMagnitude < 0.0001f) return;

        Vector3 pan = new Vector3(moveInput.x, moveInput.y, 0f) * panSpeed * Time.deltaTime;
        transform.position += pan;
    }

    private void HandleZoom()
    {
        if (Input.touchCount == 2)
        {
            HandlePinchZoom();
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.0001f) return;

        float currentSize = vcam.m_Lens.OrthographicSize;
        float newSize = currentSize - scroll * zoomSpeed;
        vcam.m_Lens.OrthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
    }

    private void HandlePinchZoom()
    {
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

        float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

        float pinchDelta = currentMagnitude - prevMagnitude;
        if (Mathf.Abs(pinchDelta) < 0.0001f) return;

        float currentSize = vcam.m_Lens.OrthographicSize;
        // Fingers spreading apart (positive delta) should zoom in, hence the minus sign.
        float newSize = currentSize - pinchDelta * pinchZoomSpeed;
        vcam.m_Lens.OrthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
    }

    // Mobile joystick input methods, driven by SegmentedJoystick's OnMobile*Pressed/Released calls
    public void OnMobileLeftPressed() { mobileLeftPressed = true; }
    public void OnMobileLeftReleased() { mobileLeftPressed = false; }
    public void OnMobileRightPressed() { mobileRightPressed = true; }
    public void OnMobileRightReleased() { mobileRightPressed = false; }
    public void OnMobileUpPressed() { mobileUpPressed = true; }
    public void OnMobileUpReleased() { mobileUpPressed = false; }
    public void OnMobileDownPressed() { mobileDownPressed = true; }
    public void OnMobileDownReleased() { mobileDownPressed = false; }
}
