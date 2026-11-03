using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// First-person desktop walkthrough with a collision-aware CharacterController.
/// Right mouse drag controls look; WASD moves relative to the view direction.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class WalkthroughCameraController : MonoBehaviour
{
    private const float DemoWalkSpeed = 2f;
    private const float DemoFastWalkSpeed = 3f;
    private const float DemoCharacterHeight = 1.75f;
    private const float DemoCharacterRadius = 0.28f;
    private const float DemoEyeHeight = 1.62f;
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0.5f)] private float walkSpeed = 2f;
    [SerializeField, Min(0.5f)] private float fastWalkSpeed = 3f;
    [SerializeField, Min(1f)] private float characterHeight = 1.75f;
    [SerializeField, Min(0.1f)] private float characterRadius = 0.28f;
    [SerializeField, Min(1f)] private float eyeHeight = 1.62f;
    [SerializeField, Min(0.1f)] private float mouseSensitivity = 2.4f;
    [SerializeField, Min(1f)] private float gravity = 22f;
    [SerializeField, Range(1f, 89f)] private float slopeLimit = 48f;
    [SerializeField, Min(0f)] private float stepOffset = 0.28f;
    [SerializeField, Min(0.01f)] private float skinWidth = 0.06f;
    [SerializeField, Range(10f, 85f)] private float pitchLimit = 82f;
    [SerializeField] private float yawDegrees;
    [SerializeField] private float pitchDegrees;

    private CharacterController characterController;
    private float verticalVelocity;
    private bool isLooking;
    private bool inputBlocked;
    private Vector3 initialPosition;
    private float initialYaw;
    private float initialPitch;

    public void Configure(Camera camera, float initialYaw, float initialPitch)
    {
        playerCamera = camera;
        yawDegrees = initialYaw;
        pitchDegrees = Mathf.Clamp(initialPitch, -pitchLimit, pitchLimit);
        RememberInitialView();
        ApplyLookRotation();
    }

    /// <summary>Updates serialized settings on existing scene rigs to the current visitor-preview defaults.</summary>
    public void ConfigureDemoMovement()
    {
        walkSpeed = DemoWalkSpeed;
        fastWalkSpeed = DemoFastWalkSpeed;
        characterHeight = DemoCharacterHeight;
        characterRadius = DemoCharacterRadius;
        eyeHeight = DemoEyeHeight;
        stepOffset = 0.28f;
        slopeLimit = 48f;
        skinWidth = 0.06f;

        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (characterController != null)
        {
            characterController.height = characterHeight;
            characterController.radius = characterRadius;
            characterController.center = new Vector3(0f, characterHeight * 0.5f, 0f);
            characterController.stepOffset = stepOffset;
            characterController.slopeLimit = slopeLimit;
            characterController.skinWidth = skinWidth;
        }
        if (playerCamera != null) playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        RememberInitialView();
        ApplyLookRotation();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        characterController.height = characterHeight;
        characterController.radius = characterRadius;
        characterController.center = new Vector3(0f, characterHeight * 0.5f, 0f);
        characterController.slopeLimit = slopeLimit;
        characterController.stepOffset = stepOffset;
        characterController.skinWidth = skinWidth;
        RememberInitialView();
        if (playerCamera != null)
            playerCamera.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        ApplyLookRotation();
    }

    private void OnEnable()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private void OnDisable()
    {
        isLooking = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }

    private void Update()
    {
        if (inputBlocked) return;

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool focusedText = HasFocusedTextInput();
        HandleLook(pointerOverUi || focusedText || !Application.isFocused);

        float deltaTime = Time.unscaledDeltaTime;
        Vector3 horizontalMotion = Vector3.zero;
        if (!pointerOverUi && !focusedText && Application.isFocused)
        {
            horizontalMotion = ReadWalkInput();
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -1.5f;
        }

        verticalVelocity -= gravity * deltaTime;
        float activeSpeed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? fastWalkSpeed : walkSpeed;
        Vector3 motion = horizontalMotion * activeSpeed + Vector3.up * verticalVelocity;
        characterController.Move(motion * deltaTime);
    }

    public void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;
        if (blocked)
        {
            isLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResetView()
    {
        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled) characterController.enabled = false;
        transform.position = initialPosition;
        yawDegrees = initialYaw;
        pitchDegrees = initialPitch;
        verticalVelocity = 0f;
        ApplyLookRotation();
        if (wasEnabled) characterController.enabled = true;
    }

    private void RememberInitialView()
    {
        initialPosition = transform.position;
        initialYaw = yawDegrees;
        initialPitch = pitchDegrees;
    }

    private Vector3 ReadWalkInput()
    {
        float horizontal = 0f;
        float forward = 0f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.S)) forward -= 1f;
        if (Input.GetKey(KeyCode.W)) forward += 1f;

        Vector3 localInput = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, forward), 1f);
        return Quaternion.Euler(0f, yawDegrees, 0f) * localInput;
    }

    private void HandleLook(bool pointerOverUi)
    {
        if (pointerOverUi)
        {
            isLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (!Input.GetMouseButton(1))
        {
            isLooking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (!isLooking || Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isLooking = true;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yawDegrees += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitchDegrees = Mathf.Clamp(pitchDegrees - Input.GetAxis("Mouse Y") * mouseSensitivity, -pitchLimit, pitchLimit);
        ApplyLookRotation();
    }

    private void ApplyLookRotation()
    {
        transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
        }
    }

    private static bool HasFocusedTextInput()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.GetComponentInParent<UnityEngine.UI.InputField>() != null;
    }
}
