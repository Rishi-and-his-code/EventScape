using UnityEngine;

/// <summary>
/// Provides the tool JSON schema and system prompt for the AI layout assistant.
/// </summary>
public static class AIToolDefinitions
{
    public static string GetPlaceGridToolJson()
    {
        return @"{
  ""type"": ""function"",
  ""function"": {
    ""name"": ""place_grid"",
    ""description"": ""Places a rectangular grid of identical furniture items in the auditorium. Returns the number of items placed or an error message."",
    ""parameters"": {
      ""type"": ""object"",
      ""properties"": {
        ""furnitureId"": {
          ""type"": ""string"",
          ""description"": ""The stable ID of the furniture item to place (e.g. 'auditorium_chair', 'classroom_desk').""
        },
        ""rows"": {
          ""type"": ""integer"",
          ""description"": ""Number of rows in the grid (front-to-back, along Z axis).""
        },
        ""cols"": {
          ""type"": ""integer"",
          ""description"": ""Number of columns in the grid (left-to-right, along X axis).""
        },
        ""rowSpacing_m"": {
          ""type"": ""number"",
          ""description"": ""Distance between row centres in metres (Z direction).""
        },
        ""colSpacing_m"": {
          ""type"": ""number"",
          ""description"": ""Distance between column centres in metres (X direction).""
        },
        ""originX_m"": {
          ""type"": ""number"",
          ""description"": ""X coordinate of the first item (row 0, col 0) in metres. Hall centre is 0.""
        },
        ""originZ_m"": {
          ""type"": ""number"",
          ""description"": ""Z coordinate of the first item (row 0, col 0) in metres. Positive Z is toward the stage.""
        },
        ""rotY_deg"": {
          ""type"": ""number"",
          ""description"": ""Y-axis rotation in degrees applied to every item. 0 = facing +Z (toward stage).""
        }
      },
      ""required"": [""furnitureId"", ""rows"", ""cols"", ""rowSpacing_m"", ""colSpacing_m"", ""originX_m"", ""originZ_m"", ""rotY_deg""]
    }
  }
}";
    }

    public static string GetSystemPrompt(string sceneContext)
    {
        return @"You are an AI layout assistant for a virtual auditorium event planner.
You help the user arrange furniture in a hall by calling the place_grid tool.

## Hall Dimensions
- Hall: 16m wide (X) x 25m deep (Z) x 3.65m tall
- Hall centre is at world position (0, 0, 0)
- X axis: left (−8) to right (+8)
- Z axis: entrance at −12.5, stage at +12.5
- Stage: 9.5m wide x 3.7m deep x 0.45m tall, centred on X=0

## Coordinate Conventions
- The hall centre is (0, 0). Positive Z is toward the stage, negative Z toward the entrance.
- Grid origin is the position of the first item (row 0, col 0).
- Rows increase toward the entrance (−Z). Each subsequent row is placed at originZ − r × rowSpacing.
- Columns increase toward +X. Each subsequent column is placed at originX + c × colSpacing.
- rotY_deg=0 means items face the stage (+Z direction).

## Furniture Catalog (13 items)
FURNITURE:
- bench: Bench (1.80m W × 0.62m D × 0.98m H)
- auditorium_chair: Auditorium Chair (0.48m W × 0.55m D × 0.82m H)
- chair: Classroom Chair (0.46m W × 0.50m D × 0.82m H)
- classroom_desk: Classroom Desk (1.20m W × 0.55m D × 0.75m H)
- round_table: Round Table (1.30m W × 1.30m D × 0.78m H)
- square_table: Square Table (1.25m W × 1.25m D × 0.78m H)
- lounge_chair: Lounge Chair (0.80m W × 0.80m D × 0.90m H)
- registration_table: Registration Table (1.20m W × 0.60m D × 0.75m H)

STAGE & AV:
- event_lectern: Event Lectern (0.55m W × 0.50m D × 1.10m H)
- microphone_stand: Microphone Stand (0.45m W × 0.45m D × 1.60m H)
- pa_floor_speaker: PA Floor Speaker (0.35m W × 0.35m D × 0.70m H)

DECORATIVE:
- flower_pot: Flower Pot (0.62m W × 0.62m D × 1.00m H)
- rollup_banner: Roll-up Banner Stand (0.85m W × 0.35m D × 2.00m H)

## Rules
1. Use the place_grid tool to place items. Multiple calls are fine for complex layouts.
2. Keep items within the hall bounds (|X| < 7.5, -12 < Z < 10).
3. Leave aisles for movement (at least 1m between groups).
4. For theatre-style seating, face chairs toward the stage (rotY_deg=0).
5. Avoid overlapping items — use adequate spacing based on item dimensions.
6. If the user's request is ambiguous, make reasonable assumptions and explain your choices.
7. Stage equipment (lectern, microphone, speakers) should go on or near the stage area (Z > 8).

## Current Scene State
" + sceneContext;
    }
}
