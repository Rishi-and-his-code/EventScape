using UnityEngine;
using UnityEngine.InputSystem;

namespace EventScape.Camera
{
    /// <summary>
    /// Top-down / orbit planning camera: drag to rotate, scroll to zoom.
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 25f;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 60f;
        [SerializeField] private float orbitSensitivity = 0.2f;
        [SerializeField] private float zoomSensitivity = 2f;
        [SerializeField] private float minPitch = 20f;
        [SerializeField] private float maxPitch = 85f;

        private float _yaw = 45f;
        private float _pitch = 55f;

        private void LateUpdate()
        {
            if (!isActiveAndEnabled || target == null)
                return;

            HandleOrbit();
            HandleZoom();
            ApplyTransform();
        }

        private void HandleOrbit()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
                return;

            Vector2 delta = Mouse.current.delta.ReadValue() * orbitSensitivity;
            _yaw += delta.x;
            _pitch = Mathf.Clamp(_pitch - delta.y, minPitch, maxPitch);
        }

        private void HandleZoom()
        {
            if (Mouse.current == null)
                return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            distance = Mathf.Clamp(distance - scroll * zoomSensitivity * 0.01f, minDistance, maxDistance);
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            transform.position = target.position + offset;
            transform.LookAt(target.position);
        }

        public void Activate()
        {
            enabled = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Deactivate()
        {
            enabled = false;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
