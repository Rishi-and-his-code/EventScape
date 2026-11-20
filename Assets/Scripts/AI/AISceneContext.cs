using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Builds a JSON snapshot of the current auditorium state for the AI system prompt.
/// </summary>
public sealed class AISceneContext : MonoBehaviour
{
    private AuditoriumDemoSettings settings;
    private PlacementSystem placementSystem;

    public void Configure(AuditoriumDemoSettings demoSettings, PlacementSystem placement)
    {
        settings = demoSettings;
        placementSystem = placement;
    }

    public string BuildContextJson()
    {
        JObject root = new JObject();

        // Hall dimensions
        JObject hall = new JObject();
        if (settings != null)
        {
            hall["width_m"] = settings.hallWidth;
            hall["depth_m"] = settings.hallDepth;
            hall["height_m"] = settings.wallHeight;
            hall["centerX"] = 0f;
            hall["centerZ"] = 0f;
        }
        root["hall"] = hall;

        // Stage dimensions
        JObject stage = new JObject();
        if (settings != null)
        {
            stage["width_m"] = settings.stageWidth;
            stage["depth_m"] = settings.stageDepth;
            stage["height_m"] = settings.stageHeight;
            stage["centerX"] = 0f;
            stage["centerZ"] = settings.StageCenterZ;
        }
        root["stage"] = stage;

        // Furniture catalog
        JArray catalogArray = new JArray();
        FurnitureDefinition[] allDefs = FurnitureCatalog.GetAll();
        for (int i = 0; i < allDefs.Length; i++)
        {
            FurnitureDefinition def = allDefs[i];
            JObject item = new JObject();
            item["id"] = def.StableId;
            item["name"] = def.DisplayName;
            item["category"] = def.Category;
            item["width_m"] = def.Dimensions.x;
            item["depth_m"] = def.Dimensions.z;
            item["height_m"] = def.Dimensions.y;
            catalogArray.Add(item);
        }
        root["furniture_catalog"] = catalogArray;

        // Currently placed items
        JArray placedArray = new JArray();
        if (placementSystem != null)
        {
            FurnitureInstance[] placed = placementSystem.GetPlacedFurniture();
            for (int i = 0; i < placed.Length; i++)
            {
                FurnitureInstance inst = placed[i];
                if (inst == null) continue;
                FurnitureDefinition def = FurnitureCatalog.Get(inst.Type);
                JObject placedItem = new JObject();
                placedItem["id"] = def.StableId;
                placedItem["instanceId"] = inst.InstanceId;

                JObject pos = new JObject();
                pos["x"] = System.Math.Round(inst.transform.position.x, 3);
                pos["y"] = System.Math.Round(inst.transform.position.y, 3);
                pos["z"] = System.Math.Round(inst.transform.position.z, 3);
                placedItem["position"] = pos;

                JObject rot = new JObject();
                rot["y"] = System.Math.Round(inst.transform.eulerAngles.y, 1);
                placedItem["rotation"] = rot;

                placedArray.Add(placedItem);
            }
        }
        root["placed_items"] = placedArray;

        return root.ToString(Formatting.Indented);
    }
}
