using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Orbit camera for inspecting the auditorium and planned layouts from above.
/// The pivot remains fixed at the planning center; orbit and zoom retain state
/// while the user switches to walkthrough mode.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class PlanningCameraController : MonoBehaviour
{
    [SerializeField] private Vector3 pivot = new Vector3(0f, 1.7f, 3f);
    [SerializeField, Range(-180f, 180f)] private float yawDegrees;
    [SerializeField, Range(5f, 82f)] private float pitchDegrees = 56f;
    [SerializeField, Min(12f)] private float distance = 40f;
    [SerializeField, Min(12f)] private float minimumZoom = 13f;
    [SerializeField, Min(20f)] private float maximumZoom = 56f;
    [SerializeField, Min(0.01f)] private float orbitSensitivity = 0.16f;
    [SerializeField, Min(0.1f)] private float zoomSensitivity = 2.4f;

    private Vector3 initialPivot;
    private float initialYaw;
    private float initialPitch;
    private float initialDistance;
    private Vector3 previousMousePosition;
    private bool isOrbiting;
    private bool inputBlocked;

    public void Configure(Vector3 lookAtPoint, float yaw, float pitch, float viewDistance, float minZoom, float maxZoom)
    {
        pivot = lookAtPoint;
        yawDegrees = yaw;
        pitchDegrees = pitch;
        distance = viewDistance;
        minimumZoom = minZoom;
        maximumZoom = maxZoom;
        RememberInitialView();
        ApplyCameraPose();
    }

    private void Awake()
    {
        distance = Mathf.Clamp(distance, minimumZoom, maximumZoom);
        pitchDegrees = Mathf.Clamp(pitchDegrees, 5f, 82f);
        RememberInitialView();
        ApplyCameraPose();
    }

    private void OnEnable()
    {
        previousMousePosition = Input.mousePosition;
        isOrbiting = false;
    }

    private void Update()
    {
        if (inputBlocked)
        {
            isOrbiting = false;
            previousMousePosition = Input.mousePosition;
            return;
        }

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (pointerOverUi)
        {
            isOrbiting = false;
            previousMousePosition = Input.mousePosition;
            return;
        }

        HandleOrbit();
        HandleZoom();

        if (!HasFocusedTextInput() && Input.GetKeyDown(KeyCode.R))
        {
            ResetView();
        }
    }

    public void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;
        isOrbiting = false;
        previousMousePosition = Input.mousePosition;
    }

    public void SetTopView()
    {
        yawDegrees = 0f;
        pitchDegrees = 82f;
        distance = Mathf.Clamp(initialDistance, minimumZoom, maximumZoom);
        ApplyCameraPose();
    }

    public void SetFrontView()
    {
        yawDegrees = 0f;
        pitchDegrees = 8f;
        distance = Mathf.Clamp(Mathf.Min(initialDistance, 11f), minimumZoom, maximumZoom);
        ApplyCameraPose();
    }

    public void SetIsometricView()
    {
        yawDegrees = 38f;
        pitchDegrees = 42f;
        distance = Mathf.Clamp(initialDistance, minimumZoom, maximumZoom);
        ApplyCameraPose();
    }

    private void LateUpdate()
    {
        ApplyCameraPose();
    }

    private void HandleOrbit()
    {
        if (!Input.GetMouseButton(1))
        {
            isOrbiting = false;
            previousMousePosition = Input.mousePosition;
            return;
        }

        Vector3 currentMousePosition = Input.mousePosition;
        if (!isOrbiting || Input.GetMouseButtonDown(1))
        {
            previousMousePosition = currentMousePosition;
            isOrbiting = true;
            return;
        }

        Vector3 mouseDelta = currentMousePosition - previousMousePosition;
        previousMousePosition = currentMousePosition;
        yawDegrees += mouseDelta.x * orbitSensitivity;
        pitchDegrees = Mathf.Clamp(pitchDegrees - mouseDelta.y * orbitSensitivity, 5f, 82f);
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minimumZoom, maximumZoom);
        }
    }

    private void ApplyCameraPose()
    {
        float pitchRadians = pitchDegrees * Mathf.Deg2Rad;
        float yawRadians = yawDegrees * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(
            Mathf.Sin(yawRadians) * Mathf.Cos(pitchRadians),
            Mathf.Sin(pitchRadians),
            -Mathf.Cos(yawRadians) * Mathf.Cos(pitchRadians));

        transform.position = pivot + direction * distance;
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    private void RememberInitialView()
    {
        initialPivot = pivot;
        initialYaw = yawDegrees;
        initialPitch = pitchDegrees;
        initialDistance = distance;
    }

    public void ResetView()
    {
        pivot = initialPivot;
        yawDegrees = initialYaw;
        pitchDegrees = initialPitch;
        distance = Mathf.Clamp(initialDistance, minimumZoom, maximumZoom);
        ApplyCameraPose();
    }

    private static bool HasFocusedTextInput()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.GetComponentInParent<UnityEngine.UI.InputField>() != null;
    }
}
