using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable JSON data model for a saved auditorium layout.
/// </summary>
[Serializable]
public sealed class AuditoriumLayoutData
{
    public int formatVersion = 1;
    // Named-layout saves fill this explicitly; empty allows legacy files without metadata to use a filename fallback.
    public string layoutName = "";
    // Optional version-1 metadata used only by user-created named layout files.
    public string description;
    public string createdUtc;
    public string modifiedUtc;
    public List<FurniturePlacementRecord> placements = new List<FurniturePlacementRecord>();
}

[Serializable]
public struct FurniturePlacementRecord
{
    // Optional in formatVersion 1: older files omit this field and receive a new ID when loaded.
    public string instanceId;
    public string furnitureId;
    public Vector3 position;
    public Vector3 rotationEuler;
    public Vector3 scale;
}
