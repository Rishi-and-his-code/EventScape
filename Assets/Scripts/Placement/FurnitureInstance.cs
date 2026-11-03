using System;
using UnityEngine;

/// <summary>
/// Identifies a catalog-created furniture root and whether it is still a preview.
/// </summary>
public sealed class FurnitureInstance : MonoBehaviour
{
    [SerializeField] private FurnitureType furnitureType;
    [SerializeField] private bool isPreview;
    [SerializeField] private string instanceId;

    public FurnitureType Type { get { return furnitureType; } }
    public bool IsPreview { get { return isPreview; } }
    public string InstanceId { get { return instanceId; } }

    public void Configure(FurnitureType type, bool preview)
    {
        furnitureType = type;
        isPreview = preview;
        instanceId = Guid.NewGuid().ToString("N");
    }

    public void SetInstanceId(string id)
    {
        instanceId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
    }

    public void SetPreview(bool preview)
    {
        isPreview = preview;
    }
}
