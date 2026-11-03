using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Template gallery and named-layout dialogs integrated into the existing UGUI canvas.</summary>
public sealed class LayoutTemplateUI : MonoBehaviour
{
    private readonly Color panelColor = new Color(0.045f, 0.065f, 0.085f, 0.98f);
    private readonly Color cardColor = new Color(0.09f, 0.12f, 0.15f, 1f);
    private readonly Color accentColor = new Color(0.78f, 0.58f, 0.32f, 1f);
    private readonly Color textColor = new Color(0.94f, 0.96f, 0.97f, 1f);
    private readonly Color mutedColor = new Color(0.72f, 0.78f, 0.82f, 1f);

    [SerializeField] private LayoutTemplateService templateService;
    [SerializeField] private LayoutPersistenceService persistence;
    [SerializeField] private AuditoriumDemoSettings settings;
    [SerializeField] private Transform auditoriumRoot;
    [SerializeField] private Transform stageTransform;
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private Font font;
    [SerializeField] private RectTransform overlay;
    private RectTransform dialog;
    private LayoutTemplateDefinition selectedTemplate;
    private InputField nameInput;
    private InputField descriptionInput;
    [SerializeField] private Button galleryButton;
    [SerializeField] private Button savedLayoutsButton;
    [SerializeField] private Button saveAsButton;

    public void Configure(LayoutTemplateService templates, LayoutPersistenceService layoutPersistence,
        AuditoriumDemoSettings demoSettings, Transform roomRoot, Transform stage, PlacementSystem placement,
        Button templatesButton, Button layoutsButton, Button saveButton)
    {
        templateService = templates;
        persistence = layoutPersistence;
        settings = demoSettings;
        auditoriumRoot = roomRoot;
        stageTransform = stage;
        placementSystem = placement;
        galleryButton = templatesButton;
        savedLayoutsButton = layoutsButton;
        saveAsButton = saveButton;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (overlay == null) BuildOverlay();
        BindToolbarButtons();
    }

    private void Start()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (overlay == null && transform.Find("Layout Template Dialog") != null)
            overlay = transform.Find("Layout Template Dialog").GetComponent<RectTransform>();
        BindToolbarButtons();
    }

    private void BindToolbarButtons()
    {
        if (galleryButton != null)
        {
            galleryButton.onClick.RemoveListener(OpenTemplates);
            galleryButton.onClick.AddListener(OpenTemplates);
        }
        if (savedLayoutsButton != null)
        {
            savedLayoutsButton.onClick.RemoveListener(OpenMyLayouts);
            savedLayoutsButton.onClick.AddListener(OpenMyLayouts);
        }
        if (saveAsButton != null)
        {
            saveAsButton.onClick.RemoveListener(OpenSaveAs);
            saveAsButton.onClick.AddListener(OpenSaveAs);
        }
    }

    public void OpenTemplates()
    {
        LayoutTemplateDefinition[] templates = templateService != null ? templateService.GetTemplates() : new LayoutTemplateDefinition[0];
        ShowDialog("CHOOSE EVENT LAYOUT", 1260f, 780f);
        AddText(dialog, "MODE: SELECT / EDIT  ·  Preview is temporary; furniture changes only when you apply a template.",
            17, mutedColor, new Vector2(24f, -58f), new Vector2(1200f, 32f));
        if (templates.Length == 0)
            AddText(dialog, "Templates are unavailable until the auditorium scene has been built.", 18, textColor,
                new Vector2(0f, -180f), new Vector2(900f, 42f));
        else
            for (int i = 0; i < templates.Length; i++) BuildTemplateCard(templates[i], i);
        AddButton(dialog, "Close", "CLOSE", new Vector2(0f, 24f), new Vector2(140f, 42f), Hide);
    }

    public void OpenSaveAs()
    {
        OpenSaveAsWithValues("", "", null);
    }

    private void OpenSaveAsWithValues(string initialName, string initialDescription, string errorMessage)
    {
        ShowDialog("SAVE AS MY LAYOUT", 640f, 430f);
        AddText(dialog, "Save the current arrangement as an independent named JSON layout.", 17, mutedColor,
            new Vector2(28f, -66f), new Vector2(580f, 42f));
        AddText(dialog, "LAYOUT NAME", 15, accentColor, new Vector2(30f, -120f), new Vector2(580f, 24f));
        nameInput = AddInput(dialog, "Enter a layout name", new Vector2(0f, -165f), new Vector2(570f, 44f));
        if (!string.IsNullOrEmpty(initialName)) nameInput.text = initialName;
        AddText(dialog, "DESCRIPTION (OPTIONAL)", 15, accentColor, new Vector2(30f, -222f), new Vector2(580f, 24f));
        descriptionInput = AddInput(dialog, "Event notes", new Vector2(0f, -268f), new Vector2(570f, 44f));
        if (!string.IsNullOrEmpty(initialDescription)) descriptionInput.text = initialDescription;
        if (!string.IsNullOrEmpty(errorMessage)) AddText(dialog, errorMessage, 14, new Color(1f, 0.55f, 0.45f),
            new Vector2(30f, -312f), new Vector2(570f, 38f));
        AddButton(dialog, "Cancel", "CANCEL", new Vector2(-92f, 36f), new Vector2(150f, 42f), Hide);
        AddButton(dialog, "Save", "SAVE LAYOUT", new Vector2(92f, 36f), new Vector2(190f, 42f), SaveCurrentAs);
    }

    public void OpenMyLayouts()
    {
        ShowDialog("MY SAVED LAYOUTS", 1060f, 720f);
        AddText(dialog, "Saved layouts are stored separately under Application.persistentDataPath/VirtualAuditorium/Layouts.",
            16, mutedColor, new Vector2(24f, -58f), new Vector2(1010f, 34f));
        CustomLayoutSummary[] layouts = persistence != null ? persistence.GetMyLayouts() : new CustomLayoutSummary[0];
        if (layouts.Length == 0)
            AddText(dialog, "No saved layouts yet. Use Save As My Layout to create one.", 19, textColor,
                new Vector2(0f, -150f), new Vector2(960f, 44f));
        else BuildSavedLayoutList(layouts);
        AddButton(dialog, "Close", "CLOSE", new Vector2(0f, 24f), new Vector2(140f, 42f), Hide);
    }

    private void BuildOverlay()
    {
        Transform existing = transform.Find("Layout Template Dialog");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
        GameObject shade = new GameObject("Layout Template Dialog", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(transform, false);
        overlay = shade.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        Image shadeImage = shade.GetComponent<Image>();
        shadeImage.color = new Color(0f, 0f, 0f, 0.66f);
        shadeImage.raycastTarget = true;
        overlay.gameObject.SetActive(false);
    }

    private void BuildTemplateCard(LayoutTemplateDefinition template, int index)
    {
        float dialogWidth = dialog != null ? dialog.sizeDelta.x : 1260f;
        float scale = Mathf.Clamp(dialogWidth / 1260f, 0.72f, 1f);
        float cardWidth = 390f * scale;
        float cardHeight = Mathf.Min(585f * scale, (dialog != null ? dialog.sizeDelta.y : 780f) - 150f);
        float spacing = 410f * scale;
        float x = (index - 1) * spacing;
        RectTransform card = CreatePanel("Template Card " + template.Id, dialog, cardColor,
            new Vector2(x, -100f * scale - 12f), new Vector2(cardWidth, cardHeight));
        float contentWidth = cardWidth - 30f;
        AddText(card, template.Name, 20, accentColor, new Vector2(0f, -20f), new Vector2(contentWidth, 32f), FontStyle.Bold);
        AddText(card, template.Category.ToUpperInvariant(), 13, mutedColor, new Vector2(0f, -50f), new Vector2(contentWidth, 22f));
        AddText(card, template.Description, 14, textColor, new Vector2(0f, -88f), new Vector2(contentWidth - 10f, 40f));
        float previewWidth = 166f * scale;
        float previewHeight = 260f * scale;
        CreatePreview(card, "Generated Top Down Preview", template,
            new Vector2(previewWidth, previewHeight), -132f * scale, true);
        AddText(card, FormatCounts(template), 14, mutedColor,
            new Vector2(0f, -132f * scale - previewHeight - 18f), new Vector2(contentWidth, 34f));
        AddButton(card, "Preview", "PREVIEW", new Vector2(-78f * scale, 30f), new Vector2(140f * scale, 38f),
            () => OpenTemplatePreview(template));
        AddButton(card, "Apply", "APPLY TEMPLATE", new Vector2(84f * scale, 30f), new Vector2(176f * scale, 38f),
            () => OpenApplyChoices(template));
    }

    private void OpenTemplatePreview(LayoutTemplateDefinition template)
    {
        selectedTemplate = template;
        ShowDialog("PREVIEW · " + template.Name.ToUpperInvariant(), 850f, 760f);
        AddText(dialog, template.Description + "  ·  " + FormatCounts(template), 17, mutedColor,
            new Vector2(0f, -60f), new Vector2(800f, 50f));
        LayoutTemplatePreviewGraphic preview = CreatePreview(dialog, "Generated Template Preview", template,
            new Vector2(360f, 562.5f), -116f, true);
        if (preview != null && stageTransform != null && auditoriumRoot != null)
        {
            Vector3 stageLocal = auditoriumRoot.InverseTransformPoint(stageTransform.position);
            Vector2 stagePoint;
            if (preview.TryMapRoomPoint(stageLocal.x, stageLocal.z, out stagePoint))
            {
                Text stageLabel = AddText(preview.transform, "STAGE", 14, new Color(1f, 0.91f, 0.77f),
                    stagePoint, new Vector2(90f, 24f), FontStyle.Bold);
                stageLabel.rectTransform.anchorMin = stageLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                stageLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }
        AddText(dialog, "SEATS · BLUE     TABLES · GOLD     DECOR · GREEN     AV · CHARCOAL",
            14, mutedColor, new Vector2(0f, -642f), new Vector2(780f, 24f), FontStyle.Bold);
        AddButton(dialog, "Back", "BACK", new Vector2(-92f, 32f), new Vector2(150f, 42f), OpenTemplates);
        AddButton(dialog, "Apply", "APPLY TEMPLATE", new Vector2(100f, 32f), new Vector2(190f, 42f), () => OpenApplyChoices(template));
    }

    private LayoutTemplatePreviewGraphic CreatePreview(Transform parent, string objectName,
        LayoutTemplateDefinition template, Vector2 size, float topOffset, bool showDiagnostic)
    {
        GameObject previewObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(LayoutTemplatePreviewGraphic));
        previewObject.transform.SetParent(parent, false);
        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = previewRect.anchorMax = new Vector2(0.5f, 1f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(0f, topOffset - size.y * 0.5f);
        previewRect.sizeDelta = size;
        LayoutTemplatePreviewGraphic graphic = previewObject.GetComponent<LayoutTemplatePreviewGraphic>();
        if (graphic == null)
        {
            Debug.LogError("Could not create the layout preview Graphic for template '" +
                (template != null ? template.Id : "<missing>") + "'.", previewObject);
            if (showDiagnostic)
                AddText(previewRect, "Preview renderer is missing.", 17, new Color(1f, 0.55f, 0.48f),
                    new Vector2(0f, -size.y * 0.5f), new Vector2(size.x - 20f, 60f), FontStyle.Bold);
            return null;
        }

        Text diagnosticLabel = AddText(previewRect, "Preview Diagnostic", 15,
            new Color(0.52f, 0.10f, 0.08f), new Vector2(0f, -size.y * 0.5f),
            new Vector2(size.x - 24f, Mathf.Min(84f, size.y - 16f)), FontStyle.Bold);
        diagnosticLabel.gameObject.SetActive(false);
        graphic.DiagnosticChanged += message =>
        {
            diagnosticLabel.text = message ?? string.Empty;
            diagnosticLabel.gameObject.SetActive(!string.IsNullOrEmpty(message));
        };
        graphic.Configure(template, settings, auditoriumRoot, stageTransform);
        diagnosticLabel.text = graphic.DiagnosticMessage ?? string.Empty;
        diagnosticLabel.gameObject.SetActive(!string.IsNullOrEmpty(graphic.DiagnosticMessage));
        return graphic;
    }

    private void OpenApplyChoices(LayoutTemplateDefinition template)
    {
        selectedTemplate = template;
        ShowDialog("APPLY " + template.Name.ToUpperInvariant(), 640f, 430f);
        AddText(dialog, "Choose how this complete " + template.TotalCount + " item arrangement should be applied.",
            18, mutedColor, new Vector2(0f, -76f), new Vector2(580f, 54f));
        AddButton(dialog, "Replace", "REPLACE CURRENT LAYOUT", new Vector2(0f, -173f), new Vector2(330f, 54f),
            () => ConfirmApply(template, true));
        AddButton(dialog, "Add", "ADD TO CURRENT LAYOUT", new Vector2(0f, -242f), new Vector2(330f, 54f),
            () => ConfirmApply(template, false));
        AddButton(dialog, "Cancel", "CANCEL", new Vector2(0f, 34f), new Vector2(150f, 42f), OpenTemplates);
    }

    private void ConfirmApply(LayoutTemplateDefinition template, bool replace)
    {
        if (replace)
        {
            ShowDialog("CONFIRM REPLACEMENT", 620f, 380f);
            AddText(dialog, "This will replace all currently placed furniture with the " + template.Name + " arrangement. The complete change is one undoable action.",
                18, textColor, new Vector2(0f, -98f), new Vector2(540f, 90f));
            AddButton(dialog, "Back", "CANCEL", new Vector2(-92f, 40f), new Vector2(150f, 42f), () => OpenApplyChoices(template));
            AddButton(dialog, "Confirm", "REPLACE LAYOUT", new Vector2(100f, 40f), new Vector2(190f, 42f), () => ApplyTemplate(template, true));
        }
        else ApplyTemplate(template, false);
    }

    private void ApplyTemplate(LayoutTemplateDefinition template, bool replace)
    {
        string message = null;
        bool applied = templateService != null && templateService.ApplyTemplate(template, replace, out message);
        if (applied)
        {
            Hide();
            return;
        }

        if (templateService == null) message = "The layout template service is unavailable.";
        ShowDialog("TEMPLATE NOT APPLIED", 680f, 370f);
        AddText(dialog, string.IsNullOrEmpty(message) ? "The proposed layout could not be applied." : message,
            18, textColor, new Vector2(0f, -94f), new Vector2(590f, 100f));
        AddButton(dialog, "Back", "BACK TO TEMPLATES", new Vector2(-92f, 34f), new Vector2(190f, 42f), OpenTemplates);
        AddButton(dialog, "Close", "CLOSE", new Vector2(108f, 34f), new Vector2(140f, 42f), Hide);
    }

    private void SaveCurrentAs()
    {
        if (persistence == null) return;
        string savedFile;
        string error;
        if (persistence.SaveAsMyLayout(nameInput != null ? nameInput.text : string.Empty,
            descriptionInput != null ? descriptionInput.text : string.Empty, false, out savedFile, out error))
        {
            Hide();
            return;
        }
        if (error != null && error.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            string name = nameInput.text;
            string description = descriptionInput.text;
            ShowDialog("OVERWRITE SAVED LAYOUT?", 620f, 360f);
            AddText(dialog, "A layout with this name already exists. Replace its saved contents with the current arrangement?",
                18, textColor, new Vector2(0f, -100f), new Vector2(540f, 74f));
            AddButton(dialog, "Back", "CANCEL", new Vector2(-92f, 38f), new Vector2(150f, 42f),
                () => OpenSaveAsWithValues(name, description, null));
            AddButton(dialog, "Overwrite", "OVERWRITE", new Vector2(100f, 38f), new Vector2(180f, 42f), () =>
            {
                if (persistence.SaveAsMyLayout(name, description, true, out savedFile, out error)) Hide();
                else OpenSaveAsWithValues(name, description, error);
            });
        }
        else OpenSaveAsWithValues(nameInput != null ? nameInput.text : string.Empty,
            descriptionInput != null ? descriptionInput.text : string.Empty, error);
    }

    private void BuildSavedLayoutList(CustomLayoutSummary[] layouts)
    {
        GameObject viewportObject = new GameObject("Saved Layout Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(dialog, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 1f);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.anchoredPosition = new Vector2(0f, -96f);
        viewport.sizeDelta = new Vector2(1000f, 525f);
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.015f);
        GameObject contentObject = new GameObject("Saved Layout Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewport, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.sizeDelta = new Vector2(0f, layouts.Length * 114f + 8f);
        ScrollRect scroll = dialog.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;
        for (int i = 0; i < layouts.Length; i++) BuildSavedLayoutRow(layouts[i], i, content);
    }

    private void BuildSavedLayoutRow(CustomLayoutSummary layout, int index, Transform content)
    {
        float y = -8f - index * 114f;
        RectTransform row = CreatePanel("Saved Layout " + index, content, cardColor,
            new Vector2(0f, y), new Vector2(990f, 106f));
        Text nameHeading = AddText(row, layout.Name, 18, accentColor,
            new Vector2(-205f, -7f), new Vector2(540f, 42f), FontStyle.Bold, TextAnchor.MiddleLeft);
        nameHeading.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameHeading.verticalOverflow = VerticalWrapMode.Overflow;

        string details = layout.FurnitureCount + " furniture items";
        string date = FormatLayoutDate(layout.ModifiedUtc);
        if (!string.IsNullOrEmpty(date)) details += "  ·  Modified " + date;
        else
        {
            date = FormatLayoutDate(layout.CreatedUtc);
            if (!string.IsNullOrEmpty(date)) details += "  ·  Created " + date;
        }
        if (!string.IsNullOrWhiteSpace(layout.Description)) details += "  ·  " + layout.Description.Trim();
        Text metadata = AddText(row, details, 12, mutedColor,
            new Vector2(-205f, -51f), new Vector2(540f, 51f), FontStyle.Normal, TextAnchor.MiddleLeft);
        metadata.horizontalOverflow = HorizontalWrapMode.Wrap;
        AddButton(row, "Load", "LOAD", new Vector2(136f, 35f), new Vector2(90f, 36f), () => ConfirmLoad(layout));
        AddButton(row, "Rename", "RENAME", new Vector2(244f, 35f), new Vector2(105f, 36f), () => OpenRename(layout));
        AddButton(row, "Delete", "DELETE", new Vector2(360f, 35f), new Vector2(100f, 36f), () => ConfirmDelete(layout));
    }

    private void ConfirmLoad(CustomLayoutSummary layout)
    {
        ShowDialog("LOAD SAVED LAYOUT?", 620f, 380f);
        AddText(dialog, "Loading '" + layout.Name + "' replaces the current furniture arrangement and clears the current undo history.",
            18, textColor, new Vector2(0f, -100f), new Vector2(540f, 84f));
        AddButton(dialog, "Back", "CANCEL", new Vector2(-92f, 38f), new Vector2(150f, 42f), OpenMyLayouts);
        AddButton(dialog, "Load", "LOAD LAYOUT", new Vector2(100f, 38f), new Vector2(180f, 42f), () =>
        {
            if (persistence != null && persistence.LoadMyLayout(layout.FileName)) Hide();
            else OpenMyLayouts();
        });
    }

    private void OpenRename(CustomLayoutSummary layout)
    {
        OpenRename(layout, layout.Name, null);
    }

    private void OpenRename(CustomLayoutSummary layout, string currentName, string errorMessage)
    {
        ShowDialog("RENAME SAVED LAYOUT", 620f, 360f);
        AddText(dialog, "Enter a new name for '" + layout.Name + "'.", 17, mutedColor,
            new Vector2(0f, -72f), new Vector2(550f, 34f));
        nameInput = AddInput(dialog, "Enter a new name", new Vector2(0f, -140f), new Vector2(540f, 44f));
        nameInput.text = currentName;
        if (!string.IsNullOrEmpty(errorMessage)) AddText(dialog, errorMessage, 14,
            new Color(1f, 0.55f, 0.45f), new Vector2(0f, -196f), new Vector2(540f, 32f));
        AddButton(dialog, "Back", "CANCEL", new Vector2(-92f, 40f), new Vector2(150f, 42f), OpenMyLayouts);
        AddButton(dialog, "Rename", "RENAME", new Vector2(100f, 40f), new Vector2(170f, 42f), () => RenameSavedLayout(layout));
    }

    private void RenameSavedLayout(CustomLayoutSummary layout)
    {
        string error;
        string newName = nameInput != null ? nameInput.text : string.Empty;
        if (persistence.RenameMyLayout(layout.FileName, newName, false, out error)) { OpenMyLayouts(); return; }
        if (error != null && error.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            ShowDialog("OVERWRITE NAME?", 620f, 360f);
            AddText(dialog, "Another saved layout already has that name. Replace it with this renamed layout?", 18, textColor,
                new Vector2(0f, -100f), new Vector2(540f, 70f));
            AddButton(dialog, "Back", "CANCEL", new Vector2(-92f, 38f), new Vector2(150f, 42f), () => OpenRename(layout, newName, null));
            AddButton(dialog, "Overwrite", "OVERWRITE", new Vector2(100f, 38f), new Vector2(180f, 42f), () =>
            {
                if (persistence.RenameMyLayout(layout.FileName, newName, true, out error)) OpenMyLayouts();
                else OpenRename(layout, newName, error);
            });
        }
        else OpenRename(layout, newName, error);
    }

    private void ConfirmDelete(CustomLayoutSummary layout)
    {
        ShowDialog("DELETE SAVED LAYOUT?", 620f, 360f);
        AddText(dialog, "Permanently delete the saved file '" + layout.Name + "'? This does not affect the layout currently open in the auditorium.",
            18, textColor, new Vector2(0f, -100f), new Vector2(540f, 84f));
        AddButton(dialog, "Back", "CANCEL", new Vector2(-92f, 38f), new Vector2(150f, 42f), OpenMyLayouts);
        AddButton(dialog, "Delete", "DELETE", new Vector2(100f, 38f), new Vector2(170f, 42f), () =>
        {
            if (persistence != null) persistence.DeleteMyLayout(layout.FileName);
            OpenMyLayouts();
        });
    }

    private void ShowDialog(string title, float width, float height)
    {
        if (overlay == null) BuildOverlay();
        if (placementSystem != null && placementSystem.IsPlacing) placementSystem.CancelPlacement();
        ClearOverlayContents();
        overlay.gameObject.SetActive(true);
        overlay.SetAsLastSibling();
        FitDialogToCanvas(ref width, ref height);
        GameObject panel = new GameObject("Dialog Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay, false);
        dialog = panel.GetComponent<RectTransform>();
        dialog.anchorMin = dialog.anchorMax = new Vector2(0.5f, 0.5f);
        dialog.pivot = new Vector2(0.5f, 0.5f);
        dialog.sizeDelta = new Vector2(width, height);
        panel.GetComponent<Image>().color = panelColor;
        AddText(dialog, title, 25, accentColor, new Vector2(0f, -24f), new Vector2(width - 56f, 38f), FontStyle.Bold);
        AddDivider(dialog, height * 0.5f - 52f, width - 48f);
    }

    private void FitDialogToCanvas(ref float width, ref float height)
    {
        RectTransform canvas = transform as RectTransform;
        if (canvas == null) return;
        float maxWidth = Mathf.Max(360f, canvas.rect.width - 24f);
        float maxHeight = Mathf.Max(300f, canvas.rect.height - 24f);
        width = Mathf.Min(width, maxWidth);
        height = Mathf.Min(height, maxHeight);
    }

    private void Hide()
    {
        if (overlay != null) overlay.gameObject.SetActive(false);
        ClearOverlayContents();
        nameInput = null;
        descriptionInput = null;
        selectedTemplate = null;
    }

    private void ClearOverlayContents()
    {
        if (overlay == null) return;
        for (int i = overlay.childCount - 1; i >= 0; i--)
        {
            GameObject child = overlay.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        dialog = null;
    }

    private string FormatCounts(LayoutTemplateDefinition template)
    {
        string result = template.TotalCount + " items";
        FurnitureType[] types = (FurnitureType[])Enum.GetValues(typeof(FurnitureType));
        for (int i = 0; i < types.Length; i++)
        {
            int count = template.CountOf(types[i]);
            if (count <= 0) continue;
            result += "  ·  " + count + " " + FurnitureCatalog.Get(types[i]).DisplayName;
        }
        return result;
    }

    private static string FormatLayoutDate(string value)
    {
        DateTime date;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date)
            ? date.ToLocalTime().ToString("d MMMM yyyy", CultureInfo.CurrentCulture) : null;
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        panel.GetComponent<Image>().color = color;
        return rect;
    }

    private Text AddText(Transform parent, string value, int size, Color color, Vector2 position, Vector2 dimensions,
        FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private Button AddButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Action action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.21f, 0.25f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.23f, 0.32f, 0.35f, 1f);
        colors.pressedColor = accentColor;
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        AddText(rect, label, 15, textColor, Vector2.zero, size - new Vector2(10f, 4f), FontStyle.Bold);
        if (action != null) button.onClick.AddListener(() => action());
        return button;
    }

    private InputField AddInput(Transform parent, string placeholder, Vector2 position, Vector2 size)
    {
        GameObject fieldObject = new GameObject("Text Input", typeof(RectTransform), typeof(Image), typeof(InputField));
        fieldObject.transform.SetParent(parent, false);
        RectTransform rect = fieldObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        fieldObject.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.18f, 1f);
        Text text = AddText(rect, string.Empty, 17, textColor, new Vector2(0f, -3f), size - new Vector2(20f, 8f),
            FontStyle.Normal, TextAnchor.MiddleLeft);
        Text hint = AddText(rect, placeholder, 16, mutedColor, new Vector2(0f, -3f), size - new Vector2(20f, 8f),
            FontStyle.Italic, TextAnchor.MiddleLeft);
        InputField input = fieldObject.GetComponent<InputField>();
        input.targetGraphic = fieldObject.GetComponent<Image>();
        input.textComponent = text;
        input.placeholder = hint;
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = 180;
        return input;
    }

    private void AddDivider(Transform parent, float y, float width)
    {
        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(parent, false);
        RectTransform rect = divider.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -y);
        rect.sizeDelta = new Vector2(width, 1f);
        divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
    }
}
