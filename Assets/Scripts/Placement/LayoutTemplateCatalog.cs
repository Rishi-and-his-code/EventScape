using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TemplateFurniturePlacement
{
    public FurnitureType furnitureType;
    public Vector3 auditoriumLocalPosition;
    public float localYawDegrees;
    public Vector3 scale;

    public TemplateFurniturePlacement(FurnitureType type, Vector3 position, float yawDegrees)
    {
        furnitureType = type;
        auditoriumLocalPosition = position;
        localYawDegrees = yawDegrees;
        scale = Vector3.one;
    }
}

/// <summary>One generated, data-backed preset. Positions are relative to the auditorium root.</summary>
[Serializable]
public sealed class LayoutTemplateDefinition
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public string Description { get; private set; }
    public TemplateFurniturePlacement[] Placements { get; private set; }

    public LayoutTemplateDefinition(string id, string name, string category, string description,
        List<TemplateFurniturePlacement> placements)
    {
        Id = id;
        Name = name;
        Category = category;
        Description = description;
        Placements = placements.ToArray();
    }

    public int CountOf(FurnitureType type)
    {
        int count = 0;
        for (int i = 0; i < Placements.Length; i++)
            if (Placements[i].furnitureType == type) count++;
        return count;
    }

    public int TotalCount { get { return Placements.Length; } }
}

/// <summary>
/// Generates the three immutable bundled event presets from catalog dimensions and
/// the configured room/stage frame. No preset is stored in or overwritten by user files.
/// </summary>
public static class LayoutTemplateCatalog
{
    private const string AuditoriumChairId = "auditorium_chair";
    private const string ClassroomChairId = "chair";

    public static LayoutTemplateDefinition[] Generate(AuditoriumDemoSettings settings,
        Transform auditoriumRoot, Transform stageTransform)
    {
        if (settings == null) throw new ArgumentNullException("settings");
        if (auditoriumRoot == null) throw new ArgumentNullException("auditoriumRoot");
        if (stageTransform == null) throw new ArgumentNullException("stageTransform");

        Vector3 forward = auditoriumRoot.InverseTransformDirection(stageTransform.forward).normalized;
        forward.y = 0f;
        forward.Normalize();
        if (forward.sqrMagnitude < 0.9f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float stageCenterForward = Vector3.Dot(auditoriumRoot.InverseTransformPoint(stageTransform.position), forward);
        Vector3 stageScale = stageTransform.lossyScale;
        float stageDepth = settings.stageDepth * Mathf.Abs(stageScale.z);
        float stageFront = stageCenterForward - stageDepth * 0.5f;
        float roomHalfWidth = settings.hallWidth * 0.5f;
        float rearLimit = -settings.hallDepth * 0.5f + settings.templateRearClearance;
        float frontLimit = stageFront - settings.templateFrontClearance;
        float usableLength = Mathf.Max(0f, frontLimit - rearLimit);

        FurnitureType auditoriumChair = ResolveType(AuditoriumChairId, FurnitureType.Chair);
        FurnitureType classroomChair = ResolveType(ClassroomChairId, FurnitureType.Chair);
        LayoutTemplateDefinition theatre = BuildTheatre(settings, auditoriumChair, forward, right,
            roomHalfWidth, rearLimit, frontLimit, usableLength);
        LayoutTemplateDefinition classroom = BuildClassroom(settings, classroomChair, forward, right,
            roomHalfWidth, rearLimit, frontLimit, usableLength);
        LayoutTemplateDefinition conference = BuildConference(settings, auditoriumChair, forward, right,
            roomHalfWidth, rearLimit, frontLimit, usableLength);
        return new[] { theatre, classroom, conference };
    }

    private static LayoutTemplateDefinition BuildTheatre(AuditoriumDemoSettings settings, FurnitureType chair,
        Vector3 forward, Vector3 right, float roomHalfWidth, float rearLimit, float frontLimit, float usableLength)
    {
        List<TemplateFurniturePlacement> items = new List<TemplateFurniturePlacement>();
        FurnitureDefinition chairDefinition = FurnitureCatalog.Get(chair);
        float centralAisle = settings.templateCentralAisleWidth;
        float sideAisle = settings.templateSideAisleClearance;
        float blockWidth = Mathf.Max(0f, (settings.hallWidth - 2f * sideAisle - centralAisle) * 0.5f);
        float gap = 0.16f;
        int perBlock = Mathf.Max(0, Mathf.FloorToInt((blockWidth + gap) / (chairDefinition.Dimensions.x + gap)));
        int rowCount = Mathf.Min(Mathf.Clamp(settings.theatreTemplateRows, 4, 10),
            Mathf.Max(0, Mathf.FloorToInt((usableLength + gap) / Mathf.Max(0.95f, chairDefinition.Dimensions.z + 0.35f))));
        float rowPitch = Mathf.Max(chairDefinition.Dimensions.z + 0.35f, 1.35f);
        float rowSpan = Mathf.Max(0f, (rowCount - 1) * rowPitch);
        float rowStart = Mathf.Clamp((frontLimit + rearLimit + rowSpan) * 0.5f,
            rearLimit + rowSpan * 0.5f, frontLimit - rowSpan * 0.5f);
        float leftCenter = -centralAisle * 0.5f - blockWidth * 0.5f;
        float rightCenter = centralAisle * 0.5f + blockWidth * 0.5f;
        AddChairBlock(items, chair, forward, right, leftCenter, rowStart, perBlock, rowCount, gap, rowPitch);
        AddChairBlock(items, chair, forward, right, rightCenter, rowStart, perBlock, rowCount, gap, rowPitch);

        AddOptional(items, FurnitureType.PaFloorSpeaker, right * -6.5f + forward * (frontLimit - 0.3f));
        AddOptional(items, FurnitureType.PaFloorSpeaker, right * 6.5f + forward * (frontLimit - 0.3f));
        AddOptional(items, FurnitureType.FlowerPot, right * -7.2f + forward * (frontLimit - 0.4f));
        AddOptional(items, FurnitureType.FlowerPot, right * 7.2f + forward * (frontLimit - 0.4f));

        return new LayoutTemplateDefinition("theatre_presentation", "THEATRE / PRESENTATION", "SEMINAR · LECTURE",
            "Stage-facing seating in two blocks with a wide central aisle and clear side circulation. " +
            CountLabel(items, FurnitureCatalog.Get(chair).DisplayName) + " calculated from room and chair footprints.", items);
    }

    private static void AddChairBlock(List<TemplateFurniturePlacement> items, FurnitureType chair,
        Vector3 forward, Vector3 right, float blockCenter, float rowStart, int columns, int rows, float gap, float rowPitch)
    {
        float chairWidth = FurnitureCatalog.Get(chair).Dimensions.x;
        float spacing = chairWidth + gap;
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            float x = blockCenter + (column - (columns - 1) * 0.5f) * spacing;
            float forwardDistance = rowStart - row * rowPitch;
            items.Add(new TemplateFurniturePlacement(chair, right * x + forward * forwardDistance,
                YawForDirection(forward)));
        }
    }

    private static LayoutTemplateDefinition BuildClassroom(AuditoriumDemoSettings settings, FurnitureType chair,
        Vector3 forward, Vector3 right, float roomHalfWidth, float rearLimit, float frontLimit, float usableLength)
    {
        List<TemplateFurniturePlacement> items = new List<TemplateFurniturePlacement>();
        FurnitureDefinition deskDefinition = FurnitureCatalog.Get(FurnitureType.ClassroomDesk);
        FurnitureDefinition chairDefinition = FurnitureCatalog.Get(chair);
        float aisle = settings.templateCentralAisleWidth;
        float side = settings.templateSideAisleClearance;
        float blockWidth = Mathf.Max(0f, (settings.hallWidth - 2f * side - aisle) * 0.5f);
        float deskGap = 0.32f;
        int columns = Mathf.Min(4, Mathf.Max(0, Mathf.FloorToInt((blockWidth + deskGap) / (deskDefinition.Dimensions.x + deskGap))));
        int rows = Mathf.Min(Mathf.Clamp(settings.classroomTemplateRows, 3, 6),
            Mathf.Max(0, Mathf.FloorToInt((usableLength + 0.8f) / 1.6f)));
        float rowPitch = Mathf.Max(1.6f, deskDefinition.Dimensions.z + chairDefinition.Dimensions.z + 0.5f);
        float rowSpan = Mathf.Max(0f, (rows - 1) * rowPitch);
        float rowStart = Mathf.Clamp((frontLimit + rearLimit + rowSpan) * 0.5f,
            rearLimit + rowSpan * 0.5f, frontLimit - rowSpan * 0.5f);
        float leftCenter = -aisle * 0.5f - blockWidth * 0.5f;
        float rightCenter = aisle * 0.5f + blockWidth * 0.5f;
        AddDeskBlock(items, chair, forward, right, leftCenter, rowStart, columns, rows, deskGap, rowPitch);
        AddDeskBlock(items, chair, forward, right, rightCenter, rowStart, columns, rows, deskGap, rowPitch);

        AddOptional(items, FurnitureType.RegistrationTable,
            forward * (rearLimit + FurnitureCatalog.Get(FurnitureType.RegistrationTable).Dimensions.z * 0.5f));
        return new LayoutTemplateDefinition("classroom_workshop", "CLASSROOM / WORKSHOP", "TRAINING · EDUCATION",
            "Two aligned desk blocks, one chair per desk, a central aisle, and a rear registration point. " +
            CountType(items, FurnitureType.ClassroomDesk) + " desk-chair pairs fit the current settings.", items);
    }

    private static void AddDeskBlock(List<TemplateFurniturePlacement> items, FurnitureType chair,
        Vector3 forward, Vector3 right, float blockCenter, float rowStart, int columns, int rows, float gap, float rowPitch)
    {
        FurnitureDefinition desk = FurnitureCatalog.Get(FurnitureType.ClassroomDesk);
        FurnitureDefinition chairDefinition = FurnitureCatalog.Get(chair);
        float spacing = desk.Dimensions.x + gap;
        float chairOffset = desk.Dimensions.z * 0.5f + chairDefinition.Dimensions.z * 0.5f + 0.20f;
        float forwardYaw = YawForDirection(forward);
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            float x = blockCenter + (column - (columns - 1) * 0.5f) * spacing;
            float z = rowStart - row * rowPitch;
            items.Add(new TemplateFurniturePlacement(FurnitureType.ClassroomDesk, right * x + forward * z, forwardYaw));
            items.Add(new TemplateFurniturePlacement(chair, right * x + forward * (z - chairOffset), forwardYaw));
        }
    }

    private static LayoutTemplateDefinition BuildConference(AuditoriumDemoSettings settings, FurnitureType chair,
        Vector3 forward, Vector3 right, float roomHalfWidth, float rearLimit, float frontLimit, float usableLength)
    {
        List<TemplateFurniturePlacement> items = new List<TemplateFurniturePlacement>();
        FurnitureDefinition tableDefinition = FurnitureCatalog.Get(FurnitureType.RoundTable);
        FurnitureDefinition chairDefinition = FurnitureCatalog.Get(chair);
        int tableCount = Mathf.Clamp(settings.roundtableTemplateCount, 4, 6);
        int rows = Mathf.CeilToInt(tableCount * 0.5f);
        float spacingZ = Mathf.Min(6.0f, Mathf.Max(4.8f, usableLength / (rows + 1)));
        float firstZ = Mathf.Clamp((frontLimit + rearLimit + (rows - 1) * spacingZ) * 0.5f,
            rearLimit + spacingZ * (rows - 1) * 0.5f, frontLimit - spacingZ * (rows - 1) * 0.5f);
        float tableX = Mathf.Min(3.0f, roomHalfWidth - settings.templateSideAisleClearance - tableDefinition.Dimensions.x);
        float chairDistance = tableDefinition.Dimensions.x * 0.5f + chairDefinition.Dimensions.z * 0.5f + 0.18f;
        for (int index = 0; index < tableCount; index++)
        {
            int row = index / 2;
            int column = index % 2;
            float x = column == 0 ? -tableX : tableX;
            float z = firstZ - row * spacingZ;
            Vector3 center = right * x + forward * z;
            items.Add(new TemplateFurniturePlacement(FurnitureType.RoundTable, center, YawForDirection(forward)));
            AddRadialChair(items, chair, center, right, chairDistance);
            AddRadialChair(items, chair, center, -right, chairDistance);
            AddRadialChair(items, chair, center, -forward, chairDistance);
            AddRadialChair(items, chair, center, forward, chairDistance);
        }
        AddOptional(items, FurnitureType.FlowerPot,
            right * -(roomHalfWidth - settings.templateSideAisleClearance) + forward * (rearLimit + 0.8f));
        AddOptional(items, FurnitureType.FlowerPot,
            right * (roomHalfWidth - settings.templateSideAisleClearance) + forward * (rearLimit + 0.8f));

        return new LayoutTemplateDefinition("conference_roundtable", "CONFERENCE / ROUNDTABLE", "DISCUSSION · NETWORKING",
            "Balanced table clusters with four seats each, open paths between groups, and space at the stage end. " +
            tableCount + " tables with " + (tableCount * 4) + " surrounding chairs.", items);
    }

    private static void AddRadialChair(List<TemplateFurniturePlacement> items, FurnitureType chair,
        Vector3 center, Vector3 directionToCenter, float distance)
    {
        Vector3 outward = -directionToCenter;
        Vector3 position = center + outward * distance;
        items.Add(new TemplateFurniturePlacement(chair, position, YawForDirection(-outward)));
    }

    private static FurnitureType ResolveType(string stableId, FurnitureType fallback)
    {
        FurnitureDefinition definition;
        return FurnitureCatalog.TryGetById(stableId, out definition) ? definition.Type : fallback;
    }

    private static void AddOptional(List<TemplateFurniturePlacement> items, FurnitureType type, Vector3 position)
    {
        try { FurnitureCatalog.Get(type); }
        catch (ArgumentOutOfRangeException) { return; }
        items.Add(new TemplateFurniturePlacement(type, position, 0f));
    }

    private static float YawForDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude < 0.001f ? 0f : Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles.y;
    }

    private static string CountLabel(List<TemplateFurniturePlacement> placements, string name)
    {
        int count = 0;
        for (int i = 0; i < placements.Count; i++)
            if (FurnitureCatalog.Get(placements[i].furnitureType).DisplayName == name) count++;
        return count + " " + name.ToLowerInvariant() + "s";
    }

    private static int CountType(List<TemplateFurniturePlacement> placements, FurnitureType type)
    {
        int count = 0;
        for (int i = 0; i < placements.Count; i++)
            if (placements[i].furnitureType == type) count++;
        return count;
    }
}
