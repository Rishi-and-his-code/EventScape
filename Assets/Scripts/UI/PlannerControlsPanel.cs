using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Connects camera and furniture catalog controls to the generated UGUI.
/// </summary>
public sealed class PlannerControlsPanel : MonoBehaviour
{
    [SerializeField] private CameraModeController cameraModes;
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private Button walkthroughButton;
    [SerializeField] private Button planningButton;
    [SerializeField] private Button[] catalogButtons;
    [SerializeField] private FurnitureType[] catalogTypes;
    [SerializeField] private Text statusLabel;
    [SerializeField] private Text selectedItemLabel;
    [SerializeField] private Text placementInstructionsLabel;
    [SerializeField] private Text selectedObjectLabel;
    [SerializeField] private Button moveSelectedButton;
    [SerializeField] private Button rotateLeftButton;
    [SerializeField] private Button rotateRightButton;
    [SerializeField] private Button deleteSelectedButton;
    [SerializeField] private LayoutPersistenceService persistenceService;
    [SerializeField] private LayoutTemplateService layoutTemplateService;
    [SerializeField] private Button saveLayoutButton;
    [SerializeField] private Button loadLayoutButton;
    [SerializeField] private Button resetLayoutButton;
    [SerializeField] private Button duplicateSelectedButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;
    [SerializeField] private Button gridToggleButton;
    [SerializeField] private Button[] gridIncrementButtons;
    [SerializeField] private Button resetCameraButton;
    [SerializeField] private Button topViewButton;
    [SerializeField] private Button frontViewButton;
    [SerializeField] private Button isometricViewButton;
    [SerializeField] private Text layoutInfoLabel;
    [SerializeField] private Button finishPlacingButton;
    [SerializeField] private Button referenceSeatingButton;
    [SerializeField] private ReferenceSeatingController referenceSeatingController;
    [SerializeField] private GameObject walkthroughOverlay;
    [SerializeField] private GameObject catalogPanel;
    [SerializeField] private GameObject statusPanel;

    private readonly Color activeButtonColor = new Color(0.22f, 0.38f, 0.36f, 1f);
    private readonly Color idleButtonColor = new Color(0.12f, 0.17f, 0.22f, 1f);
    private UnityAction[] catalogListeners;
    private UnityAction moveListener;
    private UnityAction rotateLeftListener;
    private UnityAction rotateRightListener;
    private UnityAction deleteListener;
    private UnityAction saveLayoutListener;
    private UnityAction loadLayoutListener;
    private UnityAction resetLayoutListener;
    private UnityAction duplicateListener;
    private UnityAction undoListener;
    private UnityAction redoListener;
    private UnityAction gridToggleListener;
    private UnityAction[] gridIncrementListeners;
    private UnityAction resetCameraListener;
    private UnityAction topViewListener;
    private UnityAction frontViewListener;
    private UnityAction isometricViewListener;
    private UnityAction finishPlacingListener;
    private UnityAction referenceSeatingListener;

    public void Configure(CameraModeController modeController, PlacementSystem placement,
        Button walkButton, Button planButton, Button[] itemButtons, FurnitureType[] itemTypes, Text status,
        Text selectedItem, Text placementInstructions, Text selectedObject, Button moveButton,
        Button turnLeftButton, Button turnRightButton, Button deleteButton,
        LayoutPersistenceService persistence, Button saveButton, Button loadButton, Button resetButton)
    {
        cameraModes = modeController;
        placementSystem = placement;
        walkthroughButton = walkButton;
        planningButton = planButton;
        catalogButtons = itemButtons;
        catalogTypes = itemTypes;
        statusLabel = status;
        selectedItemLabel = selectedItem;
        placementInstructionsLabel = placementInstructions;
        selectedObjectLabel = selectedObject;
        moveSelectedButton = moveButton;
        rotateLeftButton = turnLeftButton;
        rotateRightButton = turnRightButton;
        deleteSelectedButton = deleteButton;
        persistenceService = persistence;
        saveLayoutButton = saveButton;
        loadLayoutButton = loadButton;
        resetLayoutButton = resetButton;
    }

    public void ConfigurePlanningControls(Button duplicateButton, Button undo, Button redo,
        Button gridToggle, Button[] gridIncrements, Button resetCamera, Button topView,
        Button frontView, Button isometricView, Text layoutInfo)
    {
        duplicateSelectedButton = duplicateButton;
        undoButton = undo;
        redoButton = redo;
        gridToggleButton = gridToggle;
        gridIncrementButtons = gridIncrements;
        resetCameraButton = resetCamera;
        topViewButton = topView;
        frontViewButton = frontView;
        isometricViewButton = isometricView;
        layoutInfoLabel = layoutInfo;
    }

    public void ConfigureInteractionMode(Button finishButton)
    {
        finishPlacingButton = finishButton;
    }

    public void ConfigureLayoutTemplates(LayoutTemplateService service)
    {
        layoutTemplateService = service;
    }

    public void ConfigureReferenceSeating(Button toggleButton, ReferenceSeatingController seatingController)
    {
        referenceSeatingButton = toggleButton;
        referenceSeatingController = seatingController;
    }

    public void ConfigureWalkthroughInterface(GameObject overlay, GameObject catalog, GameObject status)
    {
        walkthroughOverlay = overlay;
        catalogPanel = catalog;
        statusPanel = status;
    }

    private void Start()
    {
        EnsureTemplateRuntimeIntegration();
        PlannerUIBuilder.EnsureResponsiveLayout(gameObject);
        if (walkthroughButton != null) walkthroughButton.onClick.AddListener(ShowWalkthrough);
        if (planningButton != null) planningButton.onClick.AddListener(ShowPlanning);
        if (cameraModes != null) cameraModes.ModeChanged += UpdateModeDisplay;
        if (placementSystem != null)
        {
            placementSystem.FeedbackChanged += SetStatus;
            placementSystem.SelectionChanged += UpdatePlacementInstructions;
            placementSystem.SelectedObjectChanged += UpdateSelectedObject;
        }
        if (persistenceService != null) persistenceService.FeedbackChanged += SetStatus;
        if (layoutTemplateService != null) layoutTemplateService.FeedbackChanged += SetStatus;
        if (placementSystem != null)
        {
            placementSystem.HistoryChanged += UpdateHistoryButtons;
            placementSystem.LayoutChanged += UpdateLayoutInfo;
            placementSystem.GridSettingsChanged += UpdateGridControls;
        }

        moveListener = () => { if (placementSystem != null) placementSystem.MoveSelected(); };
        rotateLeftListener = () => { if (placementSystem != null) placementSystem.RotateSelected(-1f); };
        rotateRightListener = () => { if (placementSystem != null) placementSystem.RotateSelected(1f); };
        deleteListener = () => { if (placementSystem != null) placementSystem.DeleteSelected(); };
        saveLayoutListener = () => { if (persistenceService != null) persistenceService.SaveLayout(); };
        loadLayoutListener = () => { if (persistenceService != null) persistenceService.LoadLayout(); };
        resetLayoutListener = () => { if (persistenceService != null) persistenceService.ResetLayout(); };
        duplicateListener = () => { if (placementSystem != null) placementSystem.DuplicateSelected(); };
        undoListener = () => { if (placementSystem != null) placementSystem.Undo(); };
        redoListener = () => { if (placementSystem != null) placementSystem.Redo(); };
        gridToggleListener = () =>
        {
            if (placementSystem != null) placementSystem.SetGridSnapping(!placementSystem.GridSnappingEnabled);
        };
        resetCameraListener = () => { if (cameraModes != null) cameraModes.ResetCamera(); };
        topViewListener = () => { if (cameraModes != null) cameraModes.ShowTopView(); };
        frontViewListener = () => { if (cameraModes != null) cameraModes.ShowFrontView(); };
        isometricViewListener = () => { if (cameraModes != null) cameraModes.ShowIsometricView(); };
        finishPlacingListener = () => { if (placementSystem != null) placementSystem.FinishPlacing(); };
        referenceSeatingListener = () =>
        {
            if (referenceSeatingController != null) referenceSeatingController.Toggle();
            UpdateReferenceSeatingDisplay();
        };
        if (moveSelectedButton != null) moveSelectedButton.onClick.AddListener(moveListener);
        if (rotateLeftButton != null) rotateLeftButton.onClick.AddListener(rotateLeftListener);
        if (rotateRightButton != null) rotateRightButton.onClick.AddListener(rotateRightListener);
        if (deleteSelectedButton != null) deleteSelectedButton.onClick.AddListener(deleteListener);
        if (saveLayoutButton != null) saveLayoutButton.onClick.AddListener(saveLayoutListener);
        if (loadLayoutButton != null) loadLayoutButton.onClick.AddListener(loadLayoutListener);
        if (resetLayoutButton != null) resetLayoutButton.onClick.AddListener(resetLayoutListener);
        if (duplicateSelectedButton != null) duplicateSelectedButton.onClick.AddListener(duplicateListener);
        if (undoButton != null) undoButton.onClick.AddListener(undoListener);
        if (redoButton != null) redoButton.onClick.AddListener(redoListener);
        if (gridToggleButton != null) gridToggleButton.onClick.AddListener(gridToggleListener);
        if (resetCameraButton != null) resetCameraButton.onClick.AddListener(resetCameraListener);
        if (topViewButton != null) topViewButton.onClick.AddListener(topViewListener);
        if (frontViewButton != null) frontViewButton.onClick.AddListener(frontViewListener);
        if (isometricViewButton != null) isometricViewButton.onClick.AddListener(isometricViewListener);
        if (finishPlacingButton != null) finishPlacingButton.onClick.AddListener(finishPlacingListener);
        if (referenceSeatingButton != null) referenceSeatingButton.onClick.AddListener(referenceSeatingListener);
        BindGridIncrementButtons();

        BindCatalogButtons();
        UpdateModeDisplay(cameraModes != null ? cameraModes.CurrentMode : AuditoriumCameraMode.Walkthrough);
        UpdatePlacementInstructions(string.Empty);
        UpdateInteractionModeButton();
        UpdateSelectedObject(null);
        UpdateGridControls();
        UpdateHistoryButtons();
        UpdateLayoutInfo();
        UpdateReferenceSeatingDisplay();
    }

    private void EnsureTemplateRuntimeIntegration()
    {
        if (placementSystem == null)
            placementSystem = UnityEngine.Object.FindFirstObjectByType<PlacementSystem>();
        if (persistenceService == null)
            persistenceService = UnityEngine.Object.FindFirstObjectByType<LayoutPersistenceService>();

        GameObject persistenceRoot = persistenceService != null
            ? persistenceService.gameObject : GameObject.Find("Persistence");
        if (persistenceRoot == null)
        {
            persistenceRoot = new GameObject("Persistence");
            persistenceRoot.AddComponent<LayoutPersistenceService>();
        }
        if (persistenceService == null)
        {
            persistenceService = persistenceRoot.GetComponent<LayoutPersistenceService>();
            if (persistenceService == null) persistenceService = persistenceRoot.AddComponent<LayoutPersistenceService>();
            if (placementSystem != null) persistenceService.Configure(placementSystem);
        }

        if (layoutTemplateService == null)
            layoutTemplateService = persistenceRoot.GetComponent<LayoutTemplateService>();
        if (layoutTemplateService == null) layoutTemplateService = persistenceRoot.AddComponent<LayoutTemplateService>();

        GameObject environment = GameObject.Find("Environment");
        GameObject stage = GameObject.Find("Auditorium_Stage");
        AuditoriumDemoSettings settings = placementSystem != null ? placementSystem.Settings : null;
        if (placementSystem != null && settings != null && environment != null && stage != null)
        {
            layoutTemplateService.Configure(placementSystem, settings, environment.transform, stage.transform);
        }
        else
        {
            SetStatus("Layout templates need the auditorium, stage, and placement system. Run Build Demo Scene to repair the scene.");
        }

        if (!PlannerUIBuilder.EnsureTemplateEntryPoints(layoutTemplateService, persistenceService,
            placementSystem, settings, environment != null ? environment.transform : null,
            stage != null ? stage.transform : null))
        {
            SetStatus("Layout template controls could not be added to the existing Interface canvas.");
        }
    }

    private void OnDestroy()
    {
        if (walkthroughButton != null) walkthroughButton.onClick.RemoveListener(ShowWalkthrough);
        if (planningButton != null) planningButton.onClick.RemoveListener(ShowPlanning);
        if (moveSelectedButton != null && moveListener != null) moveSelectedButton.onClick.RemoveListener(moveListener);
        if (rotateLeftButton != null && rotateLeftListener != null) rotateLeftButton.onClick.RemoveListener(rotateLeftListener);
        if (rotateRightButton != null && rotateRightListener != null) rotateRightButton.onClick.RemoveListener(rotateRightListener);
        if (deleteSelectedButton != null && deleteListener != null) deleteSelectedButton.onClick.RemoveListener(deleteListener);
        if (saveLayoutButton != null && saveLayoutListener != null) saveLayoutButton.onClick.RemoveListener(saveLayoutListener);
        if (loadLayoutButton != null && loadLayoutListener != null) loadLayoutButton.onClick.RemoveListener(loadLayoutListener);
        if (resetLayoutButton != null && resetLayoutListener != null) resetLayoutButton.onClick.RemoveListener(resetLayoutListener);
        if (duplicateSelectedButton != null && duplicateListener != null) duplicateSelectedButton.onClick.RemoveListener(duplicateListener);
        if (undoButton != null && undoListener != null) undoButton.onClick.RemoveListener(undoListener);
        if (redoButton != null && redoListener != null) redoButton.onClick.RemoveListener(redoListener);
        if (gridToggleButton != null && gridToggleListener != null) gridToggleButton.onClick.RemoveListener(gridToggleListener);
        if (resetCameraButton != null && resetCameraListener != null) resetCameraButton.onClick.RemoveListener(resetCameraListener);
        if (topViewButton != null && topViewListener != null) topViewButton.onClick.RemoveListener(topViewListener);
        if (frontViewButton != null && frontViewListener != null) frontViewButton.onClick.RemoveListener(frontViewListener);
        if (isometricViewButton != null && isometricViewListener != null) isometricViewButton.onClick.RemoveListener(isometricViewListener);
        if (finishPlacingButton != null && finishPlacingListener != null) finishPlacingButton.onClick.RemoveListener(finishPlacingListener);
        if (referenceSeatingButton != null && referenceSeatingListener != null) referenceSeatingButton.onClick.RemoveListener(referenceSeatingListener);
        UnbindGridIncrementButtons();
        if (catalogButtons != null && catalogListeners != null)
        {
            for (int i = 0; i < catalogButtons.Length && i < catalogListeners.Length; i++)
            {
                if (catalogButtons[i] != null && catalogListeners[i] != null)
                    catalogButtons[i].onClick.RemoveListener(catalogListeners[i]);
            }
        }

        if (cameraModes != null) cameraModes.ModeChanged -= UpdateModeDisplay;
        if (placementSystem != null)
        {
            placementSystem.FeedbackChanged -= SetStatus;
            placementSystem.SelectionChanged -= UpdatePlacementInstructions;
            placementSystem.SelectedObjectChanged -= UpdateSelectedObject;
            placementSystem.HistoryChanged -= UpdateHistoryButtons;
            placementSystem.LayoutChanged -= UpdateLayoutInfo;
            placementSystem.GridSettingsChanged -= UpdateGridControls;
        }
        if (persistenceService != null) persistenceService.FeedbackChanged -= SetStatus;
    }

    private void BindCatalogButtons()
    {
        if (catalogButtons == null || placementSystem == null) return;
        catalogListeners = new UnityAction[catalogButtons.Length];
        for (int i = 0; i < catalogButtons.Length; i++)
        {
            if (catalogButtons[i] == null) continue;
            int capturedIndex = i;
            if (catalogTypes == null || capturedIndex >= catalogTypes.Length) continue;
            FurnitureType type = catalogTypes[capturedIndex];
            catalogListeners[i] = () => placementSystem.BeginPlacement(type);
            catalogButtons[i].onClick.AddListener(catalogListeners[i]);
        }
    }

    private void BindGridIncrementButtons()
    {
        if (gridIncrementButtons == null || placementSystem == null) return;
        float[] increments = { 0.25f, 0.5f, 1f };
        gridIncrementListeners = new UnityAction[gridIncrementButtons.Length];
        for (int i = 0; i < gridIncrementButtons.Length && i < increments.Length; i++)
        {
            if (gridIncrementButtons[i] == null) continue;
            float increment = increments[i];
            gridIncrementListeners[i] = () => placementSystem.SetGridIncrement(increment);
            gridIncrementButtons[i].onClick.AddListener(gridIncrementListeners[i]);
        }
    }

    private void UnbindGridIncrementButtons()
    {
        if (gridIncrementButtons == null || gridIncrementListeners == null) return;
        for (int i = 0; i < gridIncrementButtons.Length && i < gridIncrementListeners.Length; i++)
            if (gridIncrementButtons[i] != null && gridIncrementListeners[i] != null)
                gridIncrementButtons[i].onClick.RemoveListener(gridIncrementListeners[i]);
    }

    private void Update()
    {
        if (cameraModes == null || !CanUseCameraShortcuts()) return;
        if (Input.GetKeyDown(KeyCode.Home)) cameraModes.ResetCamera();
        else if (Input.GetKeyDown(KeyCode.F1)) cameraModes.ShowTopView();
        else if (Input.GetKeyDown(KeyCode.F2)) cameraModes.ShowFrontView();
        else if (Input.GetKeyDown(KeyCode.F3)) cameraModes.ShowIsometricView();
    }

    private static bool CanUseCameraShortcuts()
    {
        if (EventSystem.current == null) return true;
        if (EventSystem.current.IsPointerOverGameObject()) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected == null || selected.GetComponentInParent<InputField>() == null;
    }

    private void UpdateGridControls()
    {
        if (placementSystem == null) return;
        Text toggleLabel = gridToggleButton != null ? gridToggleButton.GetComponentInChildren<Text>() : null;
        if (toggleLabel != null)
            toggleLabel.text = placementSystem.GridSnappingEnabled ? "Grid Snap: ON" : "Grid Snap: OFF";
        SetButtonColor(gridToggleButton, placementSystem.GridSnappingEnabled ? activeButtonColor : idleButtonColor);

        float[] increments = { 0.25f, 0.5f, 1f };
        for (int i = 0; gridIncrementButtons != null && i < gridIncrementButtons.Length && i < increments.Length; i++)
            SetButtonColor(gridIncrementButtons[i], Mathf.Approximately(placementSystem.GridIncrement, increments[i])
                ? activeButtonColor : idleButtonColor);
    }

    private void UpdateReferenceSeatingDisplay()
    {
        if (referenceSeatingButton == null) return;
        bool visible = referenceSeatingController != null && referenceSeatingController.IsVisible;
        Text label = referenceSeatingButton.GetComponentInChildren<Text>();
        if (label != null) label.text = visible ? "Reference Seating: ON" : "Reference Seating: OFF";
        SetButtonColor(referenceSeatingButton, visible ? activeButtonColor : idleButtonColor);
    }

    private void UpdateHistoryButtons()
    {
        if (undoButton != null) undoButton.interactable = placementSystem != null && placementSystem.CanUndo;
        if (redoButton != null) redoButton.interactable = placementSystem != null && placementSystem.CanRedo;
        bool canDuplicate = placementSystem != null && placementSystem.SelectedObject != null && !placementSystem.IsPlacing;
        if (duplicateSelectedButton != null) duplicateSelectedButton.interactable = canDuplicate;
    }

    private void UpdateLayoutInfo()
    {
        if (layoutInfoLabel == null || placementSystem == null) return;
        AuditoriumDemoSettings settings = placementSystem.Settings;
        if (settings == null)
        {
            layoutInfoLabel.text = "Room and stage measurements are unavailable.";
            return;
        }

        string info = "ROOM  " + FormatOne(settings.hallDepth) + " m × " + FormatOne(settings.hallWidth) + " m × " + Format(settings.wallHeight) + " m\n"
            + "STAGE  " + FormatOne(settings.stageWidth) + " m × " + FormatOne(settings.stageDepth) + " m; top " + Format(settings.stageHeight) + " m\n"
            + "FURNITURE  " + placementSystem.PlacedFurnitureCount + " item(s)";
        FurnitureInstance selected = placementSystem.SelectedObject;
        if (selected != null)
        {
            Vector3 position = selected.transform.position;
            info += "\nSELECTED  " + FurnitureCatalog.Get(selected.Type).DisplayName
                + "  (" + position.x.ToString("F2", CultureInfo.InvariantCulture) + ", "
                + position.y.ToString("F2", CultureInfo.InvariantCulture) + ", "
                + position.z.ToString("F2", CultureInfo.InvariantCulture) + ") m";
        }
        layoutInfoLabel.text = info;
    }

    private static string Format(float value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string FormatOne(float value)
    {
        return value.ToString("F1", CultureInfo.InvariantCulture);
    }

    private void ShowWalkthrough()
    {
        if (cameraModes == null) return;
        if (cameraModes.CurrentMode == AuditoriumCameraMode.Walkthrough) cameraModes.ShowPlanning();
        else cameraModes.ShowWalkthrough();
    }

    private void ShowPlanning()
    {
        if (cameraModes != null) cameraModes.ShowPlanning();
    }

    private void UpdateModeDisplay(AuditoriumCameraMode mode)
    {
        bool isWalkthrough = mode == AuditoriumCameraMode.Walkthrough;
        Text walkthroughLabel = walkthroughButton != null ? walkthroughButton.GetComponentInChildren<Text>() : null;
        if (walkthroughLabel != null) walkthroughLabel.text = isWalkthrough ? "Exit Walkthrough" : "Enter Walkthrough";
        SetButtonColor(walkthroughButton, isWalkthrough ? activeButtonColor : idleButtonColor);
        SetButtonColor(planningButton, isWalkthrough ? idleButtonColor : activeButtonColor);
        if (walkthroughOverlay != null) walkthroughOverlay.SetActive(isWalkthrough);
        if (catalogPanel != null) catalogPanel.SetActive(!isWalkthrough);
        if (statusPanel != null) statusPanel.SetActive(!isWalkthrough);
        if (placementSystem == null || !placementSystem.IsPlacing)
        {
            SetStatus(isWalkthrough
                ? "WALKTHROUGH  •  Hold right mouse and drag to look"
                : "PLANNING VIEW  •  Drag to orbit, scroll to zoom");
        }
    }

    private void UpdatePlacementInstructions(string selectedItem)
    {
        bool placing = placementSystem != null && placementSystem.IsPlacing;
        UpdateInteractionModeButton();
        if (selectedItemLabel != null)
            selectedItemLabel.text = placing
                ? selectedItem
                : (placementSystem != null && placementSystem.SelectedObject != null
                    ? "Selected: " + FurnitureCatalog.Get(placementSystem.SelectedObject.Type).DisplayName
                    : "No item selected");
        if (placementInstructionsLabel != null)
        {
            placementInstructionsLabel.text = placing
                ? (placementSystem.IsMoving
                    ? "Move the preview onto the floor.\nClick moves this item. Q/E rotate 15°.\nEscape restores its position."
                    : (placementSystem.IsDuplicating
                        ? "Position the duplicate preview.\nLeft click confirms; Q/E rotate 15°.\nCancel Duplicate returns to the original item."
                        : "Move the preview onto the floor.\nLeft click places repeatedly; Q/E rotates 15°.\nFinish Placing returns to Select / Edit mode."))
                : "Choose an item to place it, or click furniture to select and edit it.";
        }
        UpdateSelectedObject(placementSystem != null ? placementSystem.SelectedObject : null);
    }

    private void UpdateInteractionModeButton()
    {
        if (finishPlacingButton == null) return;
        bool active = placementSystem != null && placementSystem.IsPlacing;
        Text label = finishPlacingButton.GetComponentInChildren<Text>();
        if (label != null)
        {
            if (!active) label.text = "Select / Edit Mode";
            else if (placementSystem.IsMoving) label.text = "Cancel Move";
            else if (placementSystem.IsDuplicating) label.text = "Cancel Duplicate";
            else label.text = "Finish Placing";
        }
        finishPlacingButton.interactable = active;
        SetButtonColor(finishPlacingButton, active ? activeButtonColor : idleButtonColor);
    }

    private void UpdateSelectedObject(FurnitureInstance instance)
    {
        if (selectedObjectLabel != null)
            selectedObjectLabel.text = instance != null ? FurnitureCatalog.Get(instance.Type).DisplayName : "Click placed furniture to select it";
        bool canEdit = instance != null && placementSystem != null && !placementSystem.IsPlacing;
        if (moveSelectedButton != null) moveSelectedButton.interactable = canEdit;
        if (rotateLeftButton != null) rotateLeftButton.interactable = canEdit;
        if (rotateRightButton != null) rotateRightButton.interactable = canEdit;
        if (deleteSelectedButton != null) deleteSelectedButton.interactable = canEdit;
        UpdateHistoryButtons();
        UpdateLayoutInfo();
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null) statusLabel.text = message;
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        button.colors = colors;
    }
}
