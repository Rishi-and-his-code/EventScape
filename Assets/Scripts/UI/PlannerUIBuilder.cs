using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Creates the lightweight, responsive desktop interface used by the generated demo scene.
/// </summary>
public static class PlannerUIBuilder
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private static readonly Color PanelColor = new Color(0.045f, 0.065f, 0.085f, 0.96f);
    private static readonly Color TextColor = new Color(0.94f, 0.96f, 0.97f, 1f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.78f, 0.82f, 1f);
    private static readonly Color AccentColor = new Color(0.78f, 0.58f, 0.32f, 1f);
    private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.14f);

    public static void Build(CameraModeController cameraModes, PlacementSystem placementSystem,
        LayoutPersistenceService persistenceService, LayoutTemplateService templateService,
        AuditoriumDemoSettings settings, Transform auditoriumRoot, Transform stageTransform)
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Button[] modeButtons = BuildHeader(canvasObject.transform, font);
        if (canvasObject.GetComponent<PlannerUiResponsiveLayout>() == null)
            canvasObject.AddComponent<PlannerUiResponsiveLayout>();
        Button[] catalogButtons = BuildCatalog(canvasObject.transform, font, out FurnitureType[] catalogTypes,
            out Text selectedItem, out Text placementInstructions, out Text selectedObject,
            out Button moveButton, out Button rotateLeftButton, out Button rotateRightButton, out Button deleteButton,
            out Button duplicateButton, out Button undoButton, out Button redoButton,
            out Button gridToggleButton, out Button[] gridIncrementButtons,
            out Button resetCameraButton, out Button topViewButton, out Button frontViewButton, out Button isometricViewButton,
            out Text layoutInfo, out Button finishPlacingButton);
        Text status = BuildHelp(canvasObject.transform, font, out Button[] layoutButtons);
        GameObject walkthroughOverlay = BuildWalkthroughOverlay(canvasObject.transform, font);

        PlannerControlsPanel controls = canvasObject.AddComponent<PlannerControlsPanel>();
        controls.Configure(cameraModes, placementSystem, modeButtons[0], modeButtons[1], catalogButtons, catalogTypes,
            status, selectedItem, placementInstructions, selectedObject, moveButton, rotateLeftButton,
            rotateRightButton, deleteButton, persistenceService, layoutButtons[0], layoutButtons[1], layoutButtons[2]);
        controls.ConfigurePlanningControls(duplicateButton, undoButton, redoButton, gridToggleButton,
            gridIncrementButtons, resetCameraButton, topViewButton, frontViewButton, isometricViewButton, layoutInfo);
        controls.ConfigureLayoutTemplates(templateService);
        controls.ConfigureInteractionMode(finishPlacingButton);
        controls.ConfigureWalkthroughInterface(walkthroughOverlay,
            canvasObject.transform.Find("Furniture Catalog Panel").gameObject,
            canvasObject.transform.Find("Status and Controls").gameObject);
        LayoutTemplateUI templateUi = canvasObject.AddComponent<LayoutTemplateUI>();
        templateUi.Configure(templateService, persistenceService, settings, auditoriumRoot, stageTransform,
            placementSystem, modeButtons[2], modeButtons[3], modeButtons[4]);
        EnsureReferenceSeatingToggle(AuditoriumArchitectureBuilder.SeatingController);
        EnsureResponsiveLayout(canvasObject);
    }

    /// <summary>Repairs CanvasScaler and panel anchors on the existing Interface without rebuilding controls.</summary>
    public static void EnsureResponsiveLayout(GameObject interfaceRoot = null)
    {
        if (interfaceRoot == null) interfaceRoot = GameObject.Find("Interface");
        if (interfaceRoot == null || interfaceRoot.GetComponent<Canvas>() == null) return;

        PlannerUiResponsiveLayout layout = interfaceRoot.GetComponent<PlannerUiResponsiveLayout>();
        if (layout == null) layout = interfaceRoot.AddComponent<PlannerUiResponsiveLayout>();
        EnsureHeaderButtonRow(interfaceRoot.transform.Find("Title and Camera Modes"));
        layout.ApplyLayout(true);
    }

    /// <summary>Rebuilds only the generated interface during an explicit scene regeneration.</summary>
    public static void RebuildGenerated(CameraModeController cameraModes, PlacementSystem placementSystem,
        LayoutPersistenceService persistenceService, LayoutTemplateService templateService,
        AuditoriumDemoSettings settings, Transform auditoriumRoot, Transform stageTransform)
    {
        GameObject existing = GameObject.Find("Interface");
        if (existing != null && existing.GetComponent<PlannerControlsPanel>() != null)
            Object.DestroyImmediate(existing);
        if (GameObject.Find("Interface") == null)
            Build(cameraModes, placementSystem, persistenceService, templateService, settings, auditoriumRoot, stageTransform);
    }

    /// <summary>Repairs template controls on older saved scenes without duplicating the Canvas or EventSystem.</summary>
    public static bool EnsureTemplateEntryPoints(LayoutTemplateService templateService,
        LayoutPersistenceService persistenceService, PlacementSystem placementSystem,
        AuditoriumDemoSettings settings, Transform auditoriumRoot, Transform stageTransform)
    {
        GameObject interfaceRoot = GameObject.Find("Interface");
        if (interfaceRoot == null || interfaceRoot.GetComponent<Canvas>() == null) return false;
        Transform header = interfaceRoot.transform.Find("Title and Camera Modes");
        if (header == null) return false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform buttonRow = EnsureHeaderButtonRow(header);
        Button templatesButton = FindOrCreateTemplateButton(buttonRow, "Layout Templates", "LAYOUT TEMPLATES",
            15, 170f, font);
        Button savedLayoutsButton = FindOrCreateTemplateButton(buttonRow, "My Saved Layouts", "MY SAVED LAYOUTS",
            14, 150f, font);
        Button saveAsButton = FindOrCreateTemplateButton(buttonRow, "Save As My Layout", "SAVE AS MY LAYOUT",
            14, 150f, font);
        if (templatesButton == null || savedLayoutsButton == null || saveAsButton == null) return false;
        EnsureHeaderButtonRow(header);

        LayoutTemplateUI templateUi = interfaceRoot.GetComponent<LayoutTemplateUI>();
        if (templateUi == null) templateUi = interfaceRoot.AddComponent<LayoutTemplateUI>();
        templateUi.Configure(templateService, persistenceService, settings, auditoriumRoot, stageTransform,
            placementSystem, templatesButton, savedLayoutsButton, saveAsButton);
        EnsureResponsiveLayout(interfaceRoot);
        return true;
    }

    private static Button FindOrCreateTemplateButton(Transform parent, string objectName, string label,
        int fontSize, float preferredWidth, Font font)
    {
        Transform existing = parent != null ? parent.Find(objectName) : null;
        if (existing == null && parent != null && parent.parent != null)
            existing = parent.parent.Find(objectName);
        Button button = existing != null ? existing.GetComponent<Button>() : null;
        if (button == null)
        {
            if (existing != null)
            {
                Image image = existing.GetComponent<Image>();
                if (image == null) image = existing.gameObject.AddComponent<Image>();
                button = existing.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
            }
            else button = CreateButton(objectName, parent, font, label, fontSize);
        }
        if (parent != null && button.transform.parent != parent)
            button.transform.SetParent(parent, false);
        AddHeaderLayoutElement(button, preferredWidth);
        Text buttonLabel = button.GetComponentInChildren<Text>(true);
        if (buttonLabel == null)
        {
            buttonLabel = CreateText("Label", button.transform, font, label, fontSize, TextColor, FontStyle.Bold);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = new Vector2(8f, 0f);
            buttonLabel.rectTransform.offsetMax = new Vector2(-8f, 0f);
        }
        else buttonLabel.text = label;
        if (objectName == "Layout Templates") StyleTemplateButton(button);
        return button;
    }

    private static void StyleTemplateButton(Button button)
    {
        if (button == null) return;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.42f, 0.31f, 0.16f, 1f);
        colors.highlightedColor = new Color(0.55f, 0.41f, 0.20f, 1f);
        colors.pressedColor = new Color(0.31f, 0.22f, 0.11f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    /// <summary>Adds the optional seating control to an existing generated UI without rebuilding the UI.</summary>
    public static void EnsureReferenceSeatingToggle(ReferenceSeatingController seatingController)
    {
        GameObject interfaceRoot = GameObject.Find("Interface");
        if (interfaceRoot == null) return;
        Transform content = interfaceRoot.transform.Find("Furniture Catalog Panel/Catalog Viewport/Catalog Content");
        if (content == null) return;
        Button seatingButton = content.Find("Reference Seating Toggle") != null
            ? content.Find("Reference Seating Toggle").GetComponent<Button>() : null;
        if (seatingButton == null)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            seatingButton = CreateButton("Reference Seating Toggle", content, font,
                seatingController != null && seatingController.IsVisible ? "Reference Seating: ON" : "Reference Seating: OFF", 15);
            RectTransform contentRect = content as RectTransform;
            PlaceTopRect(seatingButton.GetComponent<RectTransform>(), 12f,
                Mathf.Max(1540f, contentRect != null ? contentRect.sizeDelta.y - 54f : 1540f), 42f);
        }

        PlannerControlsPanel controls = interfaceRoot.GetComponent<PlannerControlsPanel>();
        if (controls != null) controls.ConfigureReferenceSeating(seatingButton, seatingController);
    }

    private static Button[] BuildHeader(Transform canvas, Font font)
    {
        GameObject header = CreatePanel("Title and Camera Modes", canvas, PanelColor);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 88f);
        rect.anchoredPosition = Vector2.zero;

        GameObject accent = CreateImage("Accent Rule", header.transform, AccentColor);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(1f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.sizeDelta = new Vector2(0f, 2f);

        Text title = CreateText("Application Title", header.transform, font, "VIRTUAL AUDITORIUM", 26, TextColor, FontStyle.Bold);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(20f, -12f);
        titleRect.sizeDelta = new Vector2(400f, 36f);

        Text hint = CreateText("Switch Hint", header.transform, font, "TAB  Switch camera view", 15, MutedTextColor, FontStyle.Normal);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(0f, 1f);
        hintRect.pivot = new Vector2(0f, 1f);
        hintRect.anchoredPosition = new Vector2(22f, -50f);
        hintRect.sizeDelta = new Vector2(260f, 24f);

        RectTransform buttonRow = EnsureHeaderButtonRow(header.transform);
        Button saveAsButton = CreateButton("Save As My Layout", buttonRow, font, "SAVE AS MY LAYOUT", 14);
        Button savedLayoutsButton = CreateButton("My Saved Layouts", buttonRow, font, "MY SAVED LAYOUTS", 14);
        Button templatesButton = CreateButton("Layout Templates", buttonRow, font, "LAYOUT TEMPLATES", 15);
        StyleTemplateButton(templatesButton);
        Button walkthroughButton = CreateButton("Walkthrough Mode", buttonRow, font, "Walkthrough Mode", 16);
        Button planningButton = CreateButton("Planning View", buttonRow, font, "Planning View", 16);
        AddHeaderLayoutElement(saveAsButton, 150f);
        AddHeaderLayoutElement(savedLayoutsButton, 150f);
        AddHeaderLayoutElement(templatesButton, 170f);
        AddHeaderLayoutElement(walkthroughButton, 150f);
        AddHeaderLayoutElement(planningButton, 150f);

        return new[] { walkthroughButton, planningButton, templatesButton, savedLayoutsButton, saveAsButton };
    }

    private static RectTransform EnsureHeaderButtonRow(Transform header)
    {
        if (header == null) return null;
        Transform existing = header.Find("Header Button Row");
        RectTransform row = existing as RectTransform;
        if (row == null)
        {
            GameObject rowObject = new GameObject("Header Button Row", typeof(RectTransform));
            rowObject.transform.SetParent(header, false);
            row = rowObject.GetComponent<RectTransform>();
        }

        // Re-parent any free-floating header action buttons into the row.
        string[] buttonNames =
        {
            "Save As My Layout", "My Saved Layouts", "Layout Templates",
            "Walkthrough Mode", "Planning View"
        };
        for (int i = 0; i < buttonNames.Length; i++)
        {
            Transform button = header.Find(buttonNames[i]);
            if (button != null && button.parent != row)
                button.SetParent(row, false);
            Transform nested = row.Find(buttonNames[i]);
            if (nested != null) nested.SetSiblingIndex(i);
        }

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(0, 0, 0, 0);

        // Prefer a fixed row width from PlannerUiResponsiveLayout over ContentSizeFitter,
        // so header buttons stay inside the Canvas instead of growing past the title.
        ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            if (Application.isPlaying) Object.Destroy(fitter);
            else Object.DestroyImmediate(fitter);
        }

        row.anchorMin = new Vector2(1f, 0.5f);
        row.anchorMax = new Vector2(1f, 0.5f);
        row.pivot = new Vector2(1f, 0.5f);
        row.anchoredPosition = new Vector2(-16f, 0f);
        row.sizeDelta = new Vector2(900f, 52f);
        return row;
    }

    private static void AddHeaderLayoutElement(Button button, float preferredWidth)
    {
        if (button == null) return;
        LayoutElement element = button.GetComponent<LayoutElement>();
        if (element == null) element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.minWidth = preferredWidth * 0.85f;
        element.preferredHeight = 48f;
        element.flexibleWidth = 0f;
    }

    private static Button[] BuildCatalog(Transform canvas, Font font, out FurnitureType[] catalogTypes,
        out Text selectedItem, out Text placementInstructions, out Text selectedObject,
        out Button moveButton, out Button rotateLeftButton, out Button rotateRightButton, out Button deleteButton,
        out Button duplicateButton, out Button undoButton, out Button redoButton,
        out Button gridToggleButton, out Button[] gridIncrementButtons,
        out Button resetCameraButton, out Button topViewButton, out Button frontViewButton, out Button isometricViewButton,
        out Text layoutInfo, out Button finishPlacingButton)
    {
        GameObject panel = CreatePanel("Furniture Catalog Panel", canvas, PanelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        // ~340 px at 1920; PlannerUiResponsiveLayout clamps width for smaller canvases.
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.offsetMin = new Vector2(-352f, 12f);
        panelRect.offsetMax = new Vector2(-12f, -96f);

        RectTransform viewportRect = CreateRect("Catalog Viewport", panel.transform);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-22f, -10f);
        Image viewportImage = viewportRect.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = true;
        viewportRect.gameObject.AddComponent<RectMask2D>();

        RectTransform contentRect = CreateRect("Catalog Content", viewportRect);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 1700f);

        ScrollRect scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 28f;

        Scrollbar scrollbar = BuildScrollbar(panel.transform);
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.verticalScrollbarSpacing = -3f;

        Text heading = CreateText("Catalog Heading", contentRect, font, "FURNITURE CATALOG", 22, AccentColor, FontStyle.Bold);
        PlaceTopRect(heading.rectTransform, 16f, 18f, 34f);

        selectedItem = CreateText("Selected Furniture", contentRect, font, "No item selected", 18, TextColor, FontStyle.Bold);
        PlaceTopRect(selectedItem.rectTransform, 16f, 56f, 28f);

        CreateDivider("Catalog Divider", contentRect, 94f);

        FurnitureDefinition[] definitions = FurnitureCatalog.GetAll();
        catalogTypes = new FurnitureType[definitions.Length];
        Button[] buttons = new Button[definitions.Length];
        float catalogCursor = 108f;
        string activeCategory = null;
        for (int i = 0; i < definitions.Length; i++)
        {
            if (definitions[i].Category != activeCategory)
            {
                activeCategory = definitions[i].Category;
                Text category = CreateText(activeCategory + " Category Heading", contentRect, font,
                    activeCategory, 16, AccentColor, FontStyle.Bold);
                PlaceTopRect(category.rectTransform, 16f, catalogCursor, 26f);
                catalogCursor += 30f;
            }

            string label = definitions[i].DisplayName;
            catalogTypes[i] = definitions[i].Type;
            buttons[i] = CreateButton(label + " Catalog Button", contentRect, font, label, 17);
            PlaceTopRect(buttons[i].GetComponent<RectTransform>(), 12f, catalogCursor, 48f);
            catalogCursor += 56f;
        }

        float sectionOffset = Mathf.Max(0f, catalogCursor + 54f - 350f);
        finishPlacingButton = CreateButton("Finish Placing Button", contentRect, font, "Select / Edit Mode", 16);
        PlaceTopRect(finishPlacingButton.GetComponent<RectTransform>(), 12f, catalogCursor, 44f);

        CreateDivider("Catalog Instructions Divider", contentRect, 350f + sectionOffset);
        placementInstructions = CreateText("Placement Instructions", contentRect, font,
            "Choose an item to place it. Left click confirms repeatedly.\nFinish Placing returns to Select / Edit; Escape cancels the preview.\nQ/E rotates 15°. Scroll for editing, snapping, and history controls.",
            15, MutedTextColor, FontStyle.Normal);
        PlaceTopRect(placementInstructions.rectTransform, 16f, 362f + sectionOffset, 88f);
        placementInstructions.verticalOverflow = VerticalWrapMode.Truncate;

        CreateDivider("Selected Object Divider", contentRect, 468f + sectionOffset);
        Text objectHeading = CreateText("Selected Object Heading", contentRect, font, "SELECTED OBJECT", 20, AccentColor, FontStyle.Bold);
        PlaceTopRect(objectHeading.rectTransform, 16f, 480f + sectionOffset, 28f);

        selectedObject = CreateText("Selected Object Name", contentRect, font, "Click placed furniture to select it", 16, TextColor, FontStyle.Bold);
        PlaceTopRect(selectedObject.rectTransform, 16f, 512f + sectionOffset, 38f);

        moveButton = CreateButton("Move Selected Button", contentRect, font, "Move Selected", 16);
        PlaceTopRect(moveButton.GetComponent<RectTransform>(), 12f, 562f + sectionOffset, 46f);
        rotateLeftButton = CreateButton("Rotate Left Button", contentRect, font, "Rotate Left", 16);
        PlaceTopColumnRect(rotateLeftButton.GetComponent<RectTransform>(), 0f, 0.5f, 618f + sectionOffset, 46f, 12f, 6f);
        rotateRightButton = CreateButton("Rotate Right Button", contentRect, font, "Rotate Right", 16);
        PlaceTopColumnRect(rotateRightButton.GetComponent<RectTransform>(), 0.5f, 1f, 618f + sectionOffset, 46f, 6f, 12f);
        deleteButton = CreateButton("Delete Selected Button", contentRect, font, "Delete Selected", 16);
        PlaceTopRect(deleteButton.GetComponent<RectTransform>(), 12f, 674f + sectionOffset, 46f);

        CreateDivider("Grid Settings Divider", contentRect, 740f + sectionOffset);
        Text gridHeading = CreateText("Grid Settings Heading", contentRect, font, "GRID SNAPPING", 20, AccentColor, FontStyle.Bold);
        PlaceTopRect(gridHeading.rectTransform, 16f, 752f + sectionOffset, 30f);
        gridToggleButton = CreateButton("Grid Snapping Toggle", contentRect, font, "Grid Snap: ON", 16);
        PlaceTopRect(gridToggleButton.GetComponent<RectTransform>(), 12f, 786f + sectionOffset, 44f);
        gridIncrementButtons = new Button[3];
        string[] increments = { "0.25 m", "0.5 m", "1.0 m" };
        for (int i = 0; i < gridIncrementButtons.Length; i++)
        {
            gridIncrementButtons[i] = CreateButton("Grid Increment " + increments[i], contentRect, font, increments[i], 15);
            PlaceTopColumnRect(gridIncrementButtons[i].GetComponent<RectTransform>(), i / 3f, (i + 1) / 3f,
                840f + sectionOffset, 40f, i == 0 ? 12f : 5f, i == 2 ? 12f : 5f);
        }

        duplicateButton = CreateButton("Duplicate Selected Button", contentRect, font, "Duplicate Selected", 16);
        PlaceTopRect(duplicateButton.GetComponent<RectTransform>(), 12f, 892f + sectionOffset, 44f);
        undoButton = CreateButton("Undo Layout Button", contentRect, font, "Undo", 16);
        PlaceTopColumnRect(undoButton.GetComponent<RectTransform>(), 0f, 0.5f, 946f + sectionOffset, 44f, 12f, 6f);
        redoButton = CreateButton("Redo Layout Button", contentRect, font, "Redo", 16);
        PlaceTopColumnRect(redoButton.GetComponent<RectTransform>(), 0.5f, 1f, 946f + sectionOffset, 44f, 6f, 12f);

        CreateDivider("Camera Views Divider", contentRect, 1004f + sectionOffset);
        Text cameraHeading = CreateText("Camera Views Heading", contentRect, font, "CAMERA VIEWS", 20, AccentColor, FontStyle.Bold);
        PlaceTopRect(cameraHeading.rectTransform, 16f, 1016f + sectionOffset, 30f);
        resetCameraButton = CreateButton("Reset Camera Button", contentRect, font, "Reset Camera", 16);
        PlaceTopRect(resetCameraButton.GetComponent<RectTransform>(), 12f, 1050f + sectionOffset, 44f);
        topViewButton = CreateButton("Top View Button", contentRect, font, "Top View", 15);
        PlaceTopColumnRect(topViewButton.GetComponent<RectTransform>(), 0f, 1f / 3f, 1104f + sectionOffset, 42f, 12f, 5f);
        frontViewButton = CreateButton("Front View Button", contentRect, font, "Front View", 15);
        PlaceTopColumnRect(frontViewButton.GetComponent<RectTransform>(), 1f / 3f, 2f / 3f, 1104f + sectionOffset, 42f, 5f, 5f);
        isometricViewButton = CreateButton("Isometric View Button", contentRect, font, "Isometric", 15);
        PlaceTopColumnRect(isometricViewButton.GetComponent<RectTransform>(), 2f / 3f, 1f, 1104f + sectionOffset, 42f, 5f, 12f);

        CreateDivider("Room Information Divider", contentRect, 1162f + sectionOffset);
        Text informationHeading = CreateText("Room Information Heading", contentRect, font, "ROOM & LAYOUT", 20, AccentColor, FontStyle.Bold);
        PlaceTopRect(informationHeading.rectTransform, 16f, 1174f + sectionOffset, 30f);
        layoutInfo = CreateText("Room and Layout Information", contentRect, font, string.Empty, 15, TextColor, FontStyle.Normal);
        PlaceTopRect(layoutInfo.rectTransform, 16f, 1208f + sectionOffset, 150f);
        layoutInfo.verticalOverflow = VerticalWrapMode.Truncate;
        contentRect.sizeDelta = new Vector2(0f, 1440f + sectionOffset);
        return buttons;
    }

    private static Text BuildHelp(Transform canvas, Font font, out Button[] layoutButtons)
    {
        GameObject panel = CreatePanel("Status and Controls", canvas, PanelColor);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(16f, 12f);
        rect.sizeDelta = new Vector2(440f, 220f);

        GameObject stripe = CreateImage("Help Accent Stripe", panel.transform, AccentColor);
        RectTransform stripeRect = stripe.GetComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.sizeDelta = new Vector2(3f, 0f);

        Text status = CreateText("Status Message", panel.transform, font, "Ready", 17, TextColor, FontStyle.Bold);
        PlaceTopRect(status.rectTransform, 16f, 12f, 36f);
        status.horizontalOverflow = HorizontalWrapMode.Wrap;
        status.verticalOverflow = VerticalWrapMode.Truncate;

        Text help = CreateText("Control Help", panel.transform, font,
            "WASD Walk  ·  Right mouse drag: look / orbit\nWheel: zoom  ·  R: reset  ·  TAB switch  ·  Home/F1-F3 views",
            14, MutedTextColor, FontStyle.Normal);
        PlaceTopRect(help.rectTransform, 16f, 52f, 84f);
        help.horizontalOverflow = HorizontalWrapMode.Wrap;
        help.verticalOverflow = VerticalWrapMode.Truncate;

        layoutButtons = new Button[3];
        string[] labels = { "Save Layout", "Load Layout", "Reset Layout" };
        string[] names = { "Save Layout Button", "Load Layout Button", "Reset Layout Button" };
        const float buttonWidth = 130f;
        const float buttonGap = 8f;
        for (int i = 0; i < layoutButtons.Length; i++)
        {
            layoutButtons[i] = CreateButton(names[i], panel.transform, font, labels[i], 14);
            RectTransform buttonRect = layoutButtons[i].GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = Vector2.zero;
            buttonRect.anchoredPosition = new Vector2(16f + i * (buttonWidth + buttonGap), 12f);
            buttonRect.sizeDelta = new Vector2(buttonWidth, 40f);
        }

        return status;
    }

    private static GameObject BuildWalkthroughOverlay(Transform canvas, Font font)
    {
        GameObject panel = CreatePanel("Walkthrough Controls Overlay", canvas,
            new Color(0.045f, 0.065f, 0.085f, 0.88f));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -104f);
        rect.sizeDelta = new Vector2(310f, 112f);
        panel.GetComponent<Image>().raycastTarget = false;

        Text heading = CreateText("Walkthrough Overlay Heading", panel.transform, font,
            "FIRST-PERSON WALKTHROUGH", 17, AccentColor, FontStyle.Bold);
        PlaceTopRect(heading.rectTransform, 14f, 10f, 28f);
        Text controls = CreateText("Walkthrough Overlay Controls", panel.transform, font,
            "WASD  Move     Hold RMB  Look Around\nSHIFT  Faster Walk     ESC  Exit Walkthrough",
            14, TextColor, FontStyle.Normal);
        PlaceTopRect(controls.rectTransform, 14f, 42f, 56f);
        controls.GetComponent<Text>().raycastTarget = false;
        return panel;
    }

    private static Scrollbar BuildScrollbar(Transform parent)
    {
        GameObject trackObject = CreateImage("Catalog Scroll Track", parent, new Color(1f, 1f, 1f, 0.06f));
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.offsetMin = new Vector2(-12f, 12f);
        trackRect.offsetMax = new Vector2(-7f, -12f);

        GameObject handleObject = CreateImage("Catalog Scroll Handle", trackObject.transform, new Color(0.42f, 0.62f, 0.62f, 0.9f));
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        Scrollbar scrollbar = trackObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleObject.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        ColorBlock colors = scrollbar.colors;
        colors.normalColor = new Color(0.42f, 0.62f, 0.62f, 0.8f);
        colors.highlightedColor = new Color(0.54f, 0.74f, 0.74f, 1f);
        colors.pressedColor = new Color(0.32f, 0.52f, 0.52f, 1f);
        scrollbar.colors = colors;
        return scrollbar;
    }

    private static void PlaceTopRect(RectTransform rect, float horizontalInset, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(horizontalInset, -top - height);
        rect.offsetMax = new Vector2(-horizontalInset, -top);
    }

    private static void PlaceTopColumnRect(RectTransform rect, float leftAnchor, float rightAnchor,
        float top, float height, float leftInset, float rightInset)
    {
        rect.anchorMin = new Vector2(leftAnchor, 1f);
        rect.anchorMax = new Vector2(rightAnchor, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(leftInset, -top - height);
        rect.offsetMax = new Vector2(-rightInset, -top);
    }

    private static void CreateDivider(string name, Transform parent, float top)
    {
        GameObject divider = CreateImage(name, parent, DividerColor);
        RectTransform rect = divider.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-32f, 1f);
        Image image = divider.GetComponent<Image>();
        image.raycastTarget = false;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        GameObject eventSystemObject = eventSystem != null ? eventSystem.gameObject : new GameObject("EventSystem");
        if (eventSystem == null) eventSystem = eventSystemObject.AddComponent<EventSystem>();

        BaseInputModule[] modules = eventSystemObject.GetComponents<BaseInputModule>();
        StandaloneInputModule inputModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (inputModule == null) inputModule = eventSystemObject.AddComponent<StandaloneInputModule>();
        for (int i = 0; i < modules.Length; i++)
        {
            if (modules[i] != null && modules[i] != inputModule) modules[i].enabled = false;
        }
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static Text CreateText(string name, Transform parent, Font font, string value, int size, Color color, FontStyle style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.text = value;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, string label, int fontSize)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.17f, 0.22f, 1f);
        colors.highlightedColor = new Color(0.2f, 0.28f, 0.32f, 1f);
        colors.pressedColor = new Color(0.08f, 0.12f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText("Label", buttonObject.transform, font, label, fontSize, TextColor, FontStyle.Bold);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static void PlaceHeaderButton(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
