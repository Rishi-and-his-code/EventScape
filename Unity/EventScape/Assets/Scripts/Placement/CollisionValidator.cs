using UnityEngine;

namespace EventScape.Placement
{
    /// <summary>
    /// Basic collision / bounds validation hook.
    /// Actual overlap checks will be implemented once we have real environment geometry.
    /// </summary>
    public class CollisionValidator : MonoBehaviour
    {
        [Tooltip("Optional parent transform defining the auditorium bounds. If null, validator will only check overlaps.")]
        [SerializeField] private Transform auditoriumBounds;

        [Tooltip("Layer mask used to validate furniture-to-furniture / wall collisions.")]
        [SerializeField] private LayerMask blockingLayers = ~0;

        public bool IsPlacementValid(GameObject previewObject)
        {
            if (previewObject == null)
                return false;

            // Placeholder: allow everything for now.
            // Next: use Bounds/Collider bounds and Physics.OverlapBox/OverlapSphere against blockingLayers.
            return true;
        }
    }
}
