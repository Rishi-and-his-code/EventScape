using UnityEngine;

/// <summary>Controls the optional fixed reference chairs, separate from planner furniture.</summary>
public sealed class ReferenceSeatingController : MonoBehaviour
{
    [SerializeField] private GameObject seatingRoot;

    public bool IsVisible { get { return seatingRoot != null && seatingRoot.activeSelf; } }

    public void Configure(GameObject root, bool visible)
    {
        seatingRoot = root != null ? root : gameObject;
        seatingRoot.SetActive(visible);
    }

    public void Toggle()
    {
        SetVisible(!IsVisible);
    }

    public void SetVisible(bool visible)
    {
        if (seatingRoot != null) seatingRoot.SetActive(visible);
    }
}
