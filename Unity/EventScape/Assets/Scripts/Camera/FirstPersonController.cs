using UnityEngine;
using UnityEngine.InputSystem;

namespace EventScape.Camera
{
    /// <summary>
    /// WASD movement + mouse look with basic wall/floor collision.
    /// Attach to the first-person camera / player root.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        private CharacterController _controller;
        private float _pitch;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;

            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            if (Mouse.current == null)
                return;

            Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y, minPitch, maxPitch);
        }

        private void HandleMove()
        {
            if (Keyboard.current == null)
                return;

            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;

            Vector3 direction = (transform.forward * input.y + transform.right * input.x).normalized;
            Vector3 velocity = direction * moveSpeed;
            velocity.y = Physics.gravity.y;
            _controller.Move(velocity * Time.deltaTime);
        }

        public void Activate()
        {
            enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Deactivate()
        {
            enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
