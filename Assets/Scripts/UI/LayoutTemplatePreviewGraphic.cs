using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Renders a top-down, aspect-correct schematic from the same data used to apply a template.</summary>
public sealed class LayoutTemplatePreviewGraphic : MaskableGraphic
{
    private static readonly Color FloorColor = new Color(0.69f, 0.70f, 0.68f, 1f);
    private static readonly Color RoomBorderColor = new Color(0.19f, 0.23f, 0.25f, 1f);
    private static readonly Color AisleColor = new Color(0.28f, 0.44f, 0.43f, 0.70f);
    private static readonly Color ClearanceColor = new Color(0.83f, 0.82f, 0.75f, 0.75f);
    private static readonly Color StageColor = new Color(0.48f, 0.29f, 0.17f, 1f);
    private static readonly Color ChairColor = new Color(0.13f, 0.37f, 0.56f, 1f);
    private static readonly Color ChairBackColor = new Color(0.07f, 0.22f, 0.35f, 1f);
    private static readonly Color TableColor = new Color(0.61f, 0.40f, 0.22f, 1f);
    private static readonly Color DeskColor = new Color(0.78f, 0.58f, 0.31f, 1f);
    private static readonly Color DecorColor = new Color(0.20f, 0.46f, 0.34f, 1f);
    private static readonly Color AvColor = new Color(0.20f, 0.23f, 0.26f, 1f);
    private static readonly Color ErrorColor = new Color(0.66f, 0.12f, 0.10f, 1f);

    private const int CircleSegments = 16;
    private const float FrameThickness = 0.14f;

    private LayoutTemplateDefinition template;
    private float hallWidth;
    private float hallDepth;
    private float stageWidth;
    private float stageDepth;
    private float centralAisleWidth;
    private float sideAisleWidth;
    private float frontClearance;
    private float rearClearance;
    private Vector3 stageCenter;
    private Vector3 stageForward = Vector3.forward;
    private Vector3 stageRight = Vector3.right;
    private bool isConfigured;
    private bool errorLogged;

    public string DiagnosticMessage { get; private set; }
    public bool IsConfigured { get { return isConfigured; } }
    public event Action<string> DiagnosticChanged;

    public void Configure(LayoutTemplateDefinition definition, AuditoriumDemoSettings settings,
        Transform auditoriumRoot, Transform stageTransform)
    {
        template = definition;
        isConfigured = false;
        SetDiagnostic(null);
        errorLogged = false;
        raycastTarget = false;
        maskable = true;
        color = Color.white;

        CanvasRenderer renderer = canvasRenderer;
        if (renderer == null) renderer = gameObject.AddComponent<CanvasRenderer>();
        renderer.cullTransparentMesh = false;
        material = defaultGraphicMaterial;

        if (definition == null)
            DiagnosticMessage = "Preview unavailable: template data is missing.";
        else if (definition.Placements == null)
            DiagnosticMessage = "Preview unavailable: template placements are missing.";
        else if (definition.Placements.Length == 0)
            DiagnosticMessage = "Preview unavailable: this template contains no furniture.";
        else if (settings == null || auditoriumRoot == null || stageTransform == null)
            DiagnosticMessage = "Preview unavailable: auditorium or stage references are missing.";
        else if (settings.hallWidth <= 0f || settings.hallDepth <= 0f
            || settings.stageWidth <= 0f || settings.stageDepth <= 0f)
            DiagnosticMessage = "Preview unavailable: room or stage dimensions are invalid.";

        if (DiagnosticMessage == null)
        {
            hallWidth = settings.hallWidth;
            hallDepth = settings.hallDepth;
            stageWidth = settings.stageWidth;
            stageDepth = settings.stageDepth;
            centralAisleWidth = Mathf.Max(0f, settings.templateCentralAisleWidth);
            sideAisleWidth = Mathf.Max(0f, settings.templateSideAisleClearance);
            frontClearance = Mathf.Max(0f, settings.templateFrontClearance);
            rearClearance = Mathf.Max(0f, settings.templateRearClearance);
            stageCenter = auditoriumRoot.InverseTransformPoint(stageTransform.position);
            stageForward = auditoriumRoot.InverseTransformDirection(stageTransform.forward);
            stageForward.y = 0f;
            if (stageForward.sqrMagnitude < 0.001f)
                DiagnosticMessage = "Preview unavailable: stage orientation is invalid.";
            else
            {
                stageForward.Normalize();
                stageRight = Vector3.Cross(Vector3.up, stageForward).normalized;
                for (int i = 0; i < definition.Placements.Length; i++)
                {
                    try { FurnitureCatalog.Get(definition.Placements[i].furnitureType); }
                    catch (ArgumentOutOfRangeException)
                    {
                        DiagnosticMessage = "Preview unavailable: an item uses an unregistered furniture type.";
                        break;
                    }
                }
            }
        }

        if (DiagnosticMessage == null) isConfigured = true;
        else LogDiagnostic(DiagnosticMessage);
        SetAllDirty();
    }

    /// <summary>Maps an auditorium-local point to this Graphic's local coordinates for labels.</summary>
    public bool TryMapRoomPoint(float localX, float localZ, out Vector2 point)
    {
        Rect rect = GetPixelAdjustedRect();
        if (!isConfigured || rect.width <= 1f || rect.height <= 1f)
        {
            point = Vector2.zero;
            return false;
        }

        float scale = Mathf.Min(rect.width / hallWidth, rect.height / hallDepth);
        point = new Vector2((rect.xMin + rect.xMax) * 0.5f + localX * scale,
            (rect.yMin + rect.yMax) * 0.5f + localZ * scale);
        return true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        if (canvasRenderer != null) canvasRenderer.cullTransparentMesh = false;
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 1f || rect.height <= 1f)
        {
            SetDiagnostic("Preview unavailable: the preview area has no drawable size.");
            LogDiagnostic(DiagnosticMessage);
            DrawFallback(helper, rect);
            return;
        }
        if (!isConfigured)
        {
            DrawFallback(helper, rect);
            return;
        }

        float scale = Mathf.Min(rect.width / hallWidth, rect.height / hallDepth);
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
        {
            SetDiagnostic("Preview unavailable: room dimensions cannot be mapped to the preview.");
            isConfigured = false;
            LogDiagnostic(DiagnosticMessage);
            DrawFallback(helper, rect);
            return;
        }

        try
        {
            // The fixed aspect scale preserves the 16:25 room proportions at every UI resolution.
            AddRect(helper, rect, 0f, 0f, hallWidth, hallDepth, 0f, FloorColor);
            DrawCirculation(helper, rect);
            AddRect(helper, rect, stageCenter.x, stageCenter.z, stageWidth, stageDepth,
                Yaw(stageForward), StageColor);
            DrawFurniture(helper, rect);
            DrawRoomFrame(helper, rect);
        }
        catch (Exception exception)
        {
            SetDiagnostic("Preview rendering failed for '" + template.Id + "': " + exception.Message);
            isConfigured = false;
            LogDiagnostic(DiagnosticMessage);
            helper.Clear();
            DrawFallback(helper, rect);
        }
    }

    private void DrawCirculation(VertexHelper helper, Rect rect)
    {
        float distanceToRearEdge = DistanceToRoomEdge(-stageForward);
        float rearEndDistance = Mathf.Max(0f, distanceToRearEdge - rearClearance);
        float frontDistance = stageDepth * 0.5f + frontClearance;
        float aisleLength = Mathf.Max(0f, rearEndDistance - frontDistance);
        Vector3 aisleCenter = stageCenter - stageForward * (frontDistance + aisleLength * 0.5f);
        if (centralAisleWidth > 0f && aisleLength > 0f)
            AddRect(helper, rect, aisleCenter.x, aisleCenter.z, centralAisleWidth, aisleLength,
                Yaw(stageForward), AisleColor);

        if (sideAisleWidth > 0f && aisleLength > 0f)
        {
            float sideOffset = Mathf.Max(0f, hallWidth * 0.5f - sideAisleWidth * 0.5f);
            Vector3 leftCenter = aisleCenter - stageRight * sideOffset;
            Vector3 rightCenter = aisleCenter + stageRight * sideOffset;
            AddRect(helper, rect, leftCenter.x, leftCenter.z, sideAisleWidth, aisleLength,
                Yaw(stageForward), new Color(AisleColor.r, AisleColor.g, AisleColor.b, 0.42f));
            AddRect(helper, rect, rightCenter.x, rightCenter.z, sideAisleWidth, aisleLength,
                Yaw(stageForward), new Color(AisleColor.r, AisleColor.g, AisleColor.b, 0.42f));
        }

        if (frontClearance > 0f)
        {
            Vector3 frontCenter = stageCenter - stageForward * (stageDepth * 0.5f + frontClearance * 0.5f);
            AddRect(helper, rect, frontCenter.x, frontCenter.z, hallWidth, frontClearance,
                Yaw(stageForward), ClearanceColor);
        }
        if (rearClearance > 0f)
        {
            Vector3 rearCenter = -stageForward * (distanceToRearEdge - rearClearance * 0.5f);
            AddRect(helper, rect, rearCenter.x, rearCenter.z, hallWidth, rearClearance,
                Yaw(stageForward), ClearanceColor);
        }
    }

    private void DrawFurniture(VertexHelper helper, Rect rect)
    {
        for (int i = 0; i < template.Placements.Length; i++)
        {
            TemplateFurniturePlacement placement = template.Placements[i];
            FurnitureDefinition definition = FurnitureCatalog.Get(placement.furnitureType);
            Vector3 dimensions = Vector3.Scale(definition.Dimensions, placement.scale);
            float width = Mathf.Max(0.12f, dimensions.x);
            float depth = Mathf.Max(0.12f, dimensions.z);
            float yaw = placement.localYawDegrees;
            Color itemColor = GetFurnitureColor(placement.furnitureType);

            if (placement.furnitureType == FurnitureType.RoundTable
                || placement.furnitureType == FurnitureType.FlowerPot
                || placement.furnitureType == FurnitureType.MicrophoneStand)
            {
                AddCircle(helper, rect, placement.auditoriumLocalPosition.x,
                    placement.auditoriumLocalPosition.z, Mathf.Min(width, depth) * 0.5f, itemColor);
                if (placement.furnitureType == FurnitureType.MicrophoneStand)
                    AddCircle(helper, rect, placement.auditoriumLocalPosition.x,
                        placement.auditoriumLocalPosition.z, 0.055f, AvColor);
                continue;
            }

            AddRect(helper, rect, placement.auditoriumLocalPosition.x,
                placement.auditoriumLocalPosition.z, width, depth, yaw, itemColor);
            if (IsChair(placement.furnitureType))
            {
                Vector3 facing = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f,
                    Mathf.Cos(yaw * Mathf.Deg2Rad));
                Vector3 backrest = placement.auditoriumLocalPosition - facing * (depth * 0.37f);
                AddRect(helper, rect, backrest.x, backrest.z, width * 0.78f,
                    Mathf.Max(0.07f, depth * 0.13f), yaw, ChairBackColor);
            }
        }
    }

    private void DrawRoomFrame(VertexHelper helper, Rect rect)
    {
        float halfWidth = hallWidth * 0.5f;
        float halfDepth = hallDepth * 0.5f;
        AddRect(helper, rect, 0f, -halfDepth + FrameThickness * 0.5f,
            hallWidth, FrameThickness, 0f, RoomBorderColor);
        AddRect(helper, rect, 0f, halfDepth - FrameThickness * 0.5f,
            hallWidth, FrameThickness, 0f, RoomBorderColor);
        AddRect(helper, rect, -halfWidth + FrameThickness * 0.5f, 0f,
            FrameThickness, hallDepth, 0f, RoomBorderColor);
        AddRect(helper, rect, halfWidth - FrameThickness * 0.5f, 0f,
            FrameThickness, hallDepth, 0f, RoomBorderColor);
    }

    private float DistanceToRoomEdge(Vector3 direction)
    {
        float halfWidth = hallWidth * 0.5f;
        float halfDepth = hallDepth * 0.5f;
        float distanceX = Mathf.Abs(direction.x) > 0.0001f
            ? halfWidth / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float distanceZ = Mathf.Abs(direction.z) > 0.0001f
            ? halfDepth / Mathf.Abs(direction.z) : float.PositiveInfinity;
        return Mathf.Min(distanceX, distanceZ);
    }

    private void AddRect(VertexHelper helper, Rect rect, float centerX, float centerZ,
        float width, float depth, float yaw, Color quadColor)
    {
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        float radians = yaw * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);
        int start = helper.currentVertCount;
        AddVertex(helper, rect, centerX - cosine * halfWidth - sine * halfDepth,
            centerZ + sine * halfWidth - cosine * halfDepth, quadColor);
        AddVertex(helper, rect, centerX - cosine * halfWidth + sine * halfDepth,
            centerZ + sine * halfWidth + cosine * halfDepth, quadColor);
        AddVertex(helper, rect, centerX + cosine * halfWidth + sine * halfDepth,
            centerZ - sine * halfWidth + cosine * halfDepth, quadColor);
        AddVertex(helper, rect, centerX + cosine * halfWidth - sine * halfDepth,
            centerZ - sine * halfWidth - cosine * halfDepth, quadColor);
        helper.AddTriangle(start, start + 1, start + 2);
        helper.AddTriangle(start, start + 2, start + 3);
    }

    private void AddCircle(VertexHelper helper, Rect rect, float centerX, float centerZ,
        float radius, Color circleColor)
    {
        int centerIndex = helper.currentVertCount;
        AddVertex(helper, rect, centerX, centerZ, circleColor);
        for (int i = 0; i <= CircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegments;
            AddVertex(helper, rect, centerX + Mathf.Cos(angle) * radius,
                centerZ + Mathf.Sin(angle) * radius, circleColor);
        }
        for (int i = 0; i < CircleSegments; i++)
            helper.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + i + 2);
    }

    private void AddVertex(VertexHelper helper, Rect rect, float roomX, float roomZ, Color vertexColor)
    {
        float scale = Mathf.Min(rect.width / hallWidth, rect.height / hallDepth);
        Vector3 position = new Vector3((rect.xMin + rect.xMax) * 0.5f + roomX * scale,
            (rect.yMin + rect.yMax) * 0.5f + roomZ * scale, 0f);
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = vertexColor;
        helper.AddVert(vertex);
    }

    private void DrawFallback(VertexHelper helper, Rect rect)
    {
        if (rect.width <= 1f || rect.height <= 1f) return;
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;
        AddRawQuad(helper, -halfWidth, -halfHeight, halfWidth, halfHeight,
            new Color(ErrorColor.r, ErrorColor.g, ErrorColor.b, 0.15f));
        AddRawQuad(helper, -halfWidth, halfHeight - 2f, halfWidth, halfHeight, ErrorColor);
        AddRawQuad(helper, -halfWidth, -halfHeight, halfWidth, -halfHeight + 2f, ErrorColor);
        AddRawQuad(helper, -halfWidth, -halfHeight, -halfWidth + 2f, halfHeight, ErrorColor);
        AddRawQuad(helper, halfWidth - 2f, -halfHeight, halfWidth, halfHeight, ErrorColor);
    }

    private static void AddRawQuad(VertexHelper helper, float left, float bottom,
        float right, float top, Color quadColor)
    {
        int start = helper.currentVertCount;
        AddRawVertex(helper, left, bottom, quadColor);
        AddRawVertex(helper, left, top, quadColor);
        AddRawVertex(helper, right, top, quadColor);
        AddRawVertex(helper, right, bottom, quadColor);
        helper.AddTriangle(start, start + 1, start + 2);
        helper.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddRawVertex(VertexHelper helper, float x, float y, Color vertexColor)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = new Vector3(x, y, 0f);
        vertex.color = vertexColor;
        helper.AddVert(vertex);
    }

    private void LogDiagnostic(string message)
    {
        if (errorLogged || string.IsNullOrEmpty(message)) return;
        errorLogged = true;
        string id = template != null ? template.Id : "<missing template>";
        Debug.LogWarning("Layout template preview '" + id + "': " + message, this);
    }

    private void SetDiagnostic(string message)
    {
        if (DiagnosticMessage == message) return;
        DiagnosticMessage = message;
        if (DiagnosticChanged != null) DiagnosticChanged(message);
    }

    private static bool IsChair(FurnitureType type)
    {
        return type == FurnitureType.Chair || type == FurnitureType.AuditoriumChair
            || type == FurnitureType.LoungeChair;
    }

    private static float Yaw(Vector3 forward)
    {
        return forward.sqrMagnitude < 0.001f
            ? 0f : Quaternion.LookRotation(forward, Vector3.up).eulerAngles.y;
    }

    private static Color GetFurnitureColor(FurnitureType type)
    {
        switch (type)
        {
            case FurnitureType.Bench:
            case FurnitureType.RoundTable:
            case FurnitureType.SquareTable:
                return TableColor;
            case FurnitureType.ClassroomDesk:
            case FurnitureType.RegistrationTable:
                return DeskColor;
            case FurnitureType.Chair:
            case FurnitureType.AuditoriumChair:
            case FurnitureType.LoungeChair:
                return ChairColor;
            case FurnitureType.FlowerPot:
                return DecorColor;
            case FurnitureType.PaFloorSpeaker:
            case FurnitureType.EventLectern:
            case FurnitureType.MicrophoneStand:
                return AvColor;
            case FurnitureType.RollupBanner:
                return new Color(0.64f, 0.27f, 0.22f, 1f);
            default:
                return ChairColor;
        }
    }
}
