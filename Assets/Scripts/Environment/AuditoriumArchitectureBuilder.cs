using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the fixed Navrachana-inspired auditorium architecture.
/// Decorative meshes have no colliders; structural walls, floor, stage, and steps remain collidable.
/// Architecture realism rebuild marker.
/// </summary>
public static class AuditoriumArchitectureBuilder
{
    private const float FloorThickness = 0.15f;
    private static readonly string[] GeneratedRoots =
        { "Environment", "Stage", "Lighting", "WallFixtures", "ReferenceSeating", "RearFeatureWall", "LeftSideWall", "RightSideWall" };

    public static ReferenceSeatingController SeatingController { get; private set; }

    public static GameObject Build(AuditoriumDemoSettings settings, AuditoriumMaterialLibrary materials)
    {
        if (settings == null) throw new ArgumentNullException("settings");
        if (materials == null) throw new ArgumentNullException("materials");
        if (Application.isPlaying) throw new InvalidOperationException("Architecture generation is Edit Mode only.");

        GameObject priorSeating = FindSceneRoot("ReferenceSeating");
        bool seatingWasConfigured = priorSeating != null;
        bool showSeating = seatingWasConfigured ? priorSeating.activeSelf : settings.showReferenceSeating;

        for (int i = 0; i < GeneratedRoots.Length; i++)
        {
            GameObject oldRoot = FindSceneRoot(GeneratedRoots[i]);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
        }

        GameObject environment = Root("Environment");
        BuildFloor(settings, materials, environment.transform);
        GameObject ceiling = BuildCeiling(settings, materials, environment.transform);
        BuildPerimeter(settings, materials, environment.transform);
        BuildRearFeature(settings, materials);
        BuildSideWalls(settings, materials);
        BuildStage(settings, materials);
        BuildWallFixtures(settings, materials);
        BuildLighting(settings, materials);
        GameObject seating = BuildReferenceSeating(settings, materials);
        SeatingController = seating.AddComponent<ReferenceSeatingController>();
        SeatingController.Configure(seating, showSeating);
        return ceiling;
    }

    private static void BuildFloor(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m, Transform parent)
    {
        // Structural collider uses the SAME uniform medium-grey material as the visible tiles
        // so grout gaps never expose a near-black underlay.
        Material tileMat = m.stoneTile != null ? m.stoneTile : m.floor;
        GameObject floor = Block("Auditorium_Floor", parent, new Vector3(0f, -FloorThickness * 0.5f, 0f),
            new Vector3(s.hallWidth, FloorThickness, s.hallDepth), tileMat);
        floor.AddComponent<PlacementSurface>();

        MeshBatch tiles = new MeshBatch(Root("Floor_Tile_Surface", parent).transform);
        float tile = Mathf.Clamp(s.floorTileSize, 0.55f, 0.85f);
        float gap = Mathf.Clamp(s.floorGroutWidth, 0.002f, 0.006f);
        int columns = Mathf.CeilToInt(s.hallWidth / tile);
        int rows = Mathf.CeilToInt(s.hallDepth / tile);
        for (int x = 0; x < columns; x++)
        for (int z = 0; z < rows; z++)
        {
            float width = Mathf.Min(tile - gap, s.hallWidth - x * tile);
            float depth = Mathf.Min(tile - gap, s.hallDepth - z * tile);
            if (width <= 0.05f || depth <= 0.05f) continue;
            float cx = -s.hallWidth * 0.5f + x * tile + width * 0.5f;
            float cz = -s.hallDepth * 0.5f + z * tile + depth * 0.5f;
            // One shared material only — no light/dark tile lottery.
            tiles.AddBox(tileMat, new Vector3(cx, 0.002f, cz), new Vector3(width, 0.008f, depth),
                new Vector2(1f, 1f));
        }
        tiles.Apply("Uniform Stone Floor Tiles", false);
    }

    private static void BuildPerimeter(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m, Transform parent)
    {
        float x = s.hallWidth * 0.5f;
        float z = s.hallDepth * 0.5f;
        float t = s.wallThickness;
        float h = s.wallHeight;
        Material plaster = m.wallPlaster != null ? m.wallPlaster : m.wall;
        Block("Auditorium_Wall_Left", parent, new Vector3(-x - t * 0.5f, h * 0.5f, 0f),
            new Vector3(t, h, s.hallDepth + 2f * t), plaster);
        Block("Auditorium_Wall_Right", parent, new Vector3(x + t * 0.5f, h * 0.5f, 0f),
            new Vector3(t, h, s.hallDepth + 2f * t), plaster);

        // Photo 01: facing the rear wall (−Z), double doors sit on the far right of the frame
        // (world −X), to the right of the Navrachana logo.
        float doorway = Mathf.Min(s.doorwayWidth, s.hallWidth * 0.28f);
        float openingHeight = Mathf.Min(s.doorwayHeight, h - 0.15f);
        float doorCenterX = ResolveRearDoorCenterX(s, doorway);
        float leftWidth = (doorCenterX - doorway * 0.5f) - (-x - t);
        float rightWidth = (x + t) - (doorCenterX + doorway * 0.5f);
        float rearZ = -z - t * 0.5f;
        Transform backWall = Root("Auditorium_Wall_Back", parent).transform;
        Block("Auditorium_Wall_Back_Left", backWall,
            new Vector3((-x - t + doorCenterX - doorway * 0.5f) * 0.5f, h * 0.5f, rearZ),
            new Vector3(leftWidth, h, t), plaster);
        Block("Auditorium_Wall_Back_Right", backWall,
            new Vector3((doorCenterX + doorway * 0.5f + x + t) * 0.5f, h * 0.5f, rearZ),
            new Vector3(rightWidth, h, t), plaster);
        Block("Rear Door Header", backWall,
            new Vector3(doorCenterX, openingHeight + (h - openingHeight) * 0.5f, rearZ),
            new Vector3(doorway, h - openingHeight, t), plaster);

        float frontZ = z + t * 0.5f;
        Block("Auditorium_Wall_Front", parent, new Vector3(0f, h * 0.5f, frontZ),
            new Vector3(s.hallWidth + 2f * t, h, t), plaster);

        BuildRearDoor(s, m, parent, doorCenterX, doorway, openingHeight, -z);
    }

    /// <summary>Door centre on the −X side of the rear wall (right of the logo when facing −Z).</summary>
    private static float ResolveRearDoorCenterX(AuditoriumDemoSettings s, float doorway)
    {
        float x = s.hallWidth * 0.5f;
        float doorCenterX = -x + s.doorwayInsetFromRight;
        return Mathf.Clamp(doorCenterX, -x + doorway * 0.5f + 0.2f, -0.6f);
    }

    private static void BuildRearDoor(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m, Transform parent,
        float doorCenterX, float doorway, float openingHeight, float innerRearZ)
    {
        Transform doorRoot = Root("Rear_Entrance_Door", parent).transform;
        MeshBatch door = new MeshBatch(doorRoot);
        // Close-up reference: dark walnut double leaves, square vision lites, long silver pulls,
        // set in a light honey timber surround. Detail is mirrored on both faces so the same
        // door reads from the hall interior (+Z) and from outside the opening (−Z).
        Material leaf = m.doorLeaf != null ? m.doorLeaf : (m.woodWalnut != null ? m.woodWalnut : m.backdrop);
        Material leafEdge = m.woodWalnutLight != null ? m.woodWalnutLight : leaf;
        Material frame = m.woodHoney != null ? m.woodHoney : m.trim;
        Material glass = m.doorGlass != null ? m.doorGlass : m.projectionScreen;
        Material handle = m.fanMetal != null ? m.fanMetal : (m.structuralMetal != null ? m.structuralMetal : leaf);

        // Sit the door on the hall-interior face of the rear wall so the same dark double-door
        // (windows + silver pulls) is what you see from inside — not a cladding-filled recess.
        float leafZ = innerRearZ + 0.07f;
        float jamb = 0.09f;
        float surroundDepth = 0.14f;
        door.AddBox(frame, new Vector3(doorCenterX - doorway * 0.5f - jamb * 0.35f, openingHeight * 0.5f, leafZ),
            new Vector3(jamb, openingHeight + 0.08f, surroundDepth));
        door.AddBox(frame, new Vector3(doorCenterX + doorway * 0.5f + jamb * 0.35f, openingHeight * 0.5f, leafZ),
            new Vector3(jamb, openingHeight + 0.08f, surroundDepth));
        door.AddBox(frame, new Vector3(doorCenterX, openingHeight + jamb * 0.4f, leafZ),
            new Vector3(doorway + jamb * 1.7f, jamb, surroundDepth));
        door.AddBox(frame, new Vector3(doorCenterX, 0.025f, leafZ),
            new Vector3(doorway + 0.06f, 0.05f, surroundDepth + 0.02f));
        for (float sx = doorCenterX - doorway * 0.45f; sx <= doorCenterX + doorway * 0.45f; sx += 0.09f)
        {
            door.AddBox(frame, new Vector3(sx, openingHeight + jamb * 0.4f, leafZ + surroundDepth * 0.35f),
                new Vector3(0.04f, jamb * 0.9f, 0.025f));
        }

        float gap = 0.014f;
        float leafWidth = (doorway - gap) * 0.5f;
        float leafThickness = 0.06f;
        float leafHeight = openingHeight - 0.06f;
        float leafY = leafHeight * 0.5f + 0.025f;
        float leftX = doorCenterX - gap * 0.5f - leafWidth * 0.5f;
        float rightX = doorCenterX + gap * 0.5f + leafWidth * 0.5f;

        door.AddBox(leaf, new Vector3(leftX, leafY, leafZ),
            new Vector3(leafWidth, leafHeight, leafThickness), new Vector2(0.85f, 1.8f));
        door.AddBox(leaf, new Vector3(rightX, leafY, leafZ),
            new Vector3(leafWidth, leafHeight, leafThickness), new Vector2(0.85f, 1.8f));
        door.AddBox(leafEdge, new Vector3(doorCenterX - gap * 0.5f, leafY, leafZ + leafThickness * 0.2f),
            new Vector3(0.012f, leafHeight - 0.05f, leafThickness * 0.7f));
        door.AddBox(leafEdge, new Vector3(doorCenterX + gap * 0.5f, leafY, leafZ + leafThickness * 0.2f),
            new Vector3(0.012f, leafHeight - 0.05f, leafThickness * 0.7f));

        float window = 0.2f;
        float windowY = openingHeight * 0.62f;
        float handleHeight = 1.0f;
        float handleY = 1.08f;
        // Interior (+Z / into hall) and exterior (−Z) faces get the same close-up look.
        for (int face = -1; face <= 1; face += 2)
        {
            float faceZ = leafZ + face * (leafThickness * 0.5f + 0.01f);
            door.AddBox(leafEdge, new Vector3(leftX, windowY, faceZ),
                new Vector3(window + 0.035f, window + 0.035f, 0.014f));
            door.AddBox(leafEdge, new Vector3(rightX, windowY, faceZ),
                new Vector3(window + 0.035f, window + 0.035f, 0.014f));
            door.AddBox(glass, new Vector3(leftX, windowY, faceZ + face * 0.008f),
                new Vector3(window, window, 0.012f));
            door.AddBox(glass, new Vector3(rightX, windowY, faceZ + face * 0.008f),
                new Vector3(window, window, 0.012f));

            float handleZ = faceZ + face * 0.02f;
            door.AddBox(handle, new Vector3(doorCenterX - gap * 0.5f - 0.05f, handleY, handleZ),
                new Vector3(0.032f, handleHeight, 0.032f));
            door.AddBox(handle, new Vector3(doorCenterX + gap * 0.5f + 0.05f, handleY, handleZ),
                new Vector3(0.032f, handleHeight, 0.032f));
            door.AddBox(handle, new Vector3(doorCenterX - gap * 0.5f - 0.05f, handleY + handleHeight * 0.48f, handleZ + face * 0.012f),
                new Vector3(0.045f, 0.045f, 0.045f));
            door.AddBox(handle, new Vector3(doorCenterX - gap * 0.5f - 0.05f, handleY - handleHeight * 0.48f, handleZ + face * 0.012f),
                new Vector3(0.045f, 0.045f, 0.045f));
            door.AddBox(handle, new Vector3(doorCenterX + gap * 0.5f + 0.05f, handleY + handleHeight * 0.48f, handleZ + face * 0.012f),
                new Vector3(0.045f, 0.045f, 0.045f));
            door.AddBox(handle, new Vector3(doorCenterX + gap * 0.5f + 0.05f, handleY - handleHeight * 0.48f, handleZ + face * 0.012f),
                new Vector3(0.045f, 0.045f, 0.045f));
        }

        for (int i = 0; i < 3; i++)
        {
            float hy = 0.45f + i * 0.7f;
            door.AddBox(frame, new Vector3(doorCenterX - doorway * 0.5f - 0.015f, hy, leafZ),
                new Vector3(0.045f, 0.09f, surroundDepth * 0.75f));
            door.AddBox(frame, new Vector3(doorCenterX + doorway * 0.5f + 0.015f, hy, leafZ),
                new Vector3(0.045f, 0.09f, surroundDepth * 0.75f));
        }
        door.Apply("Rear Entrance Door Assembly", false);
    }

    private static void BuildRearFeature(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m)
    {
        Transform root = Root("RearFeatureWall").transform;
        float z = -s.hallDepth * 0.5f + 0.09f;
        float y0 = Mathf.Max(s.ResolvedCladdingHeight, 2.95f);
        float width = s.hallWidth;
        MeshBatch rear = new MeshBatch(root);
        Material honey = m.woodHoney != null ? m.woodHoney : m.trim;
        Material honeyAlt = m.woodHoneyLight != null ? m.woodHoneyLight : honey;
        Material walnut = m.woodWalnut != null ? m.woodWalnut : m.backdrop;
        Material walnutLight = m.woodWalnutLight != null ? m.woodWalnutLight : walnut;
        Material plaster = m.wallPlaster != null ? m.wallPlaster : m.wall;
        Material seam = walnutLight;

        // Upper plaster field above the wood line.
        rear.AddBox(plaster, new Vector3(0f, (y0 + s.wallHeight) * 0.5f, z),
            new Vector3(width - 0.08f, Mathf.Max(0.2f, s.wallHeight - y0), 0.045f));
        // Continuous white / light skirting under wood and feature.
        rear.AddBox(plaster, new Vector3(0f, 0.05f, z + 0.02f),
            new Vector3(width - 0.05f, 0.1f, 0.05f));

        // Photo 01: large centred dark-walnut grid with thin light seams.
        const float featureWidth = 7.4f;
        const float featureBottom = 0.14f;
        const float featureTop = 3.05f;
        const int cols = 3;
        const int rows = 5;
        float panelW = featureWidth / cols;
        float panelH = (featureTop - featureBottom) / rows;
        float seamW = 0.014f;
        float depth = 0.07f;
        // Solid walnut backing so seams never flash plaster.
        rear.AddBox(walnut, new Vector3(0f, (featureBottom + featureTop) * 0.5f, z + 0.03f),
            new Vector3(featureWidth + 0.04f, featureTop - featureBottom + 0.04f, 0.04f),
            new Vector2(2.2f, 2.4f));
        // Raised perimeter frame.
        rear.AddBox(walnut, new Vector3(0f, featureTop + 0.03f, z + 0.07f),
            new Vector3(featureWidth + 0.1f, 0.06f, 0.05f));
        rear.AddBox(walnut, new Vector3(0f, featureBottom - 0.02f, z + 0.07f),
            new Vector3(featureWidth + 0.1f, 0.05f, 0.05f));
        rear.AddBox(walnut, new Vector3(-featureWidth * 0.5f - 0.03f, (featureBottom + featureTop) * 0.5f, z + 0.07f),
            new Vector3(0.06f, featureTop - featureBottom + 0.08f, 0.05f));
        rear.AddBox(walnut, new Vector3(featureWidth * 0.5f + 0.03f, (featureBottom + featureTop) * 0.5f, z + 0.07f),
            new Vector3(0.06f, featureTop - featureBottom + 0.08f, 0.05f));

        for (int c = 0; c < cols; c++)
        for (int r = 0; r < rows; r++)
        {
            float px = -featureWidth * 0.5f + panelW * (c + 0.5f);
            float py = featureBottom + panelH * (r + 0.5f);
            rear.AddBox(walnut, new Vector3(px, py, z + 0.06f),
                new Vector3(panelW - seamW, panelH - seamW, depth),
                new Vector2((panelW - seamW) * 0.65f, (panelH - seamW) * 0.65f));
            rear.AddBox(seam, new Vector3(px, py + (panelH - seamW) * 0.5f, z + 0.098f),
                new Vector3(panelW - seamW, 0.008f, 0.01f));
            rear.AddBox(seam, new Vector3(px, py - (panelH - seamW) * 0.5f, z + 0.098f),
                new Vector3(panelW - seamW, 0.008f, 0.01f));
        }
        for (int c = 0; c <= cols; c++)
        {
            float sx = -featureWidth * 0.5f + c * panelW;
            rear.AddBox(seam, new Vector3(sx, (featureBottom + featureTop) * 0.5f, z + 0.1f),
                new Vector3(seamW, featureTop - featureBottom, 0.016f));
        }
        for (int r = 0; r <= rows; r++)
        {
            float sy = featureBottom + r * panelH;
            rear.AddBox(seam, new Vector3(0f, sy, z + 0.1f),
                new Vector3(featureWidth, seamW, 0.016f));
        }

        float doorway = Mathf.Min(s.doorwayWidth, s.hallWidth * 0.28f);
        float doorCenterX = ResolveRearDoorCenterX(s, doorway);
        float doorClear = doorway * 0.5f + 0.18f;

        // Dense honey vertical cladding flanking the walnut feature; keep a clear opening for the door.
        for (float x = -width * 0.5f + 0.12f; x < width * 0.5f - 0.08f; x += 0.085f)
        {
            if (Mathf.Abs(x) < featureWidth * 0.5f + 0.06f) continue;
            if (Mathf.Abs(x - doorCenterX) < doorClear) continue;
            Material tone = Mathf.RoundToInt((x + width) * 19f) % 3 == 0 ? honeyAlt : honey;
            rear.AddBox(tone, new Vector3(x, y0 * 0.5f + 0.02f, z + 0.055f),
                new Vector3(0.072f, y0 - 0.14f, 0.032f), new Vector2(0.18f, y0 * 0.9f));
        }

        float leftFlankOuter = -width * 0.5f + 0.04f;
        float leftFlankInner = doorCenterX - doorClear;
        float leftFlankWidth = leftFlankInner - leftFlankOuter;
        if (leftFlankWidth > 0.15f)
        {
            rear.AddBox(honey, new Vector3((leftFlankOuter + leftFlankInner) * 0.5f, y0 * 0.5f + 0.02f, z + 0.028f),
                new Vector3(leftFlankWidth, y0 - 0.12f, 0.04f));
        }
        float midFlankOuter = doorCenterX + doorClear;
        float midFlankInner = -featureWidth * 0.5f - 0.06f;
        float midFlankWidth = midFlankInner - midFlankOuter;
        if (midFlankWidth > 0.15f)
        {
            rear.AddBox(honey, new Vector3((midFlankOuter + midFlankInner) * 0.5f, y0 * 0.5f + 0.02f, z + 0.028f),
                new Vector3(midFlankWidth, y0 - 0.12f, 0.04f));
        }
        rear.AddBox(honey, new Vector3((featureWidth + width) * 0.25f, y0 * 0.5f + 0.02f, z + 0.028f),
            new Vector3((width - featureWidth) * 0.5f - 0.08f, y0 - 0.12f, 0.04f));

        // Dark logo plate proud of the walnut grid (hall-interior side).
        rear.AddBox(walnut, new Vector3(0f, 1.95f, z + 0.12f),
            new Vector3(2.05f, 2.15f, 0.04f));
        rear.Apply("Rear Feature Panels", false);

        Material logoMat = m.rearLogoMaterial != null && m.rearLogoMaterial.mainTexture != null
            ? m.rearLogoMaterial
            : (m.woodWalnut != null ? m.woodWalnut : walnut);
        // Thin board facing into the hall (+Z) so the white Navrachana mark is readable from inside.
        TexturedBoard("Navrachana Identity Artwork Slot", root,
            new Vector3(0f, 1.95f, z + 0.155f), new Vector2(1.9f, 2.05f), 0.03f, logoMat);

        // Soft logo accents — keep intensity low so the white mark is not washed to orange glare.
        CreateSpot(root, "Rear Logo Spot Left", new Vector3(-0.5f, featureTop + 0.15f, z + 0.7f),
            new Vector3(-0.1f, 1.95f, z + 0.16f), s.WarmLightColor, 1.15f, 26f, 5f);
        CreateSpot(root, "Rear Logo Spot Right", new Vector3(0.5f, featureTop + 0.15f, z + 0.7f),
            new Vector3(0.1f, 1.95f, z + 0.16f), s.WarmLightColor, 1.15f, 26f, 5f);
    }

    /// <summary>Per-bay fixture plan so blinds, fans and speakers never share the same wall volume.</summary>
    private struct WallBayPlan
    {
        public float CenterZ;
        public bool HasBlind;
        public bool HasFan;
        public bool HasSpeaker;
        public bool HasPlate;
    }

    private static WallBayPlan[] BuildBayPlan(bool isRight, AuditoriumDemoSettings s)
    {
        // Independent left/right schedules from photos 02 and 04 (estimates).
        // Right wall: more blinds. Left wall: fewer blinds, more fans.
        float halfDepth = s.hallDepth * 0.5f;
        float bay = Mathf.Max(2.8f, s.sideWallBaySpacing);
        var plans = new List<WallBayPlan>();
        int index = 0;
        for (float z = -halfDepth + 1.6f; z < halfDepth - 1.0f; z += bay)
        {
            WallBayPlan plan = new WallBayPlan { CenterZ = z };
            if (isRight)
            {
                // Photo 04: blinds in even bays; clear odd bays host fans + offset speakers.
                plan.HasBlind = (index % 2 == 0) && index <= 8;
                plan.HasFan = !plan.HasBlind && index <= 7;
                plan.HasSpeaker = !plan.HasBlind && index <= 7;
                plan.HasPlate = plan.HasBlind && (index == 2 || index == 4 || index == 6);
            }
            else
            {
                // Photo 02: fewer blinds (odd bays); more fan-capable clear bays.
                plan.HasBlind = (index % 2 == 1) && index <= 5;
                plan.HasFan = !plan.HasBlind;
                plan.HasSpeaker = !plan.HasBlind && (index % 2 == 0);
                plan.HasPlate = plan.HasBlind && (index == 1 || index == 3);
            }
            plans.Add(plan);
            index++;
        }
        return plans.ToArray();
    }

    private static void BuildSideWalls(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m)
    {
        Transform left = Root("LeftSideWall").transform;
        Transform right = Root("RightSideWall").transform;
        float halfDepth = s.hallDepth * 0.5f;
        float innerX = s.hallWidth * 0.5f - 0.04f;
        float clad = s.ResolvedCladdingHeight;
        Material honey = m.woodHoney != null ? m.woodHoney : m.trim;
        Material honeyLight = m.woodHoneyLight != null ? m.woodHoneyLight : honey;
        Material plaster = m.wallPlaster != null ? m.wallPlaster : m.wall;
        Material pilasterMat = m.column != null ? m.column : plaster;
        float pilasterW = Mathf.Clamp(s.pilasterWidth, 0.2f, 0.42f);
        float blindW = Mathf.Clamp(s.rollerBlindWidth, 1.5f, 2.4f);

        for (int side = 0; side < 2; side++)
        {
            bool isRight = side == 1;
            Transform wall = isRight ? right : left;
            float x = isRight ? innerX : -innerX;
            float faceSign = isRight ? -1f : 1f;
            WallBayPlan[] bays = BuildBayPlan(isRight, s);
            MeshBatch batch = new MeshBatch(wall);

            // Continuous upper plaster and lower cladding backers.
            batch.AddBox(plaster, new Vector3(x + faceSign * 0.012f, (clad + s.wallHeight) * 0.5f, 0f),
                new Vector3(0.04f, s.wallHeight - clad, s.hallDepth - 0.25f));
            batch.AddBox(honey, new Vector3(x + faceSign * 0.018f, clad * 0.5f, 0f),
                new Vector3(0.05f, clad, s.hallDepth - 0.25f));
            // Base and upper cladding trims.
            batch.AddBox(honeyLight, new Vector3(x + faceSign * 0.05f, 0.04f, 0f),
                new Vector3(0.06f, 0.08f, s.hallDepth - 0.3f));
            batch.AddBox(honeyLight, new Vector3(x + faceSign * 0.05f, clad - 0.03f, 0f),
                new Vector3(0.06f, 0.06f, s.hallDepth - 0.3f));

            for (int i = 0; i < bays.Length; i++)
            {
                float z = bays[i].CenterZ;
                float baySpan = i + 1 < bays.Length
                    ? (bays[i + 1].CenterZ - z)
                    : (halfDepth - 0.6f - z);
                baySpan = Mathf.Max(2.2f, baySpan);

                // White/cream pilaster with real thickness at bay boundary.
                batch.AddBox(pilasterMat, new Vector3(x + faceSign * 0.08f, s.wallHeight * 0.5f, z),
                    new Vector3(0.16f, s.wallHeight - 0.04f, pilasterW));
                batch.AddBox(pilasterMat, new Vector3(x + faceSign * 0.10f, clad * 0.5f, z),
                    new Vector3(0.20f, clad, pilasterW + 0.05f));

                // Vertical timber boards within the bay (skip blind bay centres — blinds sit there).
                float boardStart = z + pilasterW * 0.55f;
                float boardEnd = z + baySpan - pilasterW * 0.55f;
                for (float bz = boardStart; bz < boardEnd; bz += 0.13f)
                {
                    if (bays[i].HasBlind && Mathf.Abs(bz - (z + baySpan * 0.45f)) < blindW * 0.42f) continue;
                    batch.AddBox(honey, new Vector3(x + faceSign * 0.055f, clad * 0.5f, bz),
                        new Vector3(0.035f, clad - 0.14f, 0.10f), new Vector2(0.18f, clad * 0.75f));
                }

                if (bays[i].HasBlind)
                    AddRollerBlind(batch, s, m, x, faceSign, clad, z + baySpan * 0.45f);
            }

            batch.Apply(isRight ? "Right Wall Bays and Cladding" : "Left Wall Bays and Cladding", false);
        }
    }

    private static void AddRollerBlind(MeshBatch batch, AuditoriumDemoSettings s, AuditoriumMaterialLibrary m,
        float x, float faceSign, float claddingHeight, float z)
    {
        Material shade = m.rollerBlind != null ? m.rollerBlind : m.acousticPanel;
        Material frame = m.structuralMetal != null ? m.structuralMetal : m.column;
        float width = Mathf.Clamp(s.rollerBlindWidth, 1.5f, 2.4f);
        float height = Mathf.Clamp(s.rollerBlindHeight, 1.2f, s.wallHeight - claddingHeight - 0.2f);
        float y = claddingHeight + height * 0.5f + 0.06f;
        // Strong offset from wall so blinds read as fabric from orbit and walkthrough.
        float face = x + faceSign * 0.11f;

        batch.AddCylinder(frame, new Vector3(face + faceSign * 0.02f, y + height * 0.5f + 0.04f, z),
            0.045f, width + 0.12f, 12, Vector3.forward);
        batch.AddBox(shade, new Vector3(face, y, z), new Vector3(0.06f, height, width),
            new Vector2(width, height));
        batch.AddBox(frame, new Vector3(face + faceSign * 0.01f, y - height * 0.5f, z),
            new Vector3(0.07f, 0.05f, width + 0.06f));
        batch.AddBox(frame, new Vector3(face + faceSign * 0.04f, y + height * 0.5f + 0.04f, z - width * 0.5f),
            new Vector3(0.07f, 0.10f, 0.07f));
        batch.AddBox(frame, new Vector3(face + faceSign * 0.04f, y + height * 0.5f + 0.04f, z + width * 0.5f),
            new Vector3(0.07f, 0.10f, 0.07f));
    }

    private static void BuildStage(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m)
    {
        Transform stage = Root("Stage").transform;
        float z = s.StageCenterZ;
        GameObject stagePlatform = Block("Auditorium_Stage", stage, new Vector3(0f, s.stageHeight * 0.5f, z),
            new Vector3(s.stageWidth, s.stageHeight, s.stageDepth), m.stageWood);
        stagePlatform.AddComponent<PlacementSurface>();

        // Access steps on audience-right (+X), matching photograph 03.
        Transform steps = Root("StageSteps", stage).transform;
        float stageFrontZ = z - s.stageDepth * 0.5f;
        float stepX = s.stageWidth * 0.5f - 1.35f;
        Block("Stage Step Lower", steps, new Vector3(stepX, 0.075f, stageFrontZ - 0.42f),
            new Vector3(1.7f, 0.15f, 0.28f), m.stageWood);
        Block("Stage Step Upper", steps, new Vector3(stepX, 0.15f, stageFrontZ - 0.14f),
            new Vector3(1.7f, 0.30f, 0.28f), m.stageWood);

        MeshBatch planks = new MeshBatch(Root("Stage_Plank_Finish", stage).transform);
        Material timber = m.stageTimber != null ? m.stageTimber : m.stageTop;
        Material alternate = m.woodHoneyDark != null ? m.woodHoneyDark : timber;
        float plankDepth = 0.18f;
        for (float pz = z - s.stageDepth * 0.5f + 0.06f; pz < z + s.stageDepth * 0.5f - 0.05f; pz += plankDepth)
        {
            float depth = Mathf.Min(plankDepth - 0.01f, z + s.stageDepth * 0.5f - 0.04f - pz);
            planks.AddBox(Mathf.RoundToInt((pz + 20f) * 5f) % 5 == 0 ? alternate : timber,
                new Vector3(0f, s.stageHeight + 0.006f, pz + depth * 0.5f),
                new Vector3(s.stageWidth - 0.06f, 0.012f, depth),
                new Vector2((s.stageWidth - 0.06f) * 0.35f, depth * 1.2f));
        }
        planks.Apply("Stage Timber Planks", false);

        // Curved / rounded front fascia approximated with centred board + side facets inside footprint.
        MeshBatch fascias = new MeshBatch(Root("Stage_Fascia", stage).transform);
        float frontZ = z - s.stageDepth * 0.5f;
        fascias.AddBox(timber, new Vector3(0f, s.stageHeight * 0.5f, frontZ + 0.01f),
            new Vector3(s.stageWidth - 0.9f, s.stageHeight - 0.02f, 0.04f));
        for (int i = 0; i < 5; i++)
        {
            float t = (i + 1) / 6f;
            float x = (s.stageWidth * 0.5f - 0.15f) * t;
            float inset = 0.02f + 0.11f * t * t;
            fascias.AddBox(timber, new Vector3(x, s.stageHeight * 0.5f, frontZ + inset),
                new Vector3(0.42f, s.stageHeight - 0.04f, 0.035f));
            fascias.AddBox(timber, new Vector3(-x, s.stageHeight * 0.5f, frontZ + inset),
                new Vector3(0.42f, s.stageHeight - 0.04f, 0.035f));
        }
        for (float x = -s.stageWidth * 0.5f + 0.08f; x < s.stageWidth * 0.5f; x += 0.14f)
            fascias.AddBox(alternate, new Vector3(x, s.stageHeight * 0.5f, frontZ - 0.01f),
                new Vector3(0.02f, s.stageHeight - 0.08f, 0.02f));
        fascias.Apply("Stage Front Timber Fascia", false);

        float backZ = s.hallDepth * 0.5f - 0.10f;
        Transform backdrop = Root("StageBackdrop", stage).transform;
        MeshBatch panels = new MeshBatch(backdrop);
        Material walnut = m.woodWalnut != null ? m.woodWalnut : m.backdrop;
        Material walnut2 = m.woodWalnutLight != null ? m.woodWalnutLight : walnut;
        Material screen = m.projectionScreen != null ? m.projectionScreen : m.ceiling;
        float backBottom = 0.52f;
        float backTop = Mathf.Min(3.40f, s.wallHeight - 0.12f);
        panels.AddBox(walnut, new Vector3(0f, (backBottom + backTop) * 0.5f, backZ),
            new Vector3(s.stageWidth, backTop - backBottom, 0.09f));
        float screenWidth = Mathf.Min(5.2f, s.stageWidth * 0.55f);
        float screenHeight = Mathf.Min(2.2f, backTop - backBottom - 0.3f);
        float screenY = 2.20f;
        panels.AddBox(screen, new Vector3(0f, screenY, backZ - 0.055f),
            new Vector3(screenWidth, screenHeight, 0.04f));
        for (int i = -5; i <= 5; i++)
        {
            float seamX = i * s.stageWidth / 11f;
            if (Mathf.Abs(seamX) < screenWidth * 0.5f + 0.04f) continue;
            panels.AddBox(walnut2, new Vector3(seamX, (backBottom + backTop) * 0.5f, backZ - 0.05f),
                new Vector3(0.02f, backTop - backBottom, 0.02f));
        }
        for (int r = 1; r < 5; r++)
        {
            float py = backBottom + (backTop - backBottom) * r / 5f;
            panels.AddBox(walnut2, new Vector3(-(screenWidth + s.stageWidth) * 0.25f, py, backZ - 0.05f),
                new Vector3((s.stageWidth - screenWidth) * 0.5f - 0.1f, 0.015f, 0.018f));
            panels.AddBox(walnut2, new Vector3((screenWidth + s.stageWidth) * 0.25f, py, backZ - 0.05f),
                new Vector3((s.stageWidth - screenWidth) * 0.5f - 0.1f, 0.015f, 0.018f));
        }
        panels.Apply("Dark Walnut Stage Backdrop and Projection Screen", false);

        // Photo 03: two large square red/magenta Navrachana posters on the walnut flanks.
        Transform banners = Root("Banners", stage).transform;
        float flankWidth = (s.stageWidth - screenWidth) * 0.5f;
        float posterSize = Mathf.Clamp(Mathf.Min(s.stageBannerWidth, s.stageBannerHeight), 1.35f, 1.7f);
        posterSize = Mathf.Min(posterSize, flankWidth - 0.22f);
        float posterY = Mathf.Clamp(screenY - 0.05f, backBottom + 0.35f + posterSize * 0.5f,
            backTop - 0.2f - posterSize * 0.5f);
        float posterX = (screenWidth + s.stageWidth) * 0.25f;
        for (int side = -1; side <= 1; side += 2)
        {
            Material artwork = side < 0
                ? (m.bannerLeftMaterial != null ? m.bannerLeftMaterial : m.bannerArtworkMaterial)
                : (m.bannerRightMaterial != null ? m.bannerRightMaterial : m.bannerArtworkMaterial);
            bool hasArt = artwork != null && artwork.mainTexture != null;
            Material face = hasArt ? artwork : (m.bannerRed != null ? m.bannerRed : m.accent);
            Material frameMat = m.bannerRed != null ? m.bannerRed : walnut2;

            MeshBatch backing = new MeshBatch(banners);
            // Thin red frame only — never a full walnut plate that hides the artwork.
            backing.AddBox(frameMat, new Vector3(side * posterX, posterY, backZ - 0.16f),
                new Vector3(posterSize + 0.1f, posterSize + 0.1f, 0.035f));
            backing.Apply(side < 0 ? "Left Poster Frame" : "Right Poster Frame", false);

            // Thick coloured board toward the audience so the red poster always reads in Planning View.
            // 180° Z corrects cube-face UVs that otherwise show the artwork upside-down from the hall.
            GameObject board = TexturedBoard(side < 0 ? "Stage Poster Left" : "Stage Poster Right", banners,
                new Vector3(side * posterX, posterY, backZ - 0.22f),
                new Vector2(posterSize, posterSize), 0.045f, face);
            board.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        }

        Transform podium = Root("Podium", stage).transform;
        Material podiumWood = m.podiumWood != null ? m.podiumWood : walnut;
        MeshBatch lectern = new MeshBatch(podium);
        float px = -3.55f;
        float podiumZ = z - 0.35f;
        BoxCollider podiumCollider = podium.gameObject.AddComponent<BoxCollider>();
        podiumCollider.center = new Vector3(px, 0.95f, podiumZ);
        podiumCollider.size = new Vector3(0.78f, 1.05f, 0.68f);
        lectern.AddBox(podiumWood, new Vector3(px, 0.88f, podiumZ), new Vector3(0.52f, 0.86f, 0.44f));
        lectern.AddBox(podiumWood, new Vector3(px, 1.34f, podiumZ + 0.02f), new Vector3(0.74f, 0.09f, 0.58f));
        lectern.AddBox(walnut2, new Vector3(px, 1.40f, podiumZ - 0.22f), new Vector3(0.72f, 0.03f, 0.06f));
        lectern.AddBox(m.fixtureFinish != null ? m.fixtureFinish : walnut2,
            new Vector3(px, 1.05f, podiumZ - 0.23f), new Vector3(0.45f, 0.01f, 0.01f));
        lectern.Apply("Fixed Stage Lectern", false);
    }

    private static GameObject BuildCeiling(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m, Transform parent)
    {
        GameObject ceilingObject = Root("Auditorium_Ceiling", parent);
        Block("Ceiling Structure", ceilingObject.transform, new Vector3(0f, s.wallHeight + FloorThickness * 0.5f, 0f),
            new Vector3(s.hallWidth, FloorThickness, s.hallDepth), m.ceiling);

        Transform panelsRoot = Root("Acoustic_Panels_and_Grid", ceilingObject.transform).transform;
        MeshBatch panels = new MeshBatch(panelsRoot);
        Material acoustic = m.acousticPanelLight != null ? m.acousticPanelLight : m.acousticPanel;
        Material acousticAlt = m.acousticPanelAlternate != null ? m.acousticPanelAlternate : acoustic;
        Material metal = m.structuralMetal != null ? m.structuralMetal : m.furnitureMetal;
        Material fabric = m.acousticFabric != null ? m.acousticFabric : acoustic;

        for (float z = -s.hallDepth * 0.5f + 0.6f; z < s.hallDepth * 0.5f - 0.25f; z += 1.55f)
        for (float x = -s.hallWidth * 0.5f + 0.55f; x < s.hallWidth * 0.5f - 0.25f; x += 1.55f)
        {
            panels.AddBox(Mathf.RoundToInt((x + z) * 8f) % 4 == 0 ? acousticAlt : acoustic,
                new Vector3(x, s.wallHeight - 0.18f, z), new Vector3(1.38f, 0.07f, 1.38f));
        }
        for (float x = -s.hallWidth * 0.5f + 0.1f; x <= s.hallWidth * 0.5f; x += 1.52f)
            panels.AddBox(metal, new Vector3(x, s.wallHeight - 0.08f, 0f), new Vector3(0.05f, 0.1f, s.hallDepth));
        for (float z = -s.hallDepth * 0.5f + 0.1f; z <= s.hallDepth * 0.5f; z += 1.52f)
            panels.AddBox(metal, new Vector3(0f, s.wallHeight - 0.075f, z), new Vector3(s.hallWidth, 0.11f, 0.05f));

        AddCurvedCanopy(panels, fabric, metal, s);

        // Cylindrical ventilation duct (photo 01 / ceiling refs).
        panels.AddCylinder(metal, new Vector3(-5.7f, s.wallHeight - 0.22f, 0f), 0.16f, s.hallDepth - 1.4f, 14, Vector3.forward);
        for (float dz = -10f; dz < 11f; dz += 1.4f)
            panels.AddBox(acousticAlt, new Vector3(-5.7f, s.wallHeight - 0.08f, dz), new Vector3(0.22f, 0.02f, 0.08f));

        panels.Apply("Acoustic Ceiling Modules, Trusses, Canopy and HVAC", false);

        Transform fixtures = Root("Ceiling_Light_Fittings", ceilingObject.transform).transform;
        MeshBatch lights = new MeshBatch(fixtures);
        Material fixture = m.fixtureFinish != null ? m.fixtureFinish : m.accent;
        Material emissive = m.fixtureEmissive != null ? m.fixtureEmissive : fixture;
        for (int row = 0; row < 5; row++)
        for (int col = 0; col < 4; col++)
        {
            float lx = -5.4f + col * 3.6f;
            float lz = -9.2f + row * 4.5f;
            lights.AddCylinder(metal, new Vector3(lx, s.wallHeight - 0.28f, lz), 0.09f, 0.06f, 10, Vector3.up);
            lights.AddCylinder(emissive, new Vector3(lx, s.wallHeight - 0.32f, lz), 0.07f, 0.02f, 10, Vector3.up);
        }
        lights.Apply("Recessed Warm Ceiling Lights", false);
        return ceilingObject;
    }

    private static void AddCurvedCanopy(MeshBatch batch, Material fabric, Material metal, AuditoriumDemoSettings s)
    {
        float[] xs = { -5.4f, -4.3f, -3.1f, -1.7f, 0f, 1.7f, 3.1f, 4.3f, 5.4f };
        for (float z = -s.hallDepth * 0.5f + 2.4f; z < s.hallDepth * 0.5f - 1.5f; z += 2.1f)
        {
            for (int i = 0; i < xs.Length - 1; i++)
            {
                float y0 = s.wallHeight - 0.40f + 0.18f * Mathf.Pow(xs[i] / 5.4f, 2f);
                float y1 = s.wallHeight - 0.40f + 0.18f * Mathf.Pow(xs[i + 1] / 5.4f, 2f);
                Vector3 a = new Vector3(xs[i], y0, z);
                Vector3 b = new Vector3(xs[i + 1], y1, z);
                Vector3 c = new Vector3(xs[i + 1], y1, z + 1.55f);
                Vector3 d = new Vector3(xs[i], y0, z + 1.55f);
                batch.AddQuad(fabric, a, b, c, d);
                // Folded edge thickness cue.
                batch.AddQuad(fabric,
                    a + Vector3.down * 0.035f, b + Vector3.down * 0.035f, b, a);
            }
            // Sparse circular perforation cues along the canopy edge.
            for (int p = 0; p < 6; p++)
            {
                float px = -4.2f + p * 1.7f;
                float py = s.wallHeight - 0.40f + 0.18f * Mathf.Pow(px / 5.4f, 2f) - 0.02f;
                batch.AddCylinder(metal, new Vector3(px, py, z + 0.75f), 0.035f, 0.01f, 8, Vector3.up);
            }
        }
    }

    private static void BuildWallFixtures(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m)
    {
        Transform root = Root("WallFixtures").transform;
        MeshBatch fixtures = new MeshBatch(root);
        Material metal = m.fanMetal != null ? m.fanMetal : (m.structuralMetal != null ? m.structuralMetal : m.furnitureMetal);
        Material blade = m.fanBlade != null ? m.fanBlade : (m.fixtureFinish != null ? m.fixtureFinish : metal);
        Material speaker = m.speakerFinish != null ? m.speakerFinish : m.backdrop;
        Material grille = m.speakerGrille != null ? m.speakerGrille : speaker;
        Material plateMat = m.column != null ? m.column : m.wall;

        float clad = s.ResolvedCladdingHeight;
        float fanY = Mathf.Clamp(s.wallFanMountHeight, clad + 0.45f, s.wallHeight - 0.40f);
        float fanDiameter = Mathf.Clamp(s.wallFanDiameter, 0.5f, 0.72f);
        float bay = Mathf.Max(2.8f, s.sideWallBaySpacing);

        for (int side = 0; side < 2; side++)
        {
            bool isRight = side == 1;
            float faceSign = isRight ? -1f : 1f;
            float x = (isRight ? 1f : -1f) * (s.hallWidth * 0.5f - 0.12f);
            WallBayPlan[] bays = BuildBayPlan(isRight, s);

            for (int i = 0; i < bays.Length; i++)
            {
                WallBayPlan plan = bays[i];
                float nextZ = i + 1 < bays.Length ? bays[i + 1].CenterZ : plan.CenterZ + bay;
                float midZ = (plan.CenterZ + nextZ) * 0.5f;

                if (plan.HasFan)
                    AddWallFan(fixtures, metal, blade, new Vector3(x, fanY, midZ), faceSign, fanDiameter);

                if (plan.HasSpeaker)
                {
                    // Clear plaster only; offset along Z from the fan so cabinets never coincide.
                    float speakerZ = plan.HasFan ? midZ + 0.95f : midZ;
                    float sy = Mathf.Min(s.wallHeight - 0.50f, clad + (s.wallHeight - clad) * 0.78f);
                    float sx = x + faceSign * 0.10f;
                    fixtures.AddBox(metal, new Vector3(x + faceSign * 0.02f, sy + 0.12f, speakerZ),
                        new Vector3(0.08f, 0.06f, 0.20f));
                    fixtures.AddBox(speaker, new Vector3(sx, sy, speakerZ), new Vector3(0.20f, 0.42f, 0.32f));
                    fixtures.AddBox(grille, new Vector3(sx + faceSign * 0.105f, sy, speakerZ),
                        new Vector3(0.025f, 0.34f, 0.24f));
                }

                if (plan.HasPlate)
                {
                    fixtures.AddBox(plateMat, new Vector3(x + faceSign * 0.06f, clad + 0.14f, plan.CenterZ + 0.55f),
                        new Vector3(0.05f, 0.18f, 0.14f));
                    fixtures.AddBox(metal, new Vector3(x + faceSign * 0.09f, clad + 0.14f, plan.CenterZ + 0.55f),
                        new Vector3(0.02f, 0.045f, 0.035f));
                }
            }
        }
        fixtures.Apply("Wall Fans, Speakers and Electrical Fixtures", false);
    }

    private static void AddWallFan(MeshBatch batch, Material metal, Material bladeMat, Vector3 center, float faceSign, float diameter)
    {
        float radius = diameter * 0.5f;
        Vector3 hub = center + new Vector3(faceSign * 0.16f, 0f, 0f);
        // Wall plate + neck + motor housing.
        batch.AddBox(metal, center + new Vector3(faceSign * 0.02f, 0f, 0f), new Vector3(0.06f, 0.16f, 0.16f));
        batch.AddCylinder(metal, center + new Vector3(faceSign * 0.08f, 0f, 0f), 0.035f, 0.14f, 10, Vector3.right * faceSign);
        batch.AddCylinder(metal, hub + new Vector3(faceSign * -0.04f, 0f, 0f), 0.09f, 0.10f, 14, Vector3.right * faceSign);

        // Outer guard ring + concentric rings (thicker tubes for readability).
        for (int ring = 1; ring <= 4; ring++)
        {
            float rr = radius * (0.35f + 0.65f * ring / 4f);
            batch.AddRing(metal, hub, rr, 0.014f, 24, Vector3.right * faceSign);
        }
        for (int spoke = 0; spoke < 10; spoke++)
        {
            float ang = spoke * 36f * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(0f, Mathf.Sin(ang), Mathf.Cos(ang));
            batch.AddRotatedBox(metal, hub + dir * (radius * 0.48f),
                new Vector3(0.016f, 0.016f, radius * 0.92f),
                Quaternion.LookRotation(dir, Vector3.right * faceSign));
        }

        // Three recognisable blades with thickness.
        for (int b = 0; b < 3; b++)
        {
            float ang = b * 120f;
            Quaternion rot = Quaternion.AngleAxis(ang, Vector3.right * faceSign) * Quaternion.Euler(0f, 0f, 18f);
            Vector3 tip = hub + rot * new Vector3(0f, 0f, radius * 0.40f);
            batch.AddRotatedBox(bladeMat, tip, new Vector3(0.04f, 0.13f, radius * 0.78f), rot);
        }
        batch.AddCylinder(metal, hub, 0.06f, 0.05f, 14, Vector3.right * faceSign);
    }

    private static void BuildLighting(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m)
    {
        Transform root = Root("Lighting").transform;
        Color warm = s.WarmLightColor;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // Keep plaster readable and floor medium-grey — avoid near-black ambient ground.
        RenderSettings.ambientSkyColor = new Color(0.82f, 0.78f, 0.70f);
        RenderSettings.ambientEquatorColor = new Color(0.64f, 0.58f, 0.50f);
        RenderSettings.ambientGroundColor = new Color(0.42f, 0.40f, 0.38f);
        RenderSettings.ambientIntensity = 1.05f;
        RenderSettings.fog = false;

        GameObject keyObject = new GameObject("Warm Interior Key Light");
        keyObject.transform.SetParent(root, false);
        keyObject.transform.rotation = Quaternion.Euler(50f, -18f, 0f);
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = warm;
        key.intensity = 1.05f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.22f;

        CreateSpot(root, "Stage Spot Left", new Vector3(-2.8f, 3.35f, s.StageCenterZ - 2.4f),
            new Vector3(-1.2f, 1.6f, s.StageCenterZ + 0.8f), warm, s.stageSpotIntensity, 46f, 12f);
        CreateSpot(root, "Stage Spot Right", new Vector3(2.8f, 3.35f, s.StageCenterZ - 2.4f),
            new Vector3(1.2f, 1.6f, s.StageCenterZ + 0.8f), warm, s.stageSpotIntensity, 46f, 12f);
        CreateSpot(root, "Rear Logo Wash", new Vector3(0f, 3.15f, -s.hallDepth * 0.5f + 2.4f),
            new Vector3(0f, 1.95f, -s.hallDepth * 0.5f + 0.25f), warm,
            Mathf.Min(1.35f, s.rearLogoWashIntensity * 0.65f), 42f, 8f);
        CreateSpot(root, "Side Wash Left", new Vector3(-4.5f, 3.1f, 0f),
            new Vector3(-7.5f, 1.8f, 0f), warm, s.downlightIntensity * 0.55f, 70f, 14f);
        CreateSpot(root, "Side Wash Right", new Vector3(4.5f, 3.1f, 0f),
            new Vector3(7.5f, 1.8f, 0f), warm, s.downlightIntensity * 0.55f, 70f, 14f);
        CreateSpot(root, "Stage Backdrop Accent", new Vector3(0f, 3.3f, s.StageCenterZ - 0.4f),
            new Vector3(0f, 2.2f, s.hallDepth * 0.5f - 0.2f), warm, s.stageSpotIntensity * 0.55f, 50f, 10f);

        // A few practical point fills matching ceiling fittings (kept few for M3 8 GB).
        for (int i = 0; i < 4; i++)
        {
            float z = -8f + i * 5.2f;
            CreatePoint(root, "Warm Downlight Fill " + i, new Vector3((i % 2 == 0) ? -2.2f : 2.2f, 3.25f, z),
                warm, s.downlightIntensity * 0.45f, 7.5f);
        }
    }

    private static void CreateSpot(Transform parent, string name, Vector3 position, Vector3 target,
        Color color, float intensity, float angle, float range)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = angle;
        light.shadows = LightShadows.None;
    }

    private static void CreatePoint(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static GameObject BuildReferenceSeating(AuditoriumDemoSettings s, AuditoriumMaterialLibrary m)
    {
        Transform root = Root("ReferenceSeating").transform;
        MeshBatch chairs = new MeshBatch(root);
        Material seat = m.speakerFinish != null ? m.speakerFinish : m.backdrop;
        Material seatEdge = m.woodWalnut != null ? m.woodWalnut : m.backdrop;
        Material metal = m.structuralMetal != null ? m.structuralMetal : m.furnitureMetal;
        int rows = Mathf.Clamp(s.referenceSeatingRows, 3, 7);
        const int perSide = 5;
        const float chairPitch = 0.78f;
        for (int row = 0; row < rows; row++)
        {
            float z = 5.3f - row * s.referenceSeatingRowSpacing;
            for (int side = -1; side <= 1; side += 2)
            for (int col = 0; col < perSide; col++)
            {
                float x = side * (0.9f + col * chairPitch);
                chairs.AddBox(seat, new Vector3(x, 0.55f, z), new Vector3(0.58f, 0.13f, 0.54f));
                chairs.AddBox(seatEdge, new Vector3(x, 0.91f, z + 0.22f), new Vector3(0.58f, 0.55f, 0.12f));
                chairs.AddBox(metal, new Vector3(x - 0.20f, 0.27f, z - 0.16f), new Vector3(0.045f, 0.54f, 0.045f));
                chairs.AddBox(metal, new Vector3(x + 0.20f, 0.27f, z - 0.16f), new Vector3(0.045f, 0.54f, 0.045f));
            }
        }
        MeshCollider seatingCollider = chairs.Apply("Fixed Reference Auditorium Chairs", true).GetComponent<MeshCollider>();
        if (seatingCollider != null) seatingCollider.sharedMesh = seatingCollider.GetComponent<MeshFilter>().sharedMesh;
        return root.gameObject;
    }

    private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return obj;
    }

    private static GameObject Root(string name, Transform parent = null)
    {
        GameObject obj = new GameObject(name);
        if (parent != null) obj.transform.SetParent(parent, false);
        return obj;
    }

    private static GameObject FindSceneRoot(string name)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].name == name) return roots[i];
        return null;
    }

    private static GameObject Quad(string name, Transform parent, Vector3 position, Vector2 size, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return obj;
    }

    /// <summary>
    /// Thin cube board visible from both sides (avoids single-sided Quad backface culling).
    /// </summary>
    private static GameObject TexturedBoard(string name, Transform parent, Vector3 position, Vector2 size,
        float thickness, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = new Vector3(size.x, size.y, Mathf.Max(0.02f, thickness));
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        return obj;
    }

    private sealed class MeshBatch
    {
        private readonly Transform parent;
        private readonly List<Material> materials = new List<Material>();
        private readonly List<List<int>> triangles = new List<List<int>>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();

        public MeshBatch(Transform parent) { this.parent = parent; }

        public void AddBox(Material material, Vector3 center, Vector3 size)
        {
            AddBox(material, center, size, Vector2.one);
        }

        public void AddBox(Material material, Vector3 center, Vector3 size, Vector2 uvScale)
        {
            if (material == null || size.x <= 0f || size.y <= 0f || size.z <= 0f) return;
            Vector3 h = size * 0.5f;
            Vector3[] c =
            {
                center + new Vector3(-h.x,-h.y,-h.z), center + new Vector3(h.x,-h.y,-h.z),
                center + new Vector3(h.x,h.y,-h.z), center + new Vector3(-h.x,h.y,-h.z),
                center + new Vector3(-h.x,-h.y,h.z), center + new Vector3(h.x,-h.y,h.z),
                center + new Vector3(h.x,h.y,h.z), center + new Vector3(-h.x,h.y,h.z)
            };
            AddFace(material, c[0], c[3], c[2], c[1], uvScale);
            AddFace(material, c[4], c[5], c[6], c[7], uvScale);
            AddFace(material, c[0], c[4], c[7], c[3], uvScale);
            AddFace(material, c[1], c[2], c[6], c[5], uvScale);
            AddFace(material, c[3], c[7], c[6], c[2], uvScale);
            AddFace(material, c[0], c[1], c[5], c[4], uvScale);
        }

        public void AddBox(Material material, Vector3 center, Vector3 size, Quaternion rotation)
        {
            AddRotatedBox(material, center, size, rotation);
        }

        public void AddRotatedBox(Material material, Vector3 center, Vector3 size, Quaternion rotation)
        {
            if (material == null) return;
            Vector3 h = size * 0.5f;
            Vector3[] p =
            {
                new Vector3(-h.x,-h.y,-h.z), new Vector3(h.x,-h.y,-h.z), new Vector3(h.x,h.y,-h.z), new Vector3(-h.x,h.y,-h.z),
                new Vector3(-h.x,-h.y,h.z), new Vector3(h.x,-h.y,h.z), new Vector3(h.x,h.y,h.z), new Vector3(-h.x,h.y,h.z)
            };
            for (int i = 0; i < p.Length; i++) p[i] = center + rotation * p[i];
            AddFace(material, p[0], p[3], p[2], p[1], Vector2.one);
            AddFace(material, p[4], p[5], p[6], p[7], Vector2.one);
            AddFace(material, p[0], p[4], p[7], p[3], Vector2.one);
            AddFace(material, p[1], p[2], p[6], p[5], Vector2.one);
            AddFace(material, p[3], p[7], p[6], p[2], Vector2.one);
            AddFace(material, p[0], p[1], p[5], p[4], Vector2.one);
        }

        public void AddQuad(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int sub = GetSubmesh(material);
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            float uScale = Vector3.Distance(a, b);
            float vScale = Vector3.Distance(a, d);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(uScale, 0f));
            uvs.Add(new Vector2(uScale, vScale));
            uvs.Add(new Vector2(0f, vScale));
            triangles[sub].Add(start); triangles[sub].Add(start + 1); triangles[sub].Add(start + 2);
            triangles[sub].Add(start); triangles[sub].Add(start + 2); triangles[sub].Add(start + 3);
        }

        public void AddCylinder(Material material, Vector3 center, float radius, float length, int segments, Vector3 axis)
        {
            if (material == null || radius <= 0f || length <= 0f) return;
            Vector3 n = axis.normalized;
            Vector3 tangent = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 bitangent = Vector3.Cross(n, tangent);
            Vector3 start = center - n * (length * 0.5f);
            Vector3 end = center + n * (length * 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                Vector3 r0 = tangent * Mathf.Cos(a0) * radius + bitangent * Mathf.Sin(a0) * radius;
                Vector3 r1 = tangent * Mathf.Cos(a1) * radius + bitangent * Mathf.Sin(a1) * radius;
                AddFace(material, start + r0, start + r1, end + r1, end + r0, Vector2.one);
            }
        }

        public void AddRing(Material material, Vector3 center, float radius, float tube, int segments, Vector3 axis)
        {
            if (material == null) return;
            Vector3 n = axis.normalized;
            Vector3 tangent = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 bitangent = Vector3.Cross(n, tangent);
            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                Vector3 p0 = center + tangent * Mathf.Cos(a0) * radius + bitangent * Mathf.Sin(a0) * radius;
                Vector3 p1 = center + tangent * Mathf.Cos(a1) * radius + bitangent * Mathf.Sin(a1) * radius;
                Vector3 radial0 = (p0 - center).normalized;
                Vector3 radial1 = (p1 - center).normalized;
                AddFace(material,
                    p0 - radial0 * tube - n * tube,
                    p1 - radial1 * tube - n * tube,
                    p1 + radial1 * tube + n * tube,
                    p0 + radial0 * tube + n * tube,
                    Vector2.one);
            }
        }

        private void AddFace(Material material, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvScale)
        {
            int sub = GetSubmesh(material);
            int start = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            Vector3 normal = Vector3.Cross(b - a, d - a).normalized;
            if (Mathf.Abs(normal.y) >= Mathf.Abs(normal.x) && Mathf.Abs(normal.y) >= Mathf.Abs(normal.z))
            {
                uvs.Add(new Vector2(a.x, a.z) * uvScale);
                uvs.Add(new Vector2(b.x, b.z) * uvScale);
                uvs.Add(new Vector2(c.x, c.z) * uvScale);
                uvs.Add(new Vector2(d.x, d.z) * uvScale);
            }
            else if (Mathf.Abs(normal.x) >= Mathf.Abs(normal.z))
            {
                uvs.Add(new Vector2(a.z, a.y) * uvScale);
                uvs.Add(new Vector2(b.z, b.y) * uvScale);
                uvs.Add(new Vector2(c.z, c.y) * uvScale);
                uvs.Add(new Vector2(d.z, d.y) * uvScale);
            }
            else
            {
                uvs.Add(new Vector2(a.x, a.y) * uvScale);
                uvs.Add(new Vector2(b.x, b.y) * uvScale);
                uvs.Add(new Vector2(c.x, c.y) * uvScale);
                uvs.Add(new Vector2(d.x, d.y) * uvScale);
            }
            triangles[sub].Add(start); triangles[sub].Add(start + 1); triangles[sub].Add(start + 2);
            triangles[sub].Add(start); triangles[sub].Add(start + 2); triangles[sub].Add(start + 3);
        }

        private int GetSubmesh(Material material)
        {
            int index = materials.IndexOf(material);
            if (index >= 0) return index;
            materials.Add(material);
            triangles.Add(new List<int>());
            return materials.Count - 1;
        }

        public GameObject Apply(string name, bool meshCollider)
        {
            GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false);
            Mesh mesh = new Mesh();
            mesh.name = name + " Mesh";
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = materials.Count;
            for (int i = 0; i < triangles.Count; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
            if (meshCollider)
            {
                MeshCollider collider = obj.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }
            return obj;
        }
    }
}
