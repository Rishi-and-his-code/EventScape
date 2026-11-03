using UnityEngine;
using UnityEngine.Events;

namespace EventScape.UI
{
    /// <summary>
    /// A UI button that maps to a prefab type.
    /// </summary>
    public class CatalogButton : MonoBehaviour
    {
        [SerializeField] private GameObject prefabToSelect;
        [SerializeField] private ObjectCatalogPanel panel;

        public void OnClick()
        {
            if (panel == null)
                panel = FindFirstObjectByType<ObjectCatalogPanel>();

            panel?.SelectItem(prefabToSelect);
        }
    }
}
