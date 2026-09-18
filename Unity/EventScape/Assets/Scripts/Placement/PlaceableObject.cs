using UnityEngine;

namespace EventScape.Placement
{
    /// <summary>
    /// Marker / wrapper for objects that can be placed and manipulated.
    /// Attach to furniture prefabs.
    /// </summary>
    public class PlaceableObject : MonoBehaviour
    {
        [SerializeField] private string objectTypeId = "";

        public string ObjectTypeId => objectTypeId;

        private void Reset()
        {
            // Auto-populate with a sensible name when placed in prefabs.
            if (string.IsNullOrWhiteSpace(objectTypeId))
                objectTypeId = gameObject.name;
        }
    }
}
