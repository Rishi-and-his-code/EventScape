using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the demo auditorium scene and keeps its project assets/build entry repeatable.
/// </summary>
public static class BuildDemoScene
{
    private const string ScenePath = "Assets/Scenes/MainAuditorium.unity";
    private const string SettingsPath = "Assets/Settings/AuditoriumDemoSettings.asset";
    private const string MaterialLibraryPath = "Assets/Materials/AuditoriumMaterials.asset";
    private const string PipelinePath = "Assets/Settings/VirtualAuditoriumURP.asset";
    private const string BackupFolder = "Assets/Scenes/Backups";

    [MenuItem("Tools/Virtual Auditorium/Build Demo Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Stop Play Mode before rebuilding the auditorium architecture.", "OK");
            return;
        }

        try
        {
            BuildSafely();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Auditorium build failed", exception.Message, "OK");
        }
    }

    private static void BuildSafely()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Settings");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(BackupFolder);

        // Save modified work before backing up. The existing scene, placed layout,
        // cameras, UI and planner components are retained and only architecture roots are regenerated.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Virtual Auditorium rebuild cancelled because modified scene changes were not saved.");
            return;
        }
        if (File.Exists(ScenePath))
        {
            string backupPath = BackupFolder + "/MainAuditorium_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
            backupPath = AssetDatabase.GenerateUniqueAssetPath(backupPath);
            if (!AssetDatabase.CopyAsset(ScenePath, backupPath))
                throw new IOException("Could not create the safety backup at " + backupPath + ". The scene was not rebuilt.");
        }

        EnsureUrpSetup();
        Scene scene;
        if (File.Exists(ScenePath))
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AuditoriumDemoSettings settings = EnsureSettings();
        AuditoriumMaterialLibrary materials = EnsureMaterials();
        if (settings == null || materials == null)
            throw new InvalidOperationException("The demo settings or material library could not be loaded or created.");
        ApplyArtworkSlots(settings, materials);
        AssetDatabase.SaveAssets();

        GameObject ceiling = AuditoriumBuilder.Build(settings, materials);
        if (ceiling == null)
        {
            throw new InvalidOperationException("The auditorium builder returned no validated Auditorium_Ceiling; the scene was not saved.");
        }

        // Keep the interior visible when the generated scene is first opened in
        // the Scene view. CameraModeController enables the ceiling on Play.
        ceiling.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new InvalidOperationException("Could not save the demo scene to " + ScenePath + ".");
        }

        RegisterSceneInBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SelectEnvironmentRoot();
        FrameDemoSceneInSceneView(settings);
        GameViewScaleGuard.EnsureGameViewScaleOne("Build Demo Scene");
        Debug.Log("Virtual Auditorium architecture rebuilt at " + ScenePath + ". Existing planner roots and placed furniture were preserved; a scene backup was created under " + BackupFolder + ". Check Game View at Scale 1× — values above 1× crop side UI.");
    }

    private static AuditoriumDemoSettings EnsureSettings()
    {
        AuditoriumDemoSettings settings = AssetDatabase.LoadAssetAtPath<AuditoriumDemoSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<AuditoriumDemoSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        // Keep immutable room/stage frame; migrate doorway to photo-right (−X) of the rear wall.
        if (settings.walkthroughSpawnX > -4.5f)
            settings.walkthroughSpawnX = -5.0f;
        if (settings.doorwayInsetFromRight < 0.5f || settings.doorwayInsetFromRight > 3.5f)
            settings.doorwayInsetFromRight = 1.55f;
        // Dark double-door close-up: ~1.9 m pair with square vision lites.
        if (settings.doorwayWidth < 1.5f || settings.doorwayWidth > 2.4f)
            settings.doorwayWidth = 1.9f;
        // Photo 03 uses square posters on the stage flanks.
        if (settings.stageBannerWidth < 1f || settings.stageBannerWidth > 1.8f)
            settings.stageBannerWidth = 1.55f;
        if (Mathf.Abs(settings.stageBannerHeight - settings.stageBannerWidth) > 0.05f)
            settings.stageBannerHeight = settings.stageBannerWidth;
        if (settings.warmLightKelvin < 2700f || settings.warmLightKelvin > 3200f)
            settings.warmLightKelvin = 3000f;
        // Migrate older charcoal floor tint to medium grey.
        if (settings.floorBaseColor.r < 0.4f)
            settings.floorBaseColor = new Color(0.54f, 0.55f, 0.56f, 1f);
        if (settings.wallFanDiameter < 0.55f)
            settings.wallFanDiameter = 0.62f;
        Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Auditorium/Logo/Navrachana_Rear_Logo.png");
        if (logo == null) logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Architecture/NavrachanaRearLogoCrop.png");
        if (logo == null) logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/NavrachanaLogoReferenceCrop.png");
        if (settings.rearLogoArtwork == null) settings.rearLogoArtwork = logo;
        settings.stageBannerLeftArtwork = LoadPreferredStagePoster(
            "Assets/Textures/Auditorium/Banners/Navrachana_Stage_Poster_Left.png",
            "Assets/Textures/Auditorium/Banners/Navrachana_Stage_Banner_Left.png",
            "Assets/Textures/Architecture/StageBannerLeft.png",
            settings.stageBannerLeftArtwork);
        settings.stageBannerRightArtwork = LoadPreferredStagePoster(
            "Assets/Textures/Auditorium/Banners/Navrachana_Stage_Poster_Right.png",
            "Assets/Textures/Auditorium/Banners/Navrachana_Stage_Banner_Right.png",
            "Assets/Textures/Architecture/StageBannerRight.png",
            settings.stageBannerRightArtwork);

        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static AuditoriumMaterialLibrary EnsureMaterials()
    {
        AuditoriumMaterialLibrary library = AssetDatabase.LoadAssetAtPath<AuditoriumMaterialLibrary>(MaterialLibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<AuditoriumMaterialLibrary>();
            AssetDatabase.CreateAsset(library, MaterialLibraryPath);
        }

        library.floor = EnsureMaterial("VA_Floor", new Color(0.54f, 0.55f, 0.56f), 0.16f, 0.02f);
        library.aisle = EnsureMaterial("VA_Aisle", new Color(0.105f, 0.12f, 0.17f), 0.34f, 0f);
        library.wall = EnsureMaterial("VA_Wall", new Color(0.78f, 0.74f, 0.66f), 0.22f, 0f);
        library.ceiling = EnsureMaterial("VA_Ceiling", new Color(0.84f, 0.82f, 0.76f), 0.18f, 0f);
        library.acousticPanel = EnsureMaterial("VA_AcousticPanel", new Color(0.10f, 0.14f, 0.19f), 0.38f, 0f);
        library.trim = EnsureMaterial("VA_Trim", new Color(0.43f, 0.31f, 0.18f), 0.42f, 0.15f);
        library.column = EnsureMaterial("VA_Column", new Color(0.86f, 0.83f, 0.76f), 0.2f, 0f);
        library.stageWood = EnsureMaterial("VA_StageWood", new Color(0.42f, 0.28f, 0.16f), 0.34f, 0.04f);
        library.stageTop = EnsureMaterial("VA_StageTop", new Color(0.50f, 0.34f, 0.20f), 0.34f, 0f);
        library.backdrop = EnsureMaterial("VA_Backdrop", new Color(0.075f, 0.095f, 0.13f), 0.35f, 0f);
        library.accent = EnsureMaterial("VA_Accent", new Color(0.66f, 0.46f, 0.22f), 0.36f, 0.3f);
        library.door = EnsureMaterial("VA_Door", new Color(0.12f, 0.12f, 0.13f), 0.28f, 0.05f);
        library.benchWood = EnsureMaterial("VA_BenchWood", new Color(0.43f, 0.28f, 0.16f), 0.4f, 0.02f);
        library.furnitureMetal = EnsureMaterial("VA_FurnitureMetal", new Color(0.18f, 0.22f, 0.25f), 0.52f, 0.32f);
        library.tableTop = EnsureMaterial("VA_TableTop", new Color(0.39f, 0.27f, 0.17f), 0.36f, 0.02f);
        library.flowerPot = EnsureMaterial("VA_FlowerPot", new Color(0.55f, 0.25f, 0.16f), 0.35f, 0f);
        library.furnitureSoil = EnsureMaterial("VA_FurnitureSoil", new Color(0.12f, 0.085f, 0.055f), 0.25f, 0f);
        library.foliage = EnsureMaterial("VA_Foliage", new Color(0.16f, 0.35f, 0.19f), 0.3f, 0f);
        library.classroomDeskBlue = EnsureMaterial("VA_ClassroomDeskBlue", new Color(0.035f, 0.31f, 0.56f), 0.36f, 0.08f);
        library.classroomDeskTop = EnsureMaterial("VA_ClassroomDeskTop", new Color(0.66f, 0.68f, 0.67f), 0.25f, 0.02f);
        library.classroomPerforation = EnsureMaterial("VA_ClassroomPerforation", new Color(0.02f, 0.19f, 0.36f), 0.25f, 0f);
        library.chairBlue = EnsureMaterial("VA_ChairBlue", new Color(0.055f, 0.34f, 0.70f), 0.3f, 0.04f);
        library.chairMetal = EnsureMaterial("VA_ChairMetal", new Color(0.63f, 0.68f, 0.70f), 0.62f, 0.55f);

        Texture2D grain = EnsureWoodGrainTexture();
        Texture2D honeyTex = LoadArchitectureTexture("Wood/HoneyOak.png", 1024)
            ?? LoadArchitectureTexture("WoodHoneyAlbedo.png", 1024);
        Texture2D walnutTex = LoadArchitectureTexture("Wood/Walnut.png", 1024)
            ?? LoadArchitectureTexture("WoodWalnutAlbedo.png", 1024);
        Texture2D stageTex = LoadArchitectureTexture("Wood/StageTimber.png", 1024)
            ?? LoadArchitectureTexture("StageTimberAlbedo.png", 1024);
        Texture2D stoneTex = LoadArchitectureTexture("Stone/UniformGreyStone.png", 1024)
            ?? LoadArchitectureTexture("StoneTileAlbedo.png", 1024);
        Texture2D fabricTex = LoadArchitectureTexture("Fabric/GreyAcousticFabric.png", 512)
            ?? LoadArchitectureTexture("AcousticFabricAlbedo.png", 512);
        Texture2D blindTex = LoadArchitectureTexture("Fabric/BeigeBlindFabric.png", 512)
            ?? LoadArchitectureTexture("RollerBlindFabric.png", 512);
        Texture2D logo = LoadArchitectureTexture("Logo/Navrachana_Rear_Logo.png", 2048)
            ?? LoadArchitectureTexture("NavrachanaRearLogoCrop.png", 2048);
        if (logo == null) logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/NavrachanaLogoReferenceCrop.png");
        Texture2D bannerLeft = LoadArchitectureTexture("Banners/Navrachana_Stage_Poster_Left.png", 2048)
            ?? LoadArchitectureTexture("Banners/Navrachana_Stage_Banner_Left.png", 2048)
            ?? LoadArchitectureTexture("StageBannerLeft.png", 2048);
        Texture2D bannerRight = LoadArchitectureTexture("Banners/Navrachana_Stage_Poster_Right.png", 2048)
            ?? LoadArchitectureTexture("Banners/Navrachana_Stage_Banner_Right.png", 2048)
            ?? LoadArchitectureTexture("StageBannerRight.png", 2048);

        library.woodHoney = EnsureMaterial("VA_WoodHoney", new Color(0.78f, 0.52f, 0.28f), 0.26f, 0f);
        library.woodHoneyLight = EnsureMaterial("VA_WoodHoneyLight", new Color(0.86f, 0.62f, 0.36f), 0.26f, 0f);
        library.woodHoneyDark = EnsureMaterial("VA_WoodHoneyDark", new Color(0.62f, 0.38f, 0.20f), 0.26f, 0f);
        library.woodWalnut = EnsureMaterial("VA_WoodWalnut", new Color(0.28f, 0.16f, 0.10f), 0.30f, 0.02f);
        library.woodWalnutLight = EnsureMaterial("VA_WoodWalnutLight", new Color(0.38f, 0.23f, 0.14f), 0.30f, 0.02f);
        library.stageTimber = EnsureMaterial("VA_StageTimber", new Color(0.62f, 0.42f, 0.24f), 0.30f, 0f);
        library.wallPlaster = EnsureMaterial("VA_WallPlaster", new Color(0.90f, 0.87f, 0.80f), 0.14f, 0f);
        // Uniform medium grey stone — light/dark aliases match to kill patchy floors.
        library.stoneTile = EnsureMaterial("VA_StoneTile", new Color(0.54f, 0.55f, 0.56f), 0.14f, 0f);
        library.stoneTileLight = EnsureMaterial("VA_StoneTileLight", new Color(0.54f, 0.55f, 0.56f), 0.14f, 0f);
        library.stoneTileDark = EnsureMaterial("VA_StoneTileDark", new Color(0.52f, 0.53f, 0.54f), 0.14f, 0f);
        library.structuralMetal = EnsureMaterial("VA_StructuralMetal", new Color(0.08f, 0.09f, 0.10f), 0.34f, 0.45f);
        library.acousticPanelLight = EnsureMaterial("VA_AcousticPanelLight", new Color(0.84f, 0.83f, 0.78f), 0.10f, 0f);
        library.acousticPanelAlternate = EnsureMaterial("VA_AcousticPanelAlternate", new Color(0.78f, 0.78f, 0.74f), 0.11f, 0f);
        library.acousticFabric = EnsureMaterial("VA_AcousticFabric", new Color(0.52f, 0.53f, 0.53f), 0.06f, 0f);
        library.projectionScreen = EnsureMaterial("VA_ProjectionScreen", new Color(0.93f, 0.93f, 0.91f), 0.05f, 0f);
        // Distinct beige fabric — warmer and darker than cream plaster so blinds read clearly.
        library.rollerBlind = EnsureMaterial("VA_RollerBlind", new Color(0.78f, 0.70f, 0.56f), 0.07f, 0f);
        library.speakerFinish = EnsureMaterial("VA_SpeakerFinish", new Color(0.05f, 0.05f, 0.055f), 0.22f, 0.05f);
        library.speakerGrille = EnsureMaterial("VA_SpeakerGrille", new Color(0.08f, 0.08f, 0.09f), 0.18f, 0.08f);
        library.fixtureFinish = EnsureMaterial("VA_FixtureFinish", new Color(0.18f, 0.18f, 0.18f), 0.35f, 0.4f);
        library.fixtureEmissive = EnsureEmissiveMaterial("VA_FixtureEmissive", new Color(1f, 0.92f, 0.78f), 2.2f);
        library.bannerOrange = EnsureMaterial("VA_BannerOrange", new Color(0.95f, 0.31f, 0.055f), 0.10f, 0f);
        library.bannerMagenta = EnsureMaterial("VA_BannerMagenta", new Color(0.62f, 0.08f, 0.30f), 0.10f, 0f);
        library.bannerArtworkMaterial = EnsureUnlitTextureMaterial("VA_BannerArtwork", Color.white);
        library.bannerLeftMaterial = EnsureUnlitTextureMaterial("VA_BannerLeft", Color.white);
        library.bannerRightMaterial = EnsureUnlitTextureMaterial("VA_BannerRight", Color.white);
        library.rearLogoMaterial = EnsureUnlitTextureMaterial("VA_RearLogoArtwork", Color.white);
        library.bannerRed = EnsureUnlitTextureMaterial("VA_BannerRed", new Color(0.82f, 0.08f, 0.14f));
        library.podiumWood = EnsureMaterial("VA_PodiumWood", new Color(0.26f, 0.15f, 0.09f), 0.28f, 0.02f);
        library.doorLeaf = EnsureMaterial("VA_DoorLeaf", new Color(0.16f, 0.09f, 0.055f), 0.28f, 0.04f);
        library.doorGlass = EnsureMaterial("VA_DoorGlass", new Color(0.55f, 0.62f, 0.68f, 0.35f), 0.08f, 0.02f);
        ConfigureTransparent(library.doorGlass);
        library.fanMetal = EnsureMaterial("VA_FanMetal", new Color(0.55f, 0.56f, 0.58f), 0.45f, 0.65f);
        library.fanBlade = EnsureMaterial("VA_FanBlade", new Color(0.72f, 0.73f, 0.74f), 0.35f, 0.25f);

        AssignTexture(library.woodHoney, honeyTex != null ? honeyTex : grain, new Vector2(1.6f, 2.4f));
        AssignTexture(library.woodHoneyLight, honeyTex != null ? honeyTex : grain, new Vector2(1.6f, 2.4f));
        AssignTexture(library.woodHoneyDark, honeyTex != null ? honeyTex : grain, new Vector2(1.6f, 2.4f));
        AssignTexture(library.woodWalnut, walnutTex != null ? walnutTex : grain, new Vector2(1.2f, 1.8f));
        AssignTexture(library.woodWalnutLight, walnutTex != null ? walnutTex : grain, new Vector2(1.2f, 1.8f));
        AssignTexture(library.doorLeaf, walnutTex != null ? walnutTex : grain, new Vector2(0.9f, 1.7f));
        AssignTexture(library.stageTimber, stageTex != null ? stageTex : grain, new Vector2(1.8f, 2.6f));
        AssignTexture(library.podiumWood, walnutTex != null ? walnutTex : grain, new Vector2(1.3f, 1.9f));
        AssignTexture(library.stoneTile, stoneTex, Vector2.one);
        AssignTexture(library.stoneTileLight, stoneTex, Vector2.one);
        AssignTexture(library.stoneTileDark, stoneTex, Vector2.one);
        AssignTexture(library.floor, stoneTex, new Vector2(8f, 12f));
        AssignTexture(library.acousticFabric, fabricTex, new Vector2(3f, 3f));
        AssignTexture(library.rollerBlind, blindTex, new Vector2(2f, 2.5f));

        library.rearLogoArtwork = logo;
        library.stageBannerLeftArtwork = bannerLeft;
        library.stageBannerRightArtwork = bannerRight;
        library.stageBannerArtwork = bannerLeft != null ? bannerLeft : library.stageBannerArtwork;
        AssignTexture(library.rearLogoMaterial, library.rearLogoArtwork, Vector2.one);
        AssignTexture(library.bannerLeftMaterial, bannerLeft != null ? bannerLeft : library.stageBannerArtwork, Vector2.one);
        AssignTexture(library.bannerRightMaterial, bannerRight != null ? bannerRight : library.stageBannerArtwork, Vector2.one);
        AssignTexture(library.bannerArtworkMaterial, library.stageBannerArtwork, Vector2.one);
        EditorUtility.SetDirty(library);
        return library;
    }

    private static void ApplyArtworkSlots(AuditoriumDemoSettings settings, AuditoriumMaterialLibrary materials)
    {
        if (settings.rearLogoArtwork != null) materials.rearLogoArtwork = settings.rearLogoArtwork;
        if (settings.stageBannerArtwork != null) materials.stageBannerArtwork = settings.stageBannerArtwork;
        if (settings.stageBannerLeftArtwork != null) materials.stageBannerLeftArtwork = settings.stageBannerLeftArtwork;
        if (settings.stageBannerRightArtwork != null) materials.stageBannerRightArtwork = settings.stageBannerRightArtwork;

        AssignTexture(materials.rearLogoMaterial, materials.rearLogoArtwork, Vector2.one);
        AssignTexture(materials.bannerArtworkMaterial, materials.stageBannerArtwork, Vector2.one);
        AssignTexture(materials.bannerLeftMaterial,
            materials.stageBannerLeftArtwork != null ? materials.stageBannerLeftArtwork : materials.stageBannerArtwork,
            Vector2.one);
        AssignTexture(materials.bannerRightMaterial,
            materials.stageBannerRightArtwork != null ? materials.stageBannerRightArtwork : materials.stageBannerArtwork,
            Vector2.one);

        if (settings.floorBaseColor.a > 0f && materials.stoneTile != null)
        {
            materials.stoneTile.color = settings.floorBaseColor;
            if (materials.stoneTileLight != null) materials.stoneTileLight.color = settings.floorBaseColor;
            if (materials.stoneTileDark != null) materials.stoneTileDark.color = settings.floorBaseColor * 0.96f;
            if (materials.floor != null) materials.floor.color = settings.floorBaseColor;
        }
    }

    private static Texture2D LoadPreferredStagePoster(string preferredPath, string fallbackPath,
        string legacyPath, Texture2D current)
    {
        Texture2D preferred = AssetDatabase.LoadAssetAtPath<Texture2D>(preferredPath);
        if (preferred != null)
        {
            ConfigureTextureImport(preferredPath, 2048);
            return preferred;
        }
        if (current != null) return current;
        Texture2D fallback = AssetDatabase.LoadAssetAtPath<Texture2D>(fallbackPath);
        if (fallback != null) return fallback;
        return AssetDatabase.LoadAssetAtPath<Texture2D>(legacyPath);
    }

    private static Material EnsureUnlitTextureMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Unlit/Texture");
        if (unlit == null) unlit = Shader.Find("Universal Render Pipeline/Lit");
        if (material == null)
        {
            if (unlit == null)
                throw new InvalidOperationException("No Unlit/Lit shader available for poster materials.");
            material = new Material(unlit);
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }
        else if (unlit != null && material.shader != unlit)
            material.shader = unlit;

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AssignTexture(Material material, Texture2D texture, Vector2 scale)
    {
        if (material == null || texture == null) return;
        material.mainTexture = texture;
        material.mainTextureScale = scale;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", scale);
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", scale);
        }
        EditorUtility.SetDirty(material);
    }

    private static Texture2D LoadArchitectureTexture(string fileName, int maxSize)
    {
        string organized = "Assets/Textures/Auditorium/" + fileName;
        if (File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, organized)))
        {
            ConfigureTextureImport(organized, maxSize);
            Texture2D organizedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(organized);
            if (organizedTex != null) return organizedTex;
        }

        string legacy = "Assets/Textures/Architecture/" + fileName;
        ConfigureTextureImport(legacy, maxSize);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(legacy);
    }

    private static void ConfigureTextureImport(string assetPath, int maxSize)
    {
        string absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        if (!File.Exists(absolutePath)) return;
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        bool dirty = false;
        if (importer.maxTextureSize < maxSize) { importer.maxTextureSize = maxSize; dirty = true; }
        if (importer.anisoLevel < 8) { importer.anisoLevel = 8; dirty = true; }
        if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; dirty = true; }
        if (importer.filterMode != FilterMode.Trilinear) { importer.filterMode = FilterMode.Trilinear; dirty = true; }
        if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            dirty = true;
        }
        if (dirty) importer.SaveAndReimport();
    }

    private static Material EnsureEmissiveMaterial(string name, Color color, float intensity)
    {
        Material material = EnsureMaterial(name, color, 0.2f, 0f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.SetColor("_EmissionColor", color * intensity);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureTransparent(Material material)
    {
        if (material == null) return;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(material);
    }

    private static Texture2D EnsureWoodGrainTexture()
    {
        const string path = "Assets/Materials/VA_WoodGrain.asset";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(512, 256, TextureFormat.RGBA32, true, false);
            texture.name = "VA_WoodGrain";
            AssetDatabase.CreateAsset(texture, path);
        }
        else if (texture.width < 512 || texture.height < 256 || texture.mipmapCount < 2)
        {
            if (!texture.isReadable || !texture.Reinitialize(512, 256, TextureFormat.RGBA32, true))
            {
                Debug.LogWarning("The generated wood texture could not be upgraded because it is not readable.");
                return texture;
            }
            texture.name = "VA_WoodGrain";
        }
        if (!texture.isReadable) return texture;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            float grain = Mathf.Sin(x * 0.19f + Mathf.Sin(y * 0.21f) * 1.35f) * 0.055f
                + Mathf.Sin(x * 0.052f + y * 0.11f) * 0.035f;
            float value = Mathf.Clamp01(0.78f + grain);
            texture.SetPixel(x, y, new Color(value, value * 0.99f, value * 0.96f, 1f));
        }
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static Material EnsureMaterial(string name, Color color, float smoothness, float metallic)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP Lit shader is unavailable. Confirm the Universal Render Pipeline package is installed.");
            }

            material = new Material(shader);
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureUrpSetup()
    {
        UniversalRenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
        {
            pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        }

        if (pipeline == null)
        {
            UnityEngine.Object previousSelection = Selection.activeObject;
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Settings");
            bool created = EditorApplication.ExecuteMenuItem("Assets/Create/Rendering/URP Asset (with Universal Renderer)");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (created)
            {
                pipeline = Selection.activeObject as UniversalRenderPipelineAsset;
            }

            if (pipeline == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets/Settings" });
                if (guids.Length > 0)
                {
                    string generatedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(generatedPath);
                    if (pipeline != null && generatedPath != PipelinePath)
                    {
                        string moveError = AssetDatabase.MoveAsset(generatedPath, PipelinePath);
                        if (!string.IsNullOrEmpty(moveError)) Debug.LogWarning(moveError);
                    }
                }
            }

            if (pipeline == null)
            {
                Selection.activeObject = previousSelection;
                EditorUtility.DisplayDialog(
                    "URP asset required",
                    "Unity could not create the URP asset automatically. Create Assets > Create > Rendering > URP Asset (with Universal Renderer) in Assets/Settings, then run Build Demo Scene again.",
                    "OK");
                throw new InvalidOperationException("A Universal Render Pipeline asset is required to build the demo scene.");
            }
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        SerializedObject pipelineSettings = new SerializedObject(pipeline);
        SerializedProperty msaaSamples = pipelineSettings.FindProperty("m_MSAA");
        SerializedProperty renderScale = pipelineSettings.FindProperty("m_RenderScale");
        if (msaaSamples != null) msaaSamples.intValue = 4;
        if (renderScale != null) renderScale.floatValue = 1f;
        pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();
    }

    private static void RegisterSceneInBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> normalizedScenes = new List<EditorBuildSettingsScene>();
        bool registered = false;
        for (int i = 0; i < currentScenes.Length; i++)
        {
            if (currentScenes[i].path == scenePath)
            {
                if (registered) continue;
                normalizedScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                registered = true;
            }
            else
            {
                normalizedScenes.Add(currentScenes[i]);
            }
        }

        if (!registered) normalizedScenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = normalizedScenes.ToArray();
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folder = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void SelectEnvironmentRoot()
    {
        GameObject environment = GameObject.Find("Environment");
        if (environment != null)
        {
            Selection.activeGameObject = environment;
        }
    }

    private static void FrameDemoSceneInSceneView(AuditoriumDemoSettings settings)
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
        {
            return;
        }

        // The generated ceiling starts inactive in Edit mode so the complete
        // interior is visible while framing. It is enabled on Play in Walkthrough.
        view.LookAt(new Vector3(0f, 1.2f, 0f), Quaternion.Euler(38f, -24f, 0f), settings.hallDepth * 0.9f);
        view.Repaint();
    }
}
