using System;
using UnityEngine;

public enum FurnitureType
{
    Bench,
    RoundTable,
    SquareTable,
    FlowerPot,
    ClassroomDesk,
    Chair,
    AuditoriumChair,
    LoungeChair,
    EventLectern,
    MicrophoneStand,
    PaFloorSpeaker,
    RollupBanner,
    RegistrationTable
}

[Serializable]
public sealed class FurnitureDefinition
{
    public FurnitureType Type { get; private set; }
    public string StableId { get; private set; }
    public string DisplayName { get; private set; }
    public string Category { get; private set; }
    public Vector3 Dimensions { get; private set; }

    public FurnitureDefinition(FurnitureType type, string stableId, string displayName, string category, Vector3 dimensions)
    {
        Type = type;
        StableId = stableId;
        DisplayName = displayName;
        Category = category;
        Dimensions = dimensions;
    }
}

/// <summary>
/// Central dimensions and display names for all procedural furniture types.
/// Dimensions are world-space width, height, and depth in approximate metres.
/// </summary>
public static class FurnitureCatalog
{
    private static readonly FurnitureDefinition[] Definitions =
    {
        new FurnitureDefinition(FurnitureType.Bench, "bench", "Bench", "FURNITURE", new Vector3(1.8f, 0.98f, 0.62f)),
        new FurnitureDefinition(FurnitureType.AuditoriumChair, "auditorium_chair", "Auditorium Chair", "FURNITURE", new Vector3(0.48f, 0.82f, 0.55f)),
        new FurnitureDefinition(FurnitureType.Chair, "chair", "Classroom Chair", "FURNITURE", new Vector3(0.46f, 0.82f, 0.5f)),
        new FurnitureDefinition(FurnitureType.ClassroomDesk, "classroom_desk", "Classroom Desk", "FURNITURE", new Vector3(1.2f, 0.75f, 0.55f)),
        new FurnitureDefinition(FurnitureType.RoundTable, "round_table", "Round Table", "FURNITURE", new Vector3(1.3f, 0.78f, 1.3f)),
        new FurnitureDefinition(FurnitureType.SquareTable, "square_table", "Square Table", "FURNITURE", new Vector3(1.25f, 0.78f, 1.25f)),
        new FurnitureDefinition(FurnitureType.LoungeChair, "lounge_chair", "Lounge Chair", "FURNITURE", new Vector3(0.8f, 0.9f, 0.8f)),
        new FurnitureDefinition(FurnitureType.RegistrationTable, "registration_table", "Registration Table", "FURNITURE", new Vector3(1.2f, 0.75f, 0.6f)),
        new FurnitureDefinition(FurnitureType.EventLectern, "event_lectern", "Event Lectern", "STAGE & AV", new Vector3(0.55f, 1.1f, 0.5f)),
        new FurnitureDefinition(FurnitureType.MicrophoneStand, "microphone_stand", "Microphone Stand", "STAGE & AV", new Vector3(0.45f, 1.6f, 0.45f)),
        new FurnitureDefinition(FurnitureType.PaFloorSpeaker, "pa_floor_speaker", "PA Floor Speaker", "STAGE & AV", new Vector3(0.35f, 0.7f, 0.35f)),
        new FurnitureDefinition(FurnitureType.FlowerPot, "flower_pot", "Flower Pot", "DECORATIVE", new Vector3(0.62f, 1.0f, 0.62f)),
        new FurnitureDefinition(FurnitureType.RollupBanner, "rollup_banner", "Roll-up Banner Stand", "DECORATIVE", new Vector3(0.85f, 2.0f, 0.35f))
    };

    public static FurnitureDefinition Get(FurnitureType type)
    {
        for (int i = 0; i < Definitions.Length; i++)
        {
            if (Definitions[i].Type == type) return Definitions[i];
        }

        throw new ArgumentOutOfRangeException("type", type, "Furniture type is not registered in FurnitureCatalog.");
    }

    public static FurnitureDefinition[] GetAll()
    {
        return (FurnitureDefinition[])Definitions.Clone();
    }

    public static bool IsStageEquipment(FurnitureType type)
    {
        return type == FurnitureType.EventLectern
            || type == FurnitureType.MicrophoneStand
            || type == FurnitureType.PaFloorSpeaker;
    }

    public static bool TryGetById(string stableId, out FurnitureDefinition definition)
    {
        for (int i = 0; i < Definitions.Length; i++)
        {
            if (string.Equals(Definitions[i].StableId, stableId, StringComparison.OrdinalIgnoreCase))
            {
                definition = Definitions[i];
                return true;
            }
        }

        definition = null;
        return false;
    }
}
