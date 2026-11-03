using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds reusable furniture roots from built-in primitives and shared materials.
/// Preview and placed objects use this same geometry and collider setup.
/// </summary>
public static class FurnitureFactory
{
    private static Mesh sharedChairSeatMesh;
    private static Mesh sharedChairBackMesh;

    public static GameObject Create(FurnitureType type, Transform parent, AuditoriumMaterialLibrary materials, bool isPreview)
    {
        FurnitureDefinition definition = FurnitureCatalog.Get(type);
        GameObject root = new GameObject(definition.DisplayName);
        if (parent != null) root.transform.SetParent(parent, false);
        root.AddComponent<FurnitureInstance>().Configure(type, isPreview);

        switch (type)
        {
            case FurnitureType.Bench:
                BuildBench(root.transform, materials);
                break;
            case FurnitureType.RoundTable:
                BuildRoundTable(root.transform, materials);
                break;
            case FurnitureType.SquareTable:
                BuildSquareTable(root.transform, materials);
                break;
            case FurnitureType.FlowerPot:
                BuildFlowerPot(root.transform, materials);
                break;
            case FurnitureType.ClassroomDesk:
                BuildClassroomDesk(root.transform, materials);
                break;
            case FurnitureType.Chair:
                BuildChair(root.transform, materials);
                break;
            case FurnitureType.AuditoriumChair:
                BuildAuditoriumChair(root.transform, materials);
                break;
            case FurnitureType.LoungeChair:
                BuildLoungeChair(root.transform, materials);
                break;
            case FurnitureType.EventLectern:
                BuildEventLectern(root.transform, materials);
                break;
            case FurnitureType.MicrophoneStand:
                BuildMicrophoneStand(root.transform, materials);
                break;
            case FurnitureType.PaFloorSpeaker:
                BuildPaFloorSpeaker(root.transform, materials);
                break;
            case FurnitureType.RollupBanner:
                BuildRollupBanner(root.transform, materials);
                break;
            case FurnitureType.RegistrationTable:
                BuildRegistrationTable(root.transform, materials);
                break;
        }

        return root;
    }

    private static void BuildBench(Transform root, AuditoriumMaterialLibrary materials)
    {
        float halfWidth = 0.78f;
        MakePart("Bench Seat", PrimitiveType.Cube, root, new Vector3(0f, 0.47f, 0f), new Vector3(1.8f, 0.12f, 0.52f), materials.benchWood);
        for (int xSide = -1; xSide <= 1; xSide += 2)
        {
            for (int zSide = -1; zSide <= 1; zSide += 2)
            {
                MakePart("Bench Leg", PrimitiveType.Cube, root,
                    new Vector3(xSide * halfWidth, 0.225f, zSide * 0.19f),
                    new Vector3(0.1f, 0.45f, 0.1f), materials.furnitureMetal);
            }
        }

        MakePart("Bench Back", PrimitiveType.Cube, root,
            new Vector3(0f, 0.75f, 0.235f), new Vector3(1.8f, 0.42f, 0.09f), materials.benchWood);
        for (int side = -1; side <= 1; side += 2)
        {
            MakePart("Bench Back Support", PrimitiveType.Cube, root,
                new Vector3(side * 0.78f, 0.68f, 0.21f), new Vector3(0.08f, 0.43f, 0.08f), materials.furnitureMetal);
        }
    }

    private static void BuildRoundTable(Transform root, AuditoriumMaterialLibrary materials)
    {
        MakePart("Round Table Top", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.72f, 0f), new Vector3(0.65f, 0.045f, 0.65f), materials.tableTop);
        MakePart("Round Table Pedestal", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.38f, 0f), new Vector3(0.13f, 0.34f, 0.13f), materials.furnitureMetal);
        MakePart("Round Table Base", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.045f, 0f), new Vector3(0.31f, 0.045f, 0.31f), materials.furnitureMetal);
    }

    private static void BuildSquareTable(Transform root, AuditoriumMaterialLibrary materials)
    {
        MakePart("Square Table Top", PrimitiveType.Cube, root,
            new Vector3(0f, 0.72f, 0f), new Vector3(1.25f, 0.1f, 1.25f), materials.tableTop);
        for (int xSide = -1; xSide <= 1; xSide += 2)
        {
            for (int zSide = -1; zSide <= 1; zSide += 2)
            {
                MakePart("Square Table Leg", PrimitiveType.Cube, root,
                    new Vector3(xSide * 0.51f, 0.35f, zSide * 0.51f),
                    new Vector3(0.09f, 0.7f, 0.09f), materials.furnitureMetal);
            }
        }
    }

    private static void BuildFlowerPot(Transform root, AuditoriumMaterialLibrary materials)
    {
        MakePart("Pot Body", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.24f, 0f), new Vector3(0.25f, 0.24f, 0.25f), materials.flowerPot);
        MakePart("Pot Rim", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.49f, 0f), new Vector3(0.29f, 0.045f, 0.29f), materials.flowerPot);
        MakePart("Pot Soil", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.525f, 0f), new Vector3(0.23f, 0.018f, 0.23f), materials.furnitureSoil);
        MakePart("Plant Stem", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.69f, 0f), new Vector3(0.025f, 0.18f, 0.025f), materials.foliage);

        Vector3[] leafPositions =
        {
            new Vector3(0f, 0.79f, 0f),
            new Vector3(0.14f, 0.68f, 0f),
            new Vector3(-0.14f, 0.68f, 0f),
            new Vector3(0f, 0.67f, 0.14f),
            new Vector3(0f, 0.67f, -0.14f)
        };
        for (int i = 0; i < leafPositions.Length; i++)
        {
            GameObject leaf = MakePart("Plant Leaf", PrimitiveType.Sphere, root, leafPositions[i],
                i == 0 ? new Vector3(0.2f, 0.28f, 0.2f) : new Vector3(0.24f, 0.14f, 0.14f), materials.foliage);
            if (i > 0) leaf.transform.localRotation = Quaternion.Euler(0f, (i - 1) * 90f, 32f);
        }
    }

    private static void BuildClassroomDesk(Transform root, AuditoriumMaterialLibrary materials)
    {
        // The root pivot is at floor level. Separate slab, frame, panel and feet
        // keep the desk readable while staying within its catalog footprint.
        MakePart("Desk Tabletop", PrimitiveType.Cube, root,
            new Vector3(0f, 0.728f, 0f), new Vector3(1.13f, 0.044f, 0.51f), materials.classroomDeskTop);
        Vector3[] corners =
        {
            new Vector3(-0.565f, 0.728f, -0.235f), new Vector3(0.565f, 0.728f, -0.235f),
            new Vector3(-0.565f, 0.728f, 0.235f), new Vector3(0.565f, 0.728f, 0.235f)
        };
        for (int i = 0; i < corners.Length; i++)
            MakePart("Rounded Tabletop Corner", PrimitiveType.Sphere, root, corners[i],
                new Vector3(0.07f, 0.044f, 0.07f), materials.classroomDeskTop, false);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 0.50f;
            CreateTube("Desk Frame Front Leg", root, new Vector3(x * 1.02f, 0.035f, 0.22f),
                new Vector3(x, 0.69f, 0.17f), 0.026f, materials.classroomDeskBlue);
            CreateTube("Desk Frame Back Leg", root, new Vector3(x * 1.02f, 0.035f, -0.22f),
                new Vector3(x, 0.69f, -0.17f), 0.026f, materials.classroomDeskBlue);
            CreateTube("Desk Side Foot Rail", root, new Vector3(x * 1.02f, 0.055f, -0.22f),
                new Vector3(x * 1.02f, 0.055f, 0.22f), 0.025f, materials.classroomDeskBlue);
            CreateTube("Desk Side Top Rail", root, new Vector3(x, 0.67f, -0.18f),
                new Vector3(x, 0.67f, 0.18f), 0.022f, materials.classroomDeskBlue);
            MakePart("Desk Foot Cap", PrimitiveType.Cube, root,
                new Vector3(x * 1.02f, 0.018f, 0f), new Vector3(0.09f, 0.035f, 0.49f), materials.furnitureMetal);
        }
        CreateTube("Desk Rear Support Rail", root, new Vector3(-0.49f, 0.66f, -0.17f),
            new Vector3(0.49f, 0.66f, -0.17f), 0.022f, materials.classroomDeskBlue);
        MakePart("Desk Blue Modesty Panel", PrimitiveType.Cube, root,
            new Vector3(0f, 0.515f, -0.145f), new Vector3(1.00f, 0.27f, 0.025f), materials.classroomDeskBlue);

        // Small dark inset dots suggest punched holes without generating thin
        // unstable hole meshes or adding collision geometry to the panel.
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < 11; column++)
            {
                float x = -0.39f + column * 0.078f + (row == 0 ? 0f : 0.039f);
                float y = 0.475f + row * 0.055f;
                GameObject perforation = MakePart("Modesty Panel Perforation", PrimitiveType.Cylinder, root,
                    new Vector3(x, y, -0.161f), new Vector3(0.018f, 0.004f, 0.018f), materials.classroomPerforation, false);
                perforation.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }

    private static void BuildChair(Transform root, AuditoriumMaterialLibrary materials)
    {
        if (sharedChairSeatMesh == null) sharedChairSeatMesh = CreateCurvedSeatMesh();
        if (sharedChairBackMesh == null) sharedChairBackMesh = CreateCurvedBackMesh();
        MakeMeshPart("Moulded Chair Seat", root, sharedChairSeatMesh, materials.chairBlue,
            new Vector3(0f, 0.445f, 0.01f), new Vector3(0.46f, 0.10f, 0.43f));
        MakeMeshPart("Curved Chair Backrest", root, sharedChairBackMesh, materials.chairBlue,
            new Vector3(0f, 0.655f, -0.18f), new Vector3(0.43f, 0.36f, 0.10f));

        for (int xSide = -1; xSide <= 1; xSide += 2)
        {
            for (int zSide = -1; zSide <= 1; zSide += 2)
            {
                float seatX = xSide * 0.145f;
                float floorX = xSide * 0.175f;
                float seatZ = zSide > 0 ? 0.145f : -0.12f;
                float floorZ = zSide > 0 ? 0.18f : -0.17f;
                CreateTube("Chair Metal Leg", root, new Vector3(floorX, 0.025f, floorZ),
                    new Vector3(seatX, 0.43f, seatZ), 0.022f, materials.chairMetal);
            }
        }
        CreateTube("Chair Front Support Rail", root, new Vector3(-0.145f, 0.30f, 0.12f),
            new Vector3(0.145f, 0.30f, 0.12f), 0.016f, materials.chairMetal);
        CreateTube("Chair Rear Support Rail", root, new Vector3(-0.145f, 0.31f, -0.11f),
            new Vector3(0.145f, 0.31f, -0.11f), 0.016f, materials.chairMetal);
        for (int side = -1; side <= 1; side += 2)
            CreateTube("Chair Back Support", root, new Vector3(side * 0.15f, 0.42f, -0.11f),
                new Vector3(side * 0.16f, 0.59f, -0.19f), 0.018f, materials.chairMetal);
    }

    private static void BuildAuditoriumChair(Transform root, AuditoriumMaterialLibrary materials)
    {
        Material shell = materials.backdrop != null ? materials.backdrop : materials.benchWood;
        MakePart("Auditorium Chair Seat Shell", PrimitiveType.Cube, root,
            new Vector3(0f, 0.45f, 0.015f), new Vector3(0.46f, 0.09f, 0.43f), shell);
        GameObject back = MakePart("Auditorium Chair Curved Back", PrimitiveType.Cube, root,
            new Vector3(0f, 0.64f, -0.19f), new Vector3(0.46f, 0.34f, 0.075f), shell);
        back.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int front = -1; front <= 1; front += 2)
            {
                float z = front > 0 ? 0.16f : -0.16f;
                CreateTube("Auditorium Chair Metal Leg", root,
                    new Vector3(side * 0.16f, 0.025f, z + (front < 0 ? -0.015f : 0f)),
                    new Vector3(side * 0.14f, 0.42f, z), 0.018f, materials.chairMetal);
            }
        }
        for (int side = -1; side <= 1; side += 2)
            CreateTube("Auditorium Chair Back Support", root,
                new Vector3(side * 0.15f, 0.42f, -0.14f), new Vector3(side * 0.16f, 0.76f, -0.20f),
                0.018f, materials.chairMetal);
    }

    private static void BuildLoungeChair(Transform root, AuditoriumMaterialLibrary materials)
    {
        Material upholstery = materials.chairBlue != null ? materials.chairBlue : materials.benchWood;
        MakePart("Lounge Chair Seat Cushion", PrimitiveType.Cube, root,
            new Vector3(0f, 0.43f, 0.02f), new Vector3(0.72f, 0.20f, 0.62f), upholstery);
        MakePart("Lounge Chair Back Cushion", PrimitiveType.Cube, root,
            new Vector3(0f, 0.69f, -0.25f), new Vector3(0.72f, 0.48f, 0.20f), upholstery);
        MakePart("Lounge Chair Base", PrimitiveType.Cube, root,
            new Vector3(0f, 0.25f, 0.02f), new Vector3(0.72f, 0.18f, 0.64f), materials.benchWood);
        for (int side = -1; side <= 1; side += 2)
        {
            MakePart("Lounge Chair Arm", PrimitiveType.Cube, root,
                new Vector3(side * 0.34f, 0.54f, 0.02f), new Vector3(0.12f, 0.30f, 0.66f), materials.benchWood);
            MakePart("Lounge Chair Foot", PrimitiveType.Cube, root,
                new Vector3(side * 0.27f, 0.07f, 0.23f), new Vector3(0.12f, 0.14f, 0.12f), materials.furnitureMetal);
            MakePart("Lounge Chair Foot", PrimitiveType.Cube, root,
                new Vector3(side * 0.27f, 0.07f, -0.23f), new Vector3(0.12f, 0.14f, 0.12f), materials.furnitureMetal);
        }
    }

    private static void BuildEventLectern(Transform root, AuditoriumMaterialLibrary materials)
    {
        Material wood = materials.podiumWood != null ? materials.podiumWood : materials.benchWood;
        MakePart("Mobile Lectern Pedestal", PrimitiveType.Cube, root,
            new Vector3(0f, 0.49f, 0f), new Vector3(0.48f, 0.94f, 0.42f), wood);
        GameObject readingSurface = MakePart("Mobile Lectern Sloped Reading Surface", PrimitiveType.Cube, root,
            new Vector3(0f, 1.00f, 0.015f), new Vector3(0.55f, 0.09f, 0.48f), wood);
        readingSurface.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        MakePart("Mobile Lectern Reading Lip", PrimitiveType.Cube, root,
            new Vector3(0f, 0.97f, 0.23f), new Vector3(0.55f, 0.055f, 0.045f), wood);
        CreateTube("Lectern Microphone Mount", root, new Vector3(0.18f, 1.02f, -0.16f),
            new Vector3(0.22f, 1.08f, -0.20f), 0.012f, materials.furnitureMetal);
    }

    private static void BuildMicrophoneStand(Transform root, AuditoriumMaterialLibrary materials)
    {
        Material metal = materials.furnitureMetal != null ? materials.furnitureMetal : materials.structuralMetal;
        MakePart("Microphone Stand Circular Base", PrimitiveType.Cylinder, root,
            new Vector3(0f, 0.025f, 0f), new Vector3(0.225f, 0.025f, 0.225f), metal);
        CreateTube("Microphone Stand Upright", root, new Vector3(0f, 0.05f, 0f),
            new Vector3(0f, 1.35f, 0f), 0.025f, metal);
        CreateTube("Microphone Stand Boom", root, new Vector3(-0.21f, 1.40f, 0f),
            new Vector3(0.13f, 1.49f, 0f), 0.016f, metal);
        GameObject microphone = MakePart("Microphone", PrimitiveType.Capsule, root,
            new Vector3(0.17f, 1.50f, 0f), new Vector3(0.045f, 0.09f, 0.045f), materials.speakerFinish);
        microphone.transform.localRotation = Quaternion.Euler(0f, 0f, 68f);
    }

    private static void BuildPaFloorSpeaker(Transform root, AuditoriumMaterialLibrary materials)
    {
        Material cabinet = materials.speakerFinish != null ? materials.speakerFinish : materials.backdrop;
        MakePart("PA Speaker Cabinet", PrimitiveType.Cube, root,
            new Vector3(0f, 0.35f, 0f), new Vector3(0.35f, 0.70f, 0.35f), cabinet);
        MakePart("PA Speaker Front Grille", PrimitiveType.Cube, root,
            new Vector3(0f, 0.35f, 0.181f), new Vector3(0.31f, 0.63f, 0.012f), materials.structuralMetal, false);
        for (int i = 0; i < 2; i++)
        {
            float y = i == 0 ? 0.22f : 0.49f;
            GameObject driver = MakePart("PA Speaker Driver", PrimitiveType.Cylinder, root,
                new Vector3(0f, y, 0.191f), new Vector3(i == 0 ? 0.105f : 0.055f, 0.007f, i == 0 ? 0.105f : 0.055f),
                materials.speakerFinish, false);
            driver.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private static void BuildRollupBanner(Transform root, AuditoriumMaterialLibrary materials)
    {
        Material metal = materials.furnitureMetal != null ? materials.furnitureMetal : materials.structuralMetal;
        MakePart("Roll-up Banner Base", PrimitiveType.Cube, root,
            new Vector3(0f, 0.09f, 0f), new Vector3(0.85f, 0.16f, 0.28f), metal);
        CreateTube("Roll-up Banner Support", root, new Vector3(0f, 0.16f, -0.08f),
            new Vector3(0f, 1.98f, -0.08f), 0.018f, metal);
        Material banner = materials.bannerArtworkMaterial != null ? materials.bannerArtworkMaterial : materials.bannerRed;
        MakePart("Roll-up Banner Display", PrimitiveType.Cube, root,
            new Vector3(0f, 1.12f, 0.04f), new Vector3(0.78f, 1.76f, 0.025f), banner);
        MakePart("Abstract Banner Accent", PrimitiveType.Cube, root,
            new Vector3(0f, 1.22f, 0.058f), new Vector3(0.70f, 0.42f, 0.012f), materials.bannerOrange, false);
        MakePart("Abstract Banner Accent", PrimitiveType.Cube, root,
            new Vector3(-0.13f, 0.85f, 0.067f), new Vector3(0.44f, 0.20f, 0.012f), materials.bannerMagenta, false);
    }

    private static void BuildRegistrationTable(Transform root, AuditoriumMaterialLibrary materials)
    {
        MakePart("Registration Table Top", PrimitiveType.Cube, root,
            new Vector3(0f, 0.72f, 0f), new Vector3(1.20f, 0.06f, 0.60f), materials.tableTop);
        for (int xSide = -1; xSide <= 1; xSide += 2)
        {
            for (int zSide = -1; zSide <= 1; zSide += 2)
            {
                MakePart("Registration Table Leg", PrimitiveType.Cube, root,
                    new Vector3(xSide * 0.53f, 0.35f, zSide * 0.24f), new Vector3(0.06f, 0.70f, 0.06f), materials.furnitureMetal);
            }
        }
        MakePart("Registration Table Modesty Panel", PrimitiveType.Cube, root,
            new Vector3(0f, 0.47f, 0.25f), new Vector3(1.02f, 0.40f, 0.025f), materials.benchWood);
    }

    private static Mesh CreateCurvedSeatMesh()
    {
        const int divisions = 4;
        int rowLength = divisions + 1;
        int layerSize = rowLength * rowLength;
        Vector3[] vertices = new Vector3[layerSize * 2];
        for (int row = 0; row <= divisions; row++)
        {
            float v = row / (float)divisions;
            float z = Mathf.Lerp(0.22f, -0.20f, v);
            for (int column = 0; column <= divisions; column++)
            {
                float u = column / (float)divisions;
                float x = Mathf.Lerp(-0.23f, 0.23f, u);
                float edge = Mathf.Abs(u - 0.5f) * 2f;
                float front = 1f - v;
                float y = 0.445f + 0.018f * (1f - edge) - 0.012f * front * front;
                int index = row * rowLength + column;
                vertices[index] = new Vector3(x, y, z);
                vertices[layerSize + index] = new Vector3(x, y - 0.07f, z);
            }
        }
        List<int> triangles = new List<int>();
        for (int row = 0; row < divisions; row++)
        {
            for (int column = 0; column < divisions; column++)
            {
                int a = row * rowLength + column;
                int b = a + 1;
                int c = a + rowLength;
                int d = c + 1;
                triangles.AddRange(new[] { a, b, c, b, d, c });
                int aa = layerSize + a;
                int bb = layerSize + b;
                int cc = layerSize + c;
                int dd = layerSize + d;
                triangles.AddRange(new[] { aa, cc, bb, bb, cc, dd });
            }
        }
        for (int edge = 0; edge < divisions; edge++)
        {
            AddSeatSide(triangles, edge, edge + 1, layerSize);
            AddSeatSide(triangles, divisions * rowLength + edge, divisions * rowLength + edge + 1, layerSize);
            AddSeatSide(triangles, edge * rowLength, (edge + 1) * rowLength, layerSize);
            AddSeatSide(triangles, edge * rowLength + divisions, (edge + 1) * rowLength + divisions, layerSize);
        }
        return CreateMesh("Curved Chair Seat Mesh", vertices, triangles);
    }

    private static void AddSeatSide(List<int> triangles, int first, int second, int layerSize)
    {
        triangles.Add(first); triangles.Add(first + layerSize); triangles.Add(second);
        triangles.Add(second); triangles.Add(first + layerSize); triangles.Add(second + layerSize);
    }

    private static Mesh CreateCurvedBackMesh()
    {
        const int columns = 4;
        const int rows = 4;
        int rowLength = columns + 1;
        int surfaceSize = rowLength * (rows + 1);
        Vector3[] vertices = new Vector3[surfaceSize * 2];
        for (int row = 0; row <= rows; row++)
        {
            float v = row / (float)rows;
            float y = Mathf.Lerp(0.49f, 0.82f, v);
            float width = Mathf.Lerp(0.39f, 0.42f, v);
            for (int column = 0; column <= columns; column++)
            {
                float u = column / (float)columns;
                float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, u);
                float curve = 0.025f * (1f - Mathf.Abs(u - 0.5f) * 2f);
                float z = -0.155f - (y - 0.49f) * 0.18f + curve;
                int index = row * rowLength + column;
                vertices[index] = new Vector3(x, y, z);
                vertices[surfaceSize + index] = new Vector3(x, y, z - 0.035f);
            }
        }
        List<int> triangles = new List<int>();
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int a = row * rowLength + column;
                int b = a + 1;
                int c = a + rowLength;
                int d = c + 1;
                triangles.AddRange(new[] { a, b, c, b, d, c });
                int aa = surfaceSize + a;
                int bb = surfaceSize + b;
                int cc = surfaceSize + c;
                int dd = surfaceSize + d;
                triangles.AddRange(new[] { aa, cc, bb, bb, cc, dd });
            }
        }
        for (int row = 0; row < rows; row++)
        {
            AddBackSide(triangles, row * rowLength, (row + 1) * rowLength, surfaceSize);
            AddBackSide(triangles, row * rowLength + columns, (row + 1) * rowLength + columns, surfaceSize);
        }
        for (int column = 0; column < columns; column++)
        {
            AddBackSide(triangles, column, column + 1, surfaceSize);
            AddBackSide(triangles, rows * rowLength + column, rows * rowLength + column + 1, surfaceSize);
        }
        return CreateMesh("Curved Chair Back Mesh", vertices, triangles);
    }

    private static void AddBackSide(List<int> triangles, int first, int second, int surfaceSize)
    {
        triangles.Add(first); triangles.Add(second); triangles.Add(first + surfaceSize);
        triangles.Add(second); triangles.Add(second + surfaceSize); triangles.Add(first + surfaceSize);
    }

    private static Mesh CreateMesh(string name, Vector3[] vertices, List<int> triangles)
    {
        Mesh mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static GameObject MakeMeshPart(string name, Transform parent, Mesh mesh, Material material,
        Vector3 colliderCenter, Vector3 colliderSize)
    {
        GameObject part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
        part.transform.SetParent(parent, false);
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        BoxCollider collider = part.GetComponent<BoxCollider>();
        collider.center = colliderCenter;
        collider.size = colliderSize;
        return part;
    }

    private static GameObject CreateTube(string name, Transform parent, Vector3 start, Vector3 end,
        float diameter, Material material)
    {
        Vector3 direction = end - start;
        GameObject tube = MakePart(name, PrimitiveType.Cylinder, parent, (start + end) * 0.5f,
            new Vector3(diameter, direction.magnitude * 0.5f, diameter), material);
        tube.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        return tube;
    }

    private static GameObject MakePart(string name, PrimitiveType primitive, Transform parent, Vector3 localPosition,
        Vector3 localScale, Material material, bool colliderEnabled = true)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) collider.enabled = colliderEnabled;
        return part;
    }
}
