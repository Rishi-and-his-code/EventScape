using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Rebuilds fixed architecture and reuses the scene's existing planner systems.</summary>
public static class AuditoriumBuilder
{
    public static GameObject Build(AuditoriumDemoSettings settings, AuditoriumMaterialLibrary materials)
    {
        if (settings == null) throw new ArgumentNullException("settings");
        if (materials == null) throw new ArgumentNullException("materials");
        if (Application.isPlaying) throw new InvalidOperationException("The auditorium builder is Edit Mode only.");

        GameObject ceiling = AuditoriumArchitectureBuilder.Build(settings, materials);
        CameraModeController cameraModes = UnityEngine.Object.FindFirstObjectByType<CameraModeController>();
        if (cameraModes == null) cameraModes = CreateCameraRigs(settings);
        Camera[] sceneCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneCameras.Length; i++)
            if (sceneCameras[i] != null) sceneCameras[i].allowMSAA = true;
        WalkthroughCameraController walkthroughController =
            UnityEngine.Object.FindFirstObjectByType<WalkthroughCameraController>();
        if (walkthroughController != null)
        {
            walkthroughController.transform.position = GetWalkthroughSpawn(settings);
            walkthroughController.ConfigureDemoMovement();
        }
        cameraModes.ConfigureCeiling(ceiling);
        cameraModes.SetInitialMode(AuditoriumCameraMode.Planning);

        GameObject placedObjects = FindRoot("PlacedObjects");
        if (placedObjects == null) placedObjects = CreateRoot("PlacedObjects");
        GameObject placementRoot = FindRoot("PlacementSystem");
        if (placementRoot == null) placementRoot = CreateRoot("PlacementSystem");
        PlacementSystem placementSystem = placementRoot.GetComponent<PlacementSystem>();
        if (placementSystem == null) placementSystem = placementRoot.AddComponent<PlacementSystem>();
        placementSystem.Configure(cameraModes, settings, materials, placedObjects.transform);
        cameraModes.ConfigurePlacementSystem(placementSystem);

        GameObject persistenceRoot = FindRoot("Persistence");
        if (persistenceRoot == null) persistenceRoot = CreateRoot("Persistence");
        LayoutPersistenceService persistence = persistenceRoot.GetComponent<LayoutPersistenceService>();
        if (persistence == null) persistence = persistenceRoot.AddComponent<LayoutPersistenceService>();
        persistence.Configure(placementSystem);

        LayoutTemplateService templateService = persistenceRoot.GetComponent<LayoutTemplateService>();
        if (templateService == null) templateService = persistenceRoot.AddComponent<LayoutTemplateService>();
        GameObject environmentRoot = FindRoot("Environment");
        GameObject stageObject = GameObject.Find("Auditorium_Stage");
        if (environmentRoot == null || stageObject == null)
            throw new InvalidOperationException("The auditorium environment or stage geometry is missing; templates cannot be configured.");
        templateService.Configure(placementSystem, settings, environmentRoot.transform, stageObject.transform);

        PlannerUIBuilder.RebuildGenerated(cameraModes, placementSystem, persistence, templateService,
            settings, environmentRoot.transform, stageObject.transform);
        PlannerUIBuilder.EnsureReferenceSeatingToggle(AuditoriumArchitectureBuilder.SeatingController);
        EnsureRequiredGeometry(settings, ceiling, materials);
        return ceiling;
    }

    private static CameraModeController CreateCameraRigs(AuditoriumDemoSettings settings)
    {
        GameObject camerasRoot = CreateRoot("Cameras");
        GameObject walkthroughRoot = CreateRoot("WalkthroughRig", camerasRoot.transform);
        walkthroughRoot.transform.position = GetWalkthroughSpawn(settings);
        walkthroughRoot.AddComponent<CharacterController>();
        WalkthroughCameraController walkthroughController = walkthroughRoot.AddComponent<WalkthroughCameraController>();

        GameObject walkthroughCameraObject = new GameObject("WalkthroughCamera");
        walkthroughCameraObject.transform.SetParent(walkthroughRoot.transform, false);
        walkthroughCameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        Camera walkthroughCamera = ConfigureCamera(walkthroughCameraObject, settings);
        walkthroughCameraObject.tag = "MainCamera";
        walkthroughController.Configure(walkthroughCamera, 0f, 0f);

        GameObject planningCameraObject = new GameObject("PlanningCamera");
        planningCameraObject.transform.SetParent(camerasRoot.transform, false);
        Camera planningCamera = ConfigureCamera(planningCameraObject, settings);
        PlanningCameraController planningController = planningCameraObject.AddComponent<PlanningCameraController>();
        planningController.Configure(new Vector3(0f, 1.55f, settings.StageCenterZ * 0.18f), 0f, 58f,
            Mathf.Clamp(Mathf.Max(settings.hallDepth * 1.05f, settings.hallWidth * 1.1f), 13f, 38f), 9f, 40f);

        CameraModeController cameraModes = camerasRoot.AddComponent<CameraModeController>();
        cameraModes.Configure(walkthroughController, planningController, walkthroughCamera, planningCamera,
            AuditoriumCameraMode.Planning);
        return cameraModes;
    }

    private static Camera ConfigureCamera(GameObject cameraObject, AuditoriumDemoSettings settings)
    {
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.20f, 0.23f, 0.26f);
        camera.fieldOfView = 58f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = Mathf.Max(100f, settings.hallDepth * 4f);
        camera.allowHDR = false;
        camera.allowMSAA = true;
        return camera;
    }

    private static Vector3 GetWalkthroughSpawn(AuditoriumDemoSettings settings)
    {
        float maximumX = Mathf.Max(0f, settings.hallWidth * 0.5f - 0.5f);
        float spawnX = Mathf.Clamp(settings.walkthroughSpawnX, -maximumX, maximumX);
        float depthFraction = Mathf.Clamp(settings.walkthroughSpawnDepthFraction, 0.1f, 0.9f);
        return new Vector3(spawnX, 0f, -settings.hallDepth * depthFraction);
    }

    private static GameObject FindRoot(string name)
    {
        GameObject found = GameObject.Find(name);
        return found != null && found.transform.parent == null ? found : null;
    }

    private static GameObject CreateRoot(string name, Transform parent = null)
    {
        GameObject root = new GameObject(name);
        if (parent != null) root.transform.SetParent(parent, false);
        return root;
    }

    private static void EnsureRequiredGeometry(AuditoriumDemoSettings settings, GameObject ceiling,
        AuditoriumMaterialLibrary materials)
    {
        string[] requiredNames =
        {
            "Auditorium_Floor", "Auditorium_Wall_Left", "Auditorium_Wall_Right",
            "Auditorium_Wall_Front", "Auditorium_Wall_Back", "Auditorium_Stage",
            "Stage Step Lower", "Stage Step Upper"
        };
        for (int i = 0; i < requiredNames.Length; i++)
            if (GameObject.Find(requiredNames[i]) == null)
                throw new InvalidOperationException("Auditorium generation failed: required object '" + requiredNames[i] + "' is missing.");

        GameObject floor = GameObject.Find("Auditorium_Floor");
        GameObject stage = GameObject.Find("Auditorium_Stage");
        if (floor.GetComponent<Collider>() == null || floor.GetComponent<PlacementSurface>() == null)
            throw new InvalidOperationException("Auditorium generation failed: the floor needs one collider and placement-surface marker.");
        ValidateCeiling(settings, ceiling, materials);
        if (stage.GetComponent<Collider>() == null || stage.GetComponent<Renderer>() == null
            || stage.GetComponent<PlacementSurface>() == null)
            throw new InvalidOperationException("Auditorium generation failed: stage platform needs a collider, renderer, and placement-surface marker.");
        if (GameObject.Find("Stage Step Lower") == null || GameObject.Find("Stage Step Upper") == null
            || GameObject.Find("Stage Step Lower").GetComponent<Collider>() == null
            || GameObject.Find("Stage Step Upper").GetComponent<Collider>() == null)
            throw new InvalidOperationException("Auditorium generation failed: both collidable stage steps are required for walkthrough access.");

        Vector3 floorSize = floor.transform.localScale;
        if (!Mathf.Approximately(floorSize.x, settings.hallWidth) || !Mathf.Approximately(floorSize.z, settings.hallDepth)
            || !Mathf.Approximately(floor.transform.position.y + floorSize.y * 0.5f, 0f))
            throw new InvalidOperationException("Auditorium generation failed: the floor bounds no longer match the configured clear room dimensions.");
        Vector3 stageSize = stage.transform.localScale;
        if (!Mathf.Approximately(stageSize.x, settings.stageWidth) || !Mathf.Approximately(stageSize.z, settings.stageDepth)
            || !Mathf.Approximately(stage.transform.position.y + stageSize.y * 0.5f, settings.stageHeight)
            || !Mathf.Approximately(stage.transform.position.z, settings.StageCenterZ))
            throw new InvalidOperationException("Auditorium generation failed: stage footprint, top elevation, or position changed.");
    }

    private static void ValidateCeiling(AuditoriumDemoSettings settings, GameObject ceiling,
        AuditoriumMaterialLibrary materials)
    {
        if (ceiling == null)
            throw new InvalidOperationException("Auditorium generation failed: the architecture builder returned no Auditorium_Ceiling root.");
        if (ceiling.name != "Auditorium_Ceiling" || ceiling.transform.parent == null
            || ceiling.transform.parent.name != "Environment" || ceiling.transform.parent.parent != null
            || ceiling.scene != SceneManager.GetActiveScene()
            || (ceiling.transform.localPosition - Vector3.zero).sqrMagnitude > 0.0001f
            || ceiling.transform.localRotation != Quaternion.identity
            || (ceiling.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
            throw new InvalidOperationException("Auditorium generation failed: Auditorium_Ceiling must belong to the active scene under the Environment root.");

        // Planning View intentionally deactivates this complete assembly. Include inactive
        // children in structural checks and leave the visibility state unchanged.
        Transform structure = FindDirectChild(ceiling.transform, "Ceiling Structure");
        Transform panels = FindDirectChild(ceiling.transform, "Acoustic_Panels_and_Grid");
        Transform fittings = FindDirectChild(ceiling.transform, "Ceiling_Light_Fittings");
        if (structure == null || panels == null || fittings == null)
            throw new InvalidOperationException("Auditorium generation failed: the ceiling root is incomplete; structure, acoustic panels/grid, and light fittings are required.");

        MeshFilter structureMesh = structure.GetComponent<MeshFilter>();
        MeshRenderer structureRenderer = structure.GetComponent<MeshRenderer>();
        BoxCollider structureCollider = structure.GetComponent<BoxCollider>();
        if (structureMesh == null || structureMesh.sharedMesh == null || structureMesh.sharedMesh.vertexCount == 0
            || structureRenderer == null || !structureRenderer.enabled || structureRenderer.sharedMaterial == null
            || materials == null || materials.ceiling == null || structureRenderer.sharedMaterial != materials.ceiling
            || structureCollider == null || !structureCollider.enabled)
            throw new InvalidOperationException("Auditorium generation failed: ceiling structure needs a mesh, renderer with the configured ceiling material, and collider.");

        Vector3 expectedStructurePosition = new Vector3(0f, settings.wallHeight + 0.075f, 0f);
        Vector3 expectedStructureScale = new Vector3(settings.hallWidth, 0.15f, settings.hallDepth);
        if ((structure.localPosition - expectedStructurePosition).sqrMagnitude > 0.0001f
            || (structure.localScale - expectedStructureScale).sqrMagnitude > 0.0001f
            || structure.localRotation != Quaternion.identity)
            throw new InvalidOperationException("Auditorium generation failed: ceiling structure elevation or footprint does not match the configured auditorium dimensions.");

        float expectedGeometryBottom = settings.wallHeight - 0.5f;
        float expectedGeometryTop = settings.wallHeight + 0.08f;
        ValidateGeneratedMesh(panels, "acoustic ceiling panels and framework", expectedGeometryBottom, expectedGeometryTop);
        ValidateGeneratedMesh(fittings, "ceiling light fittings", expectedGeometryBottom, expectedGeometryTop);
    }

    private static void ValidateGeneratedMesh(Transform geometryRoot, string description, float minimumY, float maximumY)
    {
        MeshFilter[] meshFilters = geometryRoot.GetComponentsInChildren<MeshFilter>(true);
        MeshRenderer[] renderers = geometryRoot.GetComponentsInChildren<MeshRenderer>(true);
        bool hasMesh = false;
        float lowestVertex = float.PositiveInfinity;
        float highestVertex = float.NegativeInfinity;
        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i] == null || meshFilters[i].sharedMesh == null
                || meshFilters[i].sharedMesh.vertexCount == 0) continue;

            hasMesh = true;
            Bounds localBounds = meshFilters[i].sharedMesh.bounds;
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 localCorner = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                float worldY = meshFilters[i].transform.TransformPoint(localCorner).y;
                lowestVertex = Mathf.Min(lowestVertex, worldY);
                highestVertex = Mathf.Max(highestVertex, worldY);
            }
        }

        bool hasAssignedMaterial = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled) continue;
            Material[] sharedMaterials = renderers[i].sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0) continue;
            bool rendererMaterialsAssigned = true;
            for (int j = 0; j < sharedMaterials.Length; j++)
            {
                if (sharedMaterials[j] == null)
                {
                    rendererMaterialsAssigned = false;
                    break;
                }
            }
            if (rendererMaterialsAssigned)
            {
                hasAssignedMaterial = true;
                break;
            }
        }

        if (!hasMesh || !hasAssignedMaterial)
            throw new InvalidOperationException("Auditorium generation failed: " + description + " are missing a non-empty mesh or assigned materials.");
        if (lowestVertex < minimumY || highestVertex > maximumY)
            throw new InvalidOperationException("Auditorium generation failed: " + description + " are outside the configured ceiling elevation.");
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName) return child;
        }
        return null;
    }
}
