using UnityEngine;

namespace EventScape.UI
{
    /// <summary>
    /// Side panel listing furniture / decoration types.
    /// Buttons in the panel call SelectItem(...) with the matching prefab.
    /// </summary>
    public class ObjectCatalogPanel : MonoBehaviour
    {
        [SerializeField] private Placement.PlacementSystem placementSystem;

        public void SelectItem(GameObject prefab)
        {
            if (placementSystem == null)
                placementSystem = FindFirstObjectByType<Placement.PlacementSystem>();

            placementSystem?.SelectPrefab(prefab);
        }

        private void OnValidate()
        {
            // Helpful for inspector wiring.
        }
    }
}
