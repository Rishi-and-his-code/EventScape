using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Executes AI tool calls by translating them into PlacementSystem operations.
/// </summary>
public sealed class AICommandExecutor : MonoBehaviour
{
    private PlacementSystem placementSystem;

    public void Configure(PlacementSystem placement)
    {
        placementSystem = placement;
    }

    /// <summary>
    /// Executes a named tool call with the given arguments.
    /// Returns (success, message, records) where records is the list of placements attempted.
    /// </summary>
    public (bool success, string message, List<FurniturePlacementRecord> records) Execute(
        string toolName, JObject arguments)
    {
        if (string.IsNullOrEmpty(toolName))
            return (false, "No tool name provided.", null);

        if (string.Equals(toolName, "place_grid", StringComparison.OrdinalIgnoreCase))
            return ExecutePlaceGrid(arguments);

        return (false, "Unknown tool: " + toolName, null);
    }

    private (bool success, string message, List<FurniturePlacementRecord> records) ExecutePlaceGrid(
        JObject args)
    {
        if (args == null)
            return (false, "place_grid requires arguments.", null);

        // Extract parameters
        string furnitureId = args.Value<string>("furnitureId");
        if (string.IsNullOrEmpty(furnitureId))
            return (false, "Missing required parameter: furnitureId.", null);

        int rows = GetInt(args, "rows", 0);
        int cols = GetInt(args, "cols", 0);
        float rowSpacing = GetFloat(args, "rowSpacing_m", 1f);
        float colSpacing = GetFloat(args, "colSpacing_m", 1f);
        float originX = GetFloat(args, "originX_m", 0f);
        float originZ = GetFloat(args, "originZ_m", 0f);
        float rotY = GetFloat(args, "rotY_deg", 0f);

        // Validate furnitureId
        FurnitureDefinition definition;
        if (!FurnitureCatalog.TryGetById(furnitureId, out definition))
            return (false, "Unknown furnitureId '" + furnitureId +
                "'. Use one of the catalog IDs (e.g. auditorium_chair, classroom_desk).", null);

        // Validate grid size
        if (rows <= 0 || cols <= 0)
            return (false, "rows and cols must be positive integers. Got rows="
                + rows.ToString(CultureInfo.InvariantCulture) + " cols="
                + cols.ToString(CultureInfo.InvariantCulture) + ".", null);

        if (rows * cols > 500)
            return (false, "Grid too large: " + (rows * cols).ToString(CultureInfo.InvariantCulture)
                + " items exceeds the 500-item limit. Reduce rows or cols.", null);

        // Validate spacing
        if (rowSpacing < 0.1f || colSpacing < 0.1f)
            return (false, "Spacing values must be at least 0.1m. Got rowSpacing="
                + rowSpacing.ToString("F2", CultureInfo.InvariantCulture) + " colSpacing="
                + colSpacing.ToString("F2", CultureInfo.InvariantCulture) + ".", null);

        // Build placement records
        AuditoriumDemoSettings settings = placementSystem != null ? placementSystem.Settings : null;
        bool isStageItem = FurnitureCatalog.IsStageEquipment(definition.Type);

        List<FurniturePlacementRecord> records = new List<FurniturePlacementRecord>(rows * cols);
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float posX = originX + c * colSpacing;
                float posZ = originZ - r * rowSpacing;
                float posY = 0f;

                if (isStageItem && settings != null)
                {
                    float stageHalfW = settings.stageWidth * 0.5f;
                    float stageHalfD = settings.stageDepth * 0.5f;
                    if (Mathf.Abs(posX) <= stageHalfW && Mathf.Abs(posZ - settings.StageCenterZ) <= stageHalfD)
                    {
                        posY = settings.stageHeight;
                    }
                }

                FurniturePlacementRecord record = new FurniturePlacementRecord();
                record.instanceId = Guid.NewGuid().ToString("N");
                record.furnitureId = furnitureId;
                record.position = new Vector3(posX, posY, posZ);
                record.rotationEuler = new Vector3(0f, rotY, 0f);
                record.scale = Vector3.one;
                records.Add(record);
            }
        }

        // Apply via PlacementSystem
        if (placementSystem == null)
            return (false, "PlacementSystem is not available.", records);

        string failureReason;
        bool ok = placementSystem.TryApplyTemplate(records, false, out failureReason);

        if (!ok)
        {
            return (false,
                "Placement failed: " + failureReason +
                " Grid was " + rows.ToString(CultureInfo.InvariantCulture) + "×" +
                cols.ToString(CultureInfo.InvariantCulture) + " " + definition.DisplayName +
                " at origin (" + originX.ToString("F2", CultureInfo.InvariantCulture) + ", " +
                originZ.ToString("F2", CultureInfo.InvariantCulture) + ").",
                records);
        }

        string successMsg = "Successfully placed " +
            (rows * cols).ToString(CultureInfo.InvariantCulture) + " " +
            definition.DisplayName + "(s) in a " +
            rows.ToString(CultureInfo.InvariantCulture) + "×" +
            cols.ToString(CultureInfo.InvariantCulture) + " grid at origin (" +
            originX.ToString("F2", CultureInfo.InvariantCulture) + ", " +
            originZ.ToString("F2", CultureInfo.InvariantCulture) + ") with spacing (" +
            colSpacing.ToString("F2", CultureInfo.InvariantCulture) + "m × " +
            rowSpacing.ToString("F2", CultureInfo.InvariantCulture) + "m).";

        return (true, successMsg, records);
    }

    private static int GetInt(JObject obj, string key, int defaultValue)
    {
        JToken token = obj[key];
        if (token == null) return defaultValue;
        try
        {
            return token.Value<int>();
        }
        catch
        {
            int parsed;
            if (int.TryParse(token.ToString(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out parsed))
                return parsed;
            return defaultValue;
        }
    }

    private static float GetFloat(JObject obj, string key, float defaultValue)
    {
        JToken token = obj[key];
        if (token == null) return defaultValue;
        try
        {
            return token.Value<float>();
        }
        catch
        {
            float parsed;
            if (float.TryParse(token.ToString(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out parsed))
                return parsed;
            return defaultValue;
        }
    }
}
