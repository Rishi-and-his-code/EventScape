using UnityEngine;
using UnityEngine.InputSystem;

namespace EventScape.Placement
{
    /// <summary>
    /// Core placement loop:
    /// - raycast from camera to placement surface
    /// - update ghost preview
    /// - rotate ghost
    /// - click to instantiate final object
    /// </summary>
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private LayerMask placementSurfaceLayers = ~0;
        [SerializeField] private float raycastMaxDistance = 200f;

        [Header("Ghost")]
        [SerializeField] private GhostPreview ghostPreview;

        [Header("Prefabs")]
        [SerializeField] private GameObject currentPrefab;

        [Header("Rotation")]
        [SerializeField] private float rotationStepDegrees = 15f;

        private float _currentYaw;

        private void Reset()
        {
            sceneCamera = Camera.main;
        }

        private void Update()
        {
            if (ghostPreview == null || sceneCamera == null)
                return;

            HandleRaycastAndGhost();
            HandleRotationInput();
            HandlePlaceInput();
        }

        public void SelectPrefab(GameObject prefab)
        {
            currentPrefab = prefab;
            _currentYaw = 0f;

            if (ghostPreview != null)
                ghostPreview.SetGhost(currentPrefab);
        }

        private void HandleRaycastAndGhost()
        {
            if (currentPrefab == null)
                return;

            Ray ray = sceneCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, placementSurfaceLayers))
                return;

            Vector3 position = hit.point;
            Quaternion rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            ghostPreview.UpdateGhostPose(position, rotation);
        }

        private void HandleRotationInput()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.qKey.wasPressedThisFrame)
                _currentYaw -= rotationStepDegrees;

            if (Keyboard.current.eKey.wasPressedThisFrame)
                _currentYaw += rotationStepDegrees;
        }

        private void HandlePlaceInput()
        {
            if (Mouse.current == null || currentPrefab == null)
                return;

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (ghostPreview != null && !ghostPreview.IsValid)
                return;

            // Instantiate real object aligned to ghost pose.
            var ghost = ghostPreview.GhostInstance;
            if (ghost == null)
                return;

            Instantiate(currentPrefab, ghost.transform.position, ghost.transform.rotation);
        }
    }
}
