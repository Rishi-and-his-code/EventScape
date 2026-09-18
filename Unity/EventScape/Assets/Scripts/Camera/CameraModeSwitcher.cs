using UnityEngine;
using UnityEngine.InputSystem;

namespace EventScape.Camera
{
    /// <summary>
    /// Toggles between first-person walkthrough and orbit/top-down planning view.
    /// Default hotkey: Tab.
    /// </summary>
    public class CameraModeSwitcher : MonoBehaviour
    {
        public enum CameraMode
        {
            FirstPerson,
            Orbit
        }

        [SerializeField] private FirstPersonController firstPerson;
        [SerializeField] private OrbitCameraController orbit;
        [SerializeField] private CameraMode startingMode = CameraMode.Orbit;

        private CameraMode _current;

        private void Start()
        {
            SetMode(startingMode);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            SetMode(_current == CameraMode.FirstPerson ? CameraMode.Orbit : CameraMode.FirstPerson);
        }

        public void SetMode(CameraMode mode)
        {
            _current = mode;

            if (firstPerson != null)
            {
                if (mode == CameraMode.FirstPerson)
                    firstPerson.Activate();
                else
                    firstPerson.Deactivate();
            }

            if (orbit != null)
            {
                if (mode == CameraMode.Orbit)
                    orbit.Activate();
                else
                    orbit.Deactivate();
            }
        }

        public CameraMode CurrentMode => _current;
    }
}
