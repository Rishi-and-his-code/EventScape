using UnityEngine;

namespace EventScape.Placement
{
    /// <summary>
    /// Semi-transparent preview object that follows the mouse raycast.
    /// </summary>
    public class GhostPreview : MonoBehaviour
    {
        [SerializeField] private CollisionValidator collisionValidator;

        private GameObject _currentGhost;
        private bool _valid;

        public void SetGhost(GameObject prefab)
        {
            if (_currentGhost != null)
            {
                Destroy(_currentGhost);
                _currentGhost = null;
            }

            if (prefab == null)
                return;

            _currentGhost = Instantiate(prefab, transform.position, transform.rotation, transform);
        }

        public void UpdateGhostPose(Vector3 position, Quaternion rotation)
        {
            if (_currentGhost == null)
                return;

            _currentGhost.transform.SetPositionAndRotation(position, rotation);

            _valid = collisionValidator == null ? true : collisionValidator.IsPlacementValid(_currentGhost);
            ApplyValidityColor(_valid);
        }

        private void ApplyValidityColor(bool valid)
        {
            // Lightweight placeholder: this will be improved with materials per ghost prefab.
            // For now we only toggle renderer enabled state when we have colliders/materials.
            var renderers = _currentGhost.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.enabled = true;
        }

        public bool IsValid => _valid;
        public GameObject GhostInstance => _currentGhost;

        public void Hide()
        {
            if (_currentGhost != null)
                _currentGhost.SetActive(false);
        }

        public void Show()
        {
            if (_currentGhost != null)
                _currentGhost.SetActive(true);
        }
    }
}
