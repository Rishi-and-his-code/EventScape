using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

/// <summary>Owns furniture selection, new placement, and preview-based editing.</summary>
public sealed class PlacementSystem : MonoBehaviour
{
    private enum InteractionState { SelectEdit, PlaceNew, MoveExisting }

    private sealed class LayoutHistoryEntry
    {
        public FurniturePlacementRecord before;
        public bool hadBefore;
        public FurniturePlacementRecord after;
        public bool hasAfter;
        public bool isWholeLayout;
        public List<FurniturePlacementRecord> beforeLayout;
        public List<FurniturePlacementRecord> afterLayout;
        public string description;
    }

    [SerializeField] private CameraModeController cameraModes;
    [SerializeField] private AuditoriumDemoSettings settings;
    [SerializeField] private AuditoriumMaterialLibrary materials;
    [SerializeField] private Transform placedObjectsRoot;
    [SerializeField, Min(1f)] private float rayDistance = 200f;
    [SerializeField, Min(1f)] private float rotationStepDegrees = 15f;
    [SerializeField] private bool gridSnappingEnabled = true;
    [SerializeField] private float gridIncrement = 0.5f;
    [SerializeField, Min(0.25f)] private float duplicateOffset = 2f;
    [SerializeField, Min(1)] private int maximumHistoryEntries = 100;

    private readonly RaycastHit[] raycastHits = new RaycastHit[256];
    private readonly Collider[] overlapResults = new Collider[256];
    private readonly List<LayoutHistoryEntry> undoHistory = new List<LayoutHistoryEntry>();
    private readonly List<LayoutHistoryEntry> redoHistory = new List<LayoutHistoryEntry>();
    private InteractionState state;
    private FurnitureType selectedType;
    private FurnitureInstance selectedObject;
    private FurnitureInstance editedObject;
    private GameObject previewObject;
    private Renderer[] previewRenderers;
    private Material[][] previewOriginalMaterials;
    private bool[] previewRendererWasEnabled;
    private bool[] previewRendererReceivedShadows;
    private ShadowCastingMode[] previewShadowModes;
    private Collider[] previewColliders;
    private bool[] previewColliderWasEnabled;
    private Material validPreviewMaterial;
    private Material invalidPreviewMaterial;
    private Material selectionMaterial;
    private LineRenderer[] selectionEdges;
    private float previewYaw;
    private Vector3 originalMovePosition;
    private Quaternion originalMoveRotation;
    private FurniturePlacementRecord originalMoveRecord;
    private bool duplicateMode;
    private bool previewIsValid;
    private bool previewHasFloorHit;
    private bool physicsStateDirty = true;
    private string currentMessage;

    public event Action<string> FeedbackChanged;
    public event Action<string> SelectionChanged;
    public event Action<FurnitureInstance> SelectedObjectChanged;
    public event Action HistoryChanged;
    public event Action LayoutChanged;
    public event Action GridSettingsChanged;

    public bool IsPlacing { get { return state != InteractionState.SelectEdit; } }
    public bool IsMoving { get { return state == InteractionState.MoveExisting; } }
    public bool IsDuplicating { get { return duplicateMode; } }
    public bool IsSelectEditMode { get { return state == InteractionState.SelectEdit; } }
    public FurnitureType SelectedType { get { return selectedType; } }
    public FurnitureInstance SelectedObject { get { return selectedObject; } }
    public AuditoriumDemoSettings Settings { get { return settings; } }
    public bool GridSnappingEnabled { get { return gridSnappingEnabled; } }
    public float GridIncrement { get { return gridIncrement; } }
    public bool CanUndo { get { return undoHistory.Count > 0; } }
    public bool CanRedo { get { return redoHistory.Count > 0; } }

    public void Configure(CameraModeController modeController, AuditoriumDemoSettings demoSettings,
        AuditoriumMaterialLibrary materialLibrary, Transform placedObjects)
    {
        cameraModes = modeController;
        settings = demoSettings;
        materials = materialLibrary;
        placedObjectsRoot = placedObjects;
    }

    public int PlacedFurnitureCount
    {
        get { return GetPlacedFurniture().Length + (state == InteractionState.MoveExisting && editedObject != null ? 1 : 0); }
    }

    public void SetGridSnapping(bool enabled)
    {
        if (gridSnappingEnabled == enabled) return;
        gridSnappingEnabled = enabled;
        GridSettingsChanged?.Invoke();
    }

    public void SetGridIncrement(float increment)
    {
        if (Mathf.Approximately(increment, 0.25f)) gridIncrement = 0.25f;
        else if (Mathf.Approximately(increment, 0.5f)) gridIncrement = 0.5f;
        else if (Mathf.Approximately(increment, 1f)) gridIncrement = 1f;
        else return;

        GridSettingsChanged?.Invoke();
    }

    public void DuplicateSelected()
    {
        if (selectedObject == null || state != InteractionState.SelectEdit || materials == null || placedObjectsRoot == null || cameraModes == null)
        {
            SetFeedback("Select an item before duplicating it.");
            return;
        }

        FurnitureInstance source = selectedObject;
        selectedType = source.Type;
        previewYaw = source.transform.eulerAngles.y;
        state = InteractionState.PlaceNew;
        duplicateMode = true;
        cameraModes.SetPlacementActive(true);
        GameObject duplicatePreview = FurnitureFactory.Create(selectedType, transform, materials, true);
        duplicatePreview.transform.localScale = source.transform.localScale;
        Vector3 proposedPosition = source.transform.position + source.transform.right * duplicateOffset;
        duplicatePreview.transform.SetPositionAndRotation(proposedPosition, source.transform.rotation);
        CreatePreview(duplicatePreview);
        previewHasFloorHit = true;
        string reason = PlacementValidator.GetInvalidReason(duplicatePreview.GetComponent<FurnitureInstance>(), selectedType,
            proposedPosition, previewYaw, settings, placedObjectsRoot, null, overlapResults, ref physicsStateDirty);
        previewIsValid = string.IsNullOrEmpty(reason);
        UpdatePreviewMaterial();
        SetPreviewVisible(true);
        SetSelectionIndicatorVisible(false);
        SelectionChanged?.Invoke("Duplicating: " + FurnitureCatalog.Get(selectedType).DisplayName);
        SetFeedback(previewIsValid
            ? "Duplicate preview is valid. Move it or click to place; Escape cancels."
            : "Duplicate preview is invalid: " + reason);
        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (state != InteractionState.SelectEdit) CancelPlacement();
        if (undoHistory.Count == 0)
        {
            SetFeedback("Nothing to undo.");
            NotifyHistoryChanged();
            return;
        }

        int lastIndex = undoHistory.Count - 1;
        LayoutHistoryEntry entry = undoHistory[lastIndex];
        string failureReason = null;
        bool restored = entry.isWholeLayout
            ? TryReplacePlacedFurniture(entry.beforeLayout, out failureReason, false)
            : ApplyHistoryState(entry.before, entry.hadBefore, entry.after);
        if (!restored)
        {
            SetFeedback("Undo could not restore the previous furniture state." +
                (string.IsNullOrEmpty(failureReason) ? string.Empty : " " + failureReason));
            return;
        }
        undoHistory.RemoveAt(lastIndex);
        redoHistory.Add(entry);
        NotifyHistoryChanged();
        LayoutChanged?.Invoke();
        RefreshSelectedObjectAfterHistory();
        SetFeedback("Undid " + entry.description + ".");
    }

    public void Redo()
    {
        if (state != InteractionState.SelectEdit) CancelPlacement();
        if (redoHistory.Count == 0)
        {
            SetFeedback("Nothing to redo.");
            NotifyHistoryChanged();
            return;
        }

        int lastIndex = redoHistory.Count - 1;
        LayoutHistoryEntry entry = redoHistory[lastIndex];
        string failureReason = null;
        bool restored = entry.isWholeLayout
            ? TryReplacePlacedFurniture(entry.afterLayout, out failureReason, false)
            : ApplyHistoryState(entry.after, entry.hasAfter, entry.before);
        if (!restored)
        {
            SetFeedback("Redo could not restore the next furniture state." +
                (string.IsNullOrEmpty(failureReason) ? string.Empty : " " + failureReason));
            return;
        }
        redoHistory.RemoveAt(lastIndex);
        undoHistory.Add(entry);
        NotifyHistoryChanged();
        LayoutChanged?.Invoke();
        RefreshSelectedObjectAfterHistory();
        SetFeedback("Redid " + entry.description + ".");
    }

    public void BeginPlacement(FurnitureType type)
    {
        if (settings == null || materials == null || placedObjectsRoot == null || cameraModes == null)
        {
            SetFeedback("Placement is unavailable: scene references are missing.");
            return;
        }

        EndActiveOperation(true);
        ClearSelectedObject();
        selectedType = type;
        duplicateMode = false;
        previewYaw = 0f;
        state = InteractionState.PlaceNew;
        previewHasFloorHit = false;
        previewIsValid = false;
        cameraModes.SetPlacementActive(true);
        CreatePreview(FurnitureFactory.Create(type, transform, materials, true));
        SelectionChanged?.Invoke("Placing: " + FurnitureCatalog.Get(type).DisplayName);
        SetFeedback("Move the preview onto the floor. Left click places; Q/E rotate; Escape cancels.");
        HistoryChanged?.Invoke();
    }

    public void MoveSelected()
    {
        if (selectedObject == null || state != InteractionState.SelectEdit) return;
        editedObject = selectedObject;
        originalMovePosition = editedObject.transform.position;
        originalMoveRotation = editedObject.transform.rotation;
        originalMoveRecord = CapturePlacement(editedObject);
        duplicateMode = false;
        selectedType = editedObject.Type;
        previewYaw = originalMoveRotation.eulerAngles.y;
        state = InteractionState.MoveExisting;
        previewObject = editedObject.gameObject;
        FurnitureInstance marker = previewObject.GetComponent<FurnitureInstance>();
        if (marker != null) marker.SetPreview(true);
        PreparePreviewObject();
        if (cameraModes != null) cameraModes.SetPlacementActive(true);
        SetSelectionIndicatorVisible(false);
        SelectionChanged?.Invoke("Moving: " + FurnitureCatalog.Get(selectedType).DisplayName);
        SetFeedback("Choose a valid floor position. Click to move; Escape restores the original position.");
        HistoryChanged?.Invoke();
    }

    public void RotateSelected(float direction)
    {
        if (selectedObject == null || state != InteractionState.SelectEdit) return;
        FurniturePlacementRecord before = CapturePlacement(selectedObject);
        float proposedYaw = Mathf.Repeat(selectedObject.transform.eulerAngles.y + Mathf.Sign(direction) * rotationStepDegrees, 360f);
        string invalidReason = PlacementValidator.GetInvalidReason(selectedObject, selectedObject.Type,
            selectedObject.transform.position, proposedYaw, settings, placedObjectsRoot,
            selectedObject, overlapResults, ref physicsStateDirty);
        if (!string.IsNullOrEmpty(invalidReason))
        {
            SetFeedback("Rotation blocked: " + invalidReason);
            return;
        }
        selectedObject.transform.rotation = Quaternion.Euler(0f, proposedYaw, 0f);
        physicsStateDirty = true;
        RecordChange(before, true, CapturePlacement(selectedObject), true, "rotation");
        UpdateSelectionIndicator();
        LayoutChanged?.Invoke();
        SetFeedback("Rotated " + FurnitureCatalog.Get(selectedObject.Type).DisplayName + " by 15°.");
    }

    public void DeleteSelected()
    {
        if (selectedObject == null) return;
        FurnitureInstance deleted = selectedObject;
        FurniturePlacementRecord before = CapturePlacement(deleted);
        ClearSelectedObject();
        if (deleted != null)
        {
            deleted.gameObject.SetActive(false);
            Destroy(deleted.gameObject);
        }
        physicsStateDirty = true;
        RecordChange(before, true, default(FurniturePlacementRecord), false, "deletion");
        LayoutChanged?.Invoke();
        SetFeedback("Furniture deleted.");
    }

    public FurnitureInstance[] GetPlacedFurniture()
    {
        if (placedObjectsRoot == null) return new FurnitureInstance[0];
        List<FurnitureInstance> instances = new List<FurnitureInstance>();
        for (int i = 0; i < placedObjectsRoot.childCount; i++)
        {
            FurnitureInstance instance = placedObjectsRoot.GetChild(i).GetComponent<FurnitureInstance>();
            if (instance != null && instance.gameObject.activeInHierarchy && !instance.IsPreview) instances.Add(instance);
        }
        return instances.ToArray();
    }

    /// <summary>Replaces the current layout only after every record passes validation.</summary>
    public bool TryReplacePlacedFurniture(IList<FurniturePlacementRecord> placements, out string failureReason)
    {
        return TryReplacePlacedFurniture(placements, out failureReason, true);
    }

    private bool TryReplacePlacedFurniture(IList<FurniturePlacementRecord> placements, out string failureReason,
        bool clearHistoryOnSuccess)
    {
        failureReason = null;
        if (placements == null || placedObjectsRoot == null || settings == null || materials == null)
        {
            failureReason = "The scene is not ready to load a layout.";
            return false;
        }

        EndActiveOperation(true);
        ClearSelectedObject();
        FurnitureInstance[] oldInstances = GetPlacedFurniture();
        for (int i = 0; i < oldInstances.Length; i++) oldInstances[i].gameObject.SetActive(false);

        List<FurnitureInstance> staged = new List<FurnitureInstance>();
        HashSet<string> stagedInstanceIds = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            for (int i = 0; i < placements.Count; i++)
            {
                FurniturePlacementRecord record = placements[i];
                FurnitureDefinition definition;
                if (!FurnitureCatalog.TryGetById(record.furnitureId, out definition))
                {
                    failureReason = "Layout item " + (i + 1) + " uses an unknown furniture type.";
                    break;
                }

                GameObject item = FurnitureFactory.Create(definition.Type, placedObjectsRoot, materials, false);
                item.transform.SetPositionAndRotation(record.position, Quaternion.Euler(record.rotationEuler));
                item.transform.localScale = record.scale;
                item.name = definition.DisplayName;
                FurnitureInstance instance = item.GetComponent<FurnitureInstance>();
                if (!string.IsNullOrEmpty(record.instanceId))
                {
                    if (!stagedInstanceIds.Add(record.instanceId))
                    {
                        failureReason = "Layout contains duplicate furniture instance identifiers.";
                        staged.Add(instance);
                        break;
                    }
                    instance.SetInstanceId(record.instanceId);
                }
                else
                {
                    string generatedId;
                    do { generatedId = Guid.NewGuid().ToString("N"); }
                    while (!stagedInstanceIds.Add(generatedId));
                    instance.SetInstanceId(generatedId);
                }
                staged.Add(instance);

                physicsStateDirty = true;
                float invalidYaw = record.rotationEuler.y;
                failureReason = PlacementValidator.GetInvalidReason(instance, definition.Type, record.position,
                    invalidYaw, settings, placedObjectsRoot, instance, overlapResults, ref physicsStateDirty);
                if (!string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "Layout item " + (i + 1) + " is invalid: " + failureReason;
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            failureReason = "Could not build the loaded layout: " + exception.Message;
        }

        if (!string.IsNullOrEmpty(failureReason))
        {
            for (int i = 0; i < staged.Count; i++)
            {
                if (staged[i] != null)
                {
                    staged[i].gameObject.SetActive(false);
                    Destroy(staged[i].gameObject);
                }
            }
            for (int i = 0; i < oldInstances.Length; i++)
                if (oldInstances[i] != null) oldInstances[i].gameObject.SetActive(true);
            physicsStateDirty = true;
            return false;
        }

        for (int i = 0; i < oldInstances.Length; i++)
            if (oldInstances[i] != null) Destroy(oldInstances[i].gameObject);
        physicsStateDirty = true;
        if (clearHistoryOnSuccess) ClearHistory();
        LayoutChanged?.Invoke();
        return true;
    }

    /// <summary>Atomically applies a generated template and records it as one undoable action.</summary>
    public bool TryApplyTemplate(IList<FurniturePlacementRecord> placements, bool replaceCurrentLayout,
        out string failureReason)
    {
        failureReason = null;
        if (placements == null || placements.Count == 0 || placedObjectsRoot == null
            || settings == null || materials == null)
        {
            failureReason = "The template is empty or the scene is not ready.";
            return false;
        }
        if (placements.Count > 500)
        {
            failureReason = "This template exceeds the safe item limit.";
            return false;
        }

        if (state != InteractionState.SelectEdit) CancelPlacement();
        List<FurniturePlacementRecord> before = CapturePlacedRecords();
        if (replaceCurrentLayout)
        {
            if (!TryReplacePlacedFurniture(placements, out failureReason, false)) return false;
        }
        else
        {
            if (!TryAppendTemplatePlacements(placements, out failureReason)) return false;
        }

        List<FurniturePlacementRecord> after = CapturePlacedRecords();
        RecordWholeLayoutChange(before, after, replaceCurrentLayout ? "template replacement" : "template addition");
        ClearSelectedObject();
        state = InteractionState.SelectEdit;
        duplicateMode = false;
        if (cameraModes != null) cameraModes.SetPlacementActive(false);
        LayoutChanged?.Invoke();
        SelectionChanged?.Invoke("Select / Edit mode");
        SetFeedback("Applied template with " + placements.Count + " new item(s). Select / Edit mode.");
        return true;
    }

    private bool TryAppendTemplatePlacements(IList<FurniturePlacementRecord> placements, out string failureReason)
    {
        failureReason = null;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        FurnitureInstance[] current = GetPlacedFurniture();
        if (current.Length + placements.Count > 1000)
        {
            failureReason = "The combined layout would exceed 1000 furniture items.";
            return false;
        }
        for (int i = 0; i < current.Length; i++) ids.Add(current[i].InstanceId);
        List<FurnitureInstance> created = new List<FurnitureInstance>();
        try
        {
            for (int i = 0; i < placements.Count; i++)
            {
                FurniturePlacementRecord record = placements[i];
                FurnitureDefinition definition;
                if (!FurnitureCatalog.TryGetById(record.furnitureId, out definition))
                {
                    failureReason = "Item " + (i + 1) + " uses an unknown furniture type.";
                    break;
                }
                string id = record.instanceId;
                if (string.IsNullOrEmpty(id) || !ids.Add(id))
                {
                    do { id = Guid.NewGuid().ToString("N"); } while (!ids.Add(id));
                }

                GameObject item = FurnitureFactory.Create(definition.Type, placedObjectsRoot, materials, false);
                item.name = definition.DisplayName;
                item.transform.SetPositionAndRotation(record.position, Quaternion.Euler(record.rotationEuler));
                item.transform.localScale = record.scale;
                FurnitureInstance instance = item.GetComponent<FurnitureInstance>();
                instance.SetInstanceId(id);
                created.Add(instance);
                physicsStateDirty = true;
                failureReason = PlacementValidator.GetInvalidReason(instance, definition.Type, record.position,
                    record.rotationEuler.y, settings, placedObjectsRoot, instance, overlapResults, ref physicsStateDirty);
                if (!string.IsNullOrEmpty(failureReason))
                {
                    failureReason = "Template item " + (i + 1) + " is invalid: " + failureReason;
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            failureReason = "Could not stage the template: " + exception.Message;
        }

        if (string.IsNullOrEmpty(failureReason)) return true;
        for (int i = 0; i < created.Count; i++)
        {
            if (created[i] == null) continue;
            created[i].gameObject.SetActive(false);
            Destroy(created[i].gameObject);
        }
        physicsStateDirty = true;
        Physics.SyncTransforms();
        return false;
    }

    public void ResetPlacedFurniture()
    {
        EndActiveOperation(true);
        ClearSelectedObject();
        FurnitureInstance[] instances = GetPlacedFurniture();
        for (int i = 0; i < instances.Length; i++)
        {
            instances[i].gameObject.SetActive(false);
            Destroy(instances[i].gameObject);
        }
        physicsStateDirty = true;
        ClearHistory();
        LayoutChanged?.Invoke();
    }

    public void CancelPlacement()
    {
        if (state == InteractionState.SelectEdit) return;
        bool wasMove = state == InteractionState.MoveExisting;
        bool wasDuplicate = duplicateMode;
        EndActiveOperation(true);
        if (wasMove)
        {
            if (selectedObject != null) UpdateSelectionIndicator();
            SelectionChanged?.Invoke(selectedObject != null ? "Selected: " + FurnitureCatalog.Get(selectedObject.Type).DisplayName : "No object selected");
            SetFeedback("Move cancelled. Original position restored.");
        }
        else if (wasDuplicate)
        {
            if (selectedObject != null) UpdateSelectionIndicator();
            SelectionChanged?.Invoke(selectedObject != null
                ? "Selected: " + FurnitureCatalog.Get(selectedObject.Type).DisplayName
                : "No object selected");
            SetFeedback("Duplicate cancelled. Original item was kept.");
        }
        else
        {
            ClearSelectedObject();
            SelectionChanged?.Invoke("No object selected");
            SetFeedback("Placement cancelled.");
        }
        HistoryChanged?.Invoke();
    }

    /// <summary>Finishes a repeated placement session and returns to object selection.</summary>
    public void FinishPlacing()
    {
        if (state == InteractionState.SelectEdit)
        {
            SetFeedback("Select / Edit mode. Click placed furniture to select it.");
            return;
        }

        if (state == InteractionState.MoveExisting || duplicateMode)
        {
            CancelPlacement();
            return;
        }

        EndActiveOperation(true);
        ClearSelectedObject();
        SelectionChanged?.Invoke("Select / Edit mode");
        SetFeedback("Select / Edit mode. Click placed furniture to select it.");
        HistoryChanged?.Invoke();
    }

    private void Update()
    {
        if (cameraModes != null && cameraModes.CurrentMode != AuditoriumCameraMode.Planning) return;
        if (state != InteractionState.SelectEdit)
        {
            UpdateActiveOperation();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.GetMouseButtonDown(0)) SelectFromPointer();
    }

    private void UpdateActiveOperation()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            previewIsValid = false;
            UpdatePreviewMaterial();
            SetPreviewVisible(false);
            SetFeedback("Move the pointer over the auditorium to position furniture.");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q)) previewYaw -= rotationStepDegrees;
        if (Input.GetKeyDown(KeyCode.E)) previewYaw += rotationStepDegrees;
        previewYaw = Mathf.Repeat(previewYaw, 360f);
        UpdatePreviewFromPointer();
        if (Input.GetMouseButtonDown(0) && previewIsValid && previewHasFloorHit) ConfirmActiveOperation();
    }

    private void OnDisable()
    {
        if (state != InteractionState.SelectEdit) EndActiveOperation(true);
        ClearSelectedObject();
    }

    private void OnDestroy()
    {
        DestroyOwnedMaterial(validPreviewMaterial);
        DestroyOwnedMaterial(invalidPreviewMaterial);
        DestroyOwnedMaterial(selectionMaterial);
    }

    private void CreatePreview(GameObject instance)
    {
        previewObject = instance;
        PreparePreviewObject();
        EnsurePreviewMaterials();
        UpdatePreviewMaterial();
    }

    private void PreparePreviewObject()
    {
        if (previewObject == null) return;
        previewRenderers = previewObject.GetComponentsInChildren<Renderer>(true);
        previewOriginalMaterials = new Material[previewRenderers.Length][];
        previewRendererWasEnabled = new bool[previewRenderers.Length];
        previewRendererReceivedShadows = new bool[previewRenderers.Length];
        previewShadowModes = new ShadowCastingMode[previewRenderers.Length];
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            previewOriginalMaterials[i] = previewRenderers[i].sharedMaterials;
            previewRendererWasEnabled[i] = previewRenderers[i].enabled;
            previewRendererReceivedShadows[i] = previewRenderers[i].receiveShadows;
            previewShadowModes[i] = previewRenderers[i].shadowCastingMode;
            previewRenderers[i].shadowCastingMode = ShadowCastingMode.Off;
            previewRenderers[i].receiveShadows = false;
        }

        previewColliders = previewObject.GetComponentsInChildren<Collider>(true);
        previewColliderWasEnabled = new bool[previewColliders.Length];
        for (int i = 0; i < previewColliders.Length; i++)
        {
            previewColliderWasEnabled[i] = previewColliders[i].enabled;
            previewColliders[i].enabled = false;
        }
        EnsurePreviewMaterials();
        UpdatePreviewMaterial();
    }

    private void RestorePreviewObject()
    {
        if (previewObject == null) return;
        FurnitureInstance marker = previewObject.GetComponent<FurnitureInstance>();
        if (marker != null) marker.SetPreview(false);
        for (int i = 0; previewRenderers != null && i < previewRenderers.Length; i++)
        {
            if (previewRenderers[i] == null) continue;
            previewRenderers[i].sharedMaterials = previewOriginalMaterials[i];
            previewRenderers[i].shadowCastingMode = previewShadowModes[i];
            previewRenderers[i].receiveShadows = previewRendererReceivedShadows[i];
            previewRenderers[i].enabled = previewRendererWasEnabled[i];
        }
        for (int i = 0; previewColliders != null && i < previewColliders.Length; i++)
            if (previewColliders[i] != null) previewColliders[i].enabled = previewColliderWasEnabled[i];
        physicsStateDirty = true;
    }

    private void ClearPreviewReferences()
    {
        previewObject = null;
        editedObject = null;
        previewRenderers = null;
        previewOriginalMaterials = null;
        previewRendererWasEnabled = null;
        previewRendererReceivedShadows = null;
        previewShadowModes = null;
        previewColliders = null;
        previewColliderWasEnabled = null;
    }

    private void EndActiveOperation(bool restoreMove)
    {
        if (state == InteractionState.MoveExisting)
        {
            if (restoreMove && previewObject != null)
                previewObject.transform.SetPositionAndRotation(originalMovePosition, originalMoveRotation);
            RestorePreviewObject();
            ClearPreviewReferences();
        }
        else if (state == InteractionState.PlaceNew)
        {
            if (previewObject != null) Destroy(previewObject);
            ClearPreviewReferences();
        }
        state = InteractionState.SelectEdit;
        duplicateMode = false;
        previewHasFloorHit = false;
        previewIsValid = false;
        if (cameraModes != null) cameraModes.SetPlacementActive(false);
    }

    private void UpdatePreviewFromPointer()
    {
        Camera activeCamera = cameraModes != null ? cameraModes.ActiveCamera : null;
        if (activeCamera == null || previewObject == null)
        {
            previewHasFloorHit = false;
            previewIsValid = false;
            UpdatePreviewMaterial();
            SetPreviewVisible(false);
            SetFeedback("No active camera is available for placement.");
            return;
        }

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits,
            Mathf.Min(rayDistance, activeCamera.farClipPlane), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool hasFloor = false;
        bool hasRelevant = false;
        RaycastHit nearestFloor = new RaycastHit();
        RaycastHit nearestRelevant = new RaycastHit();
        float nearestFloorDistance = float.MaxValue;
        float nearestRelevantDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = raycastHits[i].collider;
            if (hitCollider == null) continue;
            FurnitureInstance furniture = hitCollider.GetComponentInParent<FurnitureInstance>();
            if (furniture != null) continue;
            if (raycastHits[i].distance < nearestRelevantDistance)
            {
                nearestRelevantDistance = raycastHits[i].distance;
                nearestRelevant = raycastHits[i];
                hasRelevant = true;
            }
            if (hitCollider.GetComponentInParent<PlacementSurface>() != null && raycastHits[i].distance < nearestFloorDistance)
            {
                nearestFloorDistance = raycastHits[i].distance;
                nearestFloor = raycastHits[i];
                hasFloor = true;
            }
        }

        if (hasRelevant && nearestRelevant.collider.GetComponentInParent<PlacementSurface>() == null)
        {
            previewHasFloorHit = false;
            previewIsValid = false;
            if (hasFloor)
            {
                SetPreviewPose(SnapToGrid(nearestFloor.point), Quaternion.Euler(0f, previewYaw, 0f));
                SetPreviewVisible(true);
            }
            else SetPreviewVisible(false);
            UpdatePreviewMaterial();
            SetFeedback("Aim at open floor or stage-top surface; walls, steps, and fixed objects block placement.");
            return;
        }

        if (!hasFloor)
        {
            previewHasFloorHit = false;
            previewIsValid = false;
            UpdatePreviewMaterial();
            SetPreviewVisible(false);
            SetFeedback("Aim the camera at the auditorium floor.");
            return;
        }

        previewHasFloorHit = true;
        Vector3 previewPosition = SnapToGrid(nearestFloor.point);
        SetPreviewPose(previewPosition, Quaternion.Euler(0f, previewYaw, 0f));
        SetPreviewVisible(true);
        string reason = PlacementValidator.GetInvalidReason(previewObject.GetComponent<FurnitureInstance>(), selectedType, previewPosition,
            previewYaw, settings, placedObjectsRoot, editedObject, overlapResults, ref physicsStateDirty);
        previewIsValid = string.IsNullOrEmpty(reason);
        UpdatePreviewMaterial();
        SetFeedback(previewIsValid
            ? (state == InteractionState.MoveExisting ? "Valid position. Click moves this item; Escape cancels." : "Valid position. Left click places; Q/E rotate 15°; Escape cancels.")
            : reason);
    }

    private void ConfirmActiveOperation()
    {
        if (!previewIsValid || previewObject == null) return;
        if (state == InteractionState.MoveExisting)
        {
            FurnitureInstance moved = editedObject;
            FurniturePlacementRecord after = CapturePlacement(moved);
            RestorePreviewObject();
            ClearPreviewReferences();
            state = InteractionState.SelectEdit;
            duplicateMode = false;
            selectedObject = moved;
            if (cameraModes != null) cameraModes.SetPlacementActive(false);
            RecordChange(originalMoveRecord, true, after, true, "move");
            UpdateSelectionIndicator();
            SelectionChanged?.Invoke("Selected: " + FurnitureCatalog.Get(moved.Type).DisplayName);
            SelectedObjectChanged?.Invoke(moved);
            LayoutChanged?.Invoke();
            HistoryChanged?.Invoke();
            SetFeedback("Moved " + FurnitureCatalog.Get(moved.Type).DisplayName + ".");
            return;
        }

        FurnitureDefinition definition = FurnitureCatalog.Get(selectedType);
        FurnitureInstance instance = previewObject.GetComponent<FurnitureInstance>();
        if (instance != null) instance.SetPreview(false);
        previewObject.name = definition.DisplayName;
        previewObject.transform.SetParent(placedObjectsRoot, true);
        RestorePreviewObject();
        FurniturePlacementRecord placedRecord = CapturePlacement(instance);
        bool wasDuplicate = duplicateMode;
        ClearPreviewReferences();
        physicsStateDirty = true;
        RecordChange(default(FurniturePlacementRecord), false, placedRecord, true, wasDuplicate ? "duplication" : "placement");
        LayoutChanged?.Invoke();
        HistoryChanged?.Invoke();
        if (wasDuplicate)
        {
            state = InteractionState.SelectEdit;
            duplicateMode = false;
            selectedObject = instance;
            if (cameraModes != null) cameraModes.SetPlacementActive(false);
            UpdateSelectionIndicator();
            SelectionChanged?.Invoke("Selected: " + definition.DisplayName);
            SelectedObjectChanged?.Invoke(instance);
            SetFeedback("Duplicated " + definition.DisplayName + " with a unique ID.");
            return;
        }
        SetFeedback("Placed " + definition.DisplayName + ". Click to add another or press Escape to finish.");
        CreatePreview(FurnitureFactory.Create(selectedType, transform, materials, true));
        SelectionChanged?.Invoke("Placing: " + definition.DisplayName);
    }

    private void SelectFromPointer()
    {
        Camera activeCamera = cameraModes != null ? cameraModes.ActiveCamera : null;
        if (activeCamera == null) return;
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, Mathf.Min(rayDistance, activeCamera.farClipPlane),
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        FurnitureInstance nearest = null;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            if (raycastHits[i].distance < nearestDistance)
            {
                nearestDistance = raycastHits[i].distance;
                Collider hit = raycastHits[i].collider;
                FurnitureInstance candidate = hit != null ? hit.GetComponentInParent<FurnitureInstance>() : null;
                nearest = candidate != null && !candidate.IsPreview && candidate.transform.parent == placedObjectsRoot
                    ? candidate : null;
            }
        }
        SetSelectedObject(nearest);
    }

    private void SetSelectedObject(FurnitureInstance instance)
    {
        if (selectedObject == instance)
        {
            if (instance == null) SetFeedback("No furniture selected.");
            return;
        }
        selectedObject = instance;
        UpdateSelectionIndicator();
        string label = instance != null ? "Selected: " + FurnitureCatalog.Get(instance.Type).DisplayName : "No object selected";
        SelectionChanged?.Invoke(label);
        SelectedObjectChanged?.Invoke(instance);
        LayoutChanged?.Invoke();
        SetFeedback(instance != null
            ? "Selected " + FurnitureCatalog.Get(instance.Type).DisplayName + ". Move, rotate, or delete it from the catalog panel."
            : "Selection cleared.");
    }

    private void ClearSelectedObject()
    {
        selectedObject = null;
        SetSelectionIndicatorVisible(false);
        SelectionChanged?.Invoke("No object selected");
        SelectedObjectChanged?.Invoke(null);
        LayoutChanged?.Invoke();
    }

    private void UpdateSelectionIndicator()
    {
        if (selectedObject == null)
        {
            SetSelectionIndicatorVisible(false);
            return;
        }
        EnsureSelectionIndicator();
        Bounds bounds;
        if (!TryGetRendererBounds(selectedObject.transform, out bounds))
        {
            SetSelectionIndicatorVisible(false);
            return;
        }
        Vector3 min = bounds.min - Vector3.one * 0.045f;
        Vector3 max = bounds.max + Vector3.one * 0.045f;
        Vector3[] c =
        {
            new Vector3(min.x,min.y,min.z), new Vector3(max.x,min.y,min.z),
            new Vector3(max.x,min.y,max.z), new Vector3(min.x,min.y,max.z),
            new Vector3(min.x,max.y,min.z), new Vector3(max.x,max.y,min.z),
            new Vector3(max.x,max.y,max.z), new Vector3(min.x,max.y,max.z)
        };
        int[,] edges = { {0,1},{1,2},{2,3},{3,0},{4,5},{5,6},{6,7},{7,4},{0,4},{1,5},{2,6},{3,7} };
        for (int i = 0; i < selectionEdges.Length; i++)
        {
            selectionEdges[i].SetPosition(0, c[edges[i, 0]]);
            selectionEdges[i].SetPosition(1, c[edges[i, 1]]);
            selectionEdges[i].enabled = true;
        }
    }

    private bool EnsureSelectionIndicator()
    {
        if (selectionEdges != null) return true;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return false;
        selectionMaterial = new Material(shader) { name = "Furniture Selection Outline" };
        Color color = new Color(0.25f, 0.9f, 0.78f, 1f);
        if (selectionMaterial.HasProperty("_BaseColor")) selectionMaterial.SetColor("_BaseColor", color);
        if (selectionMaterial.HasProperty("_Color")) selectionMaterial.SetColor("_Color", color);
        selectionEdges = new LineRenderer[12];
        for (int i = 0; i < selectionEdges.Length; i++)
        {
            GameObject lineObject = new GameObject("Selection Outline Edge " + (i + 1));
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.sharedMaterial = selectionMaterial;
            line.startColor = color;
            line.endColor = color;
            line.enabled = false;
            selectionEdges[i] = line;
        }
        return true;
    }

    private void SetSelectionIndicatorVisible(bool visible)
    {
        if (selectionEdges == null) return;
        for (int i = 0; i < selectionEdges.Length; i++) if (selectionEdges[i] != null) selectionEdges[i].enabled = visible;
    }

    private static bool TryGetRendererBounds(Transform target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        bounds = new Bounds();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled) continue;
            if (!hasBounds) { bounds = renderers[i].bounds; hasBounds = true; }
            else bounds.Encapsulate(renderers[i].bounds);
        }
        return hasBounds;
    }

    private void EnsurePreviewMaterials()
    {
        if (validPreviewMaterial == null) validPreviewMaterial = CreatePreviewMaterial("Placement Preview Valid", new Color(0.12f, 0.9f, 0.3f, 0.52f));
        if (invalidPreviewMaterial == null) invalidPreviewMaterial = CreatePreviewMaterial("Placement Preview Invalid", new Color(0.95f, 0.12f, 0.12f, 0.55f));
    }

    private static Material CreatePreviewMaterial(string materialName, Color tint)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) { Debug.LogError("Could not find a supported preview shader."); return null; }
        Material material = new Material(shader) { name = materialName, color = tint };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    private void UpdatePreviewMaterial()
    {
        if (previewRenderers == null) return;
        Material material = previewIsValid ? validPreviewMaterial : invalidPreviewMaterial;
        if (material == null) return;
        for (int i = 0; i < previewRenderers.Length; i++) previewRenderers[i].sharedMaterial = material;
    }

    private void SetPreviewVisible(bool visible)
    {
        if (previewRenderers == null) return;
        for (int i = 0; i < previewRenderers.Length; i++) previewRenderers[i].enabled = visible;
    }

    private static void DestroyOwnedMaterial(Material material)
    {
        if (material == null) return;
        if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
    }

    private void SetFeedback(string message)
    {
        if (currentMessage == message) return;
        currentMessage = message;
        FeedbackChanged?.Invoke(message);
    }

    private Vector3 SnapToGrid(Vector3 floorPosition)
    {
        if (!gridSnappingEnabled || gridIncrement <= 0f) return floorPosition;
        // The auditorium floor is centered on the world origin, so this keeps
        // grid lines stable when cameras or furniture are moved.
        floorPosition.x = Mathf.Round(floorPosition.x / gridIncrement) * gridIncrement;
        floorPosition.z = Mathf.Round(floorPosition.z / gridIncrement) * gridIncrement;
        return floorPosition;
    }

    private void SetPreviewPose(Vector3 position, Quaternion rotation)
    {
        if (previewObject == null) return;
        bool changed = (previewObject.transform.position - position).sqrMagnitude > 0.0025f;
        previewObject.transform.SetPositionAndRotation(position, rotation);
        if (changed && state == InteractionState.MoveExisting) LayoutChanged?.Invoke();
    }

    private static FurniturePlacementRecord CapturePlacement(FurnitureInstance instance)
    {
        FurniturePlacementRecord record = new FurniturePlacementRecord();
        record.instanceId = instance.InstanceId;
        record.furnitureId = FurnitureCatalog.Get(instance.Type).StableId;
        record.position = instance.transform.position;
        record.rotationEuler = instance.transform.rotation.eulerAngles;
        record.scale = instance.transform.localScale;
        return record;
    }

    private List<FurniturePlacementRecord> CapturePlacedRecords()
    {
        FurnitureInstance[] instances = GetPlacedFurniture();
        List<FurniturePlacementRecord> records = new List<FurniturePlacementRecord>(instances.Length);
        for (int i = 0; i < instances.Length; i++)
            if (instances[i] != null) records.Add(CapturePlacement(instances[i]));
        return records;
    }

    private void RecordWholeLayoutChange(List<FurniturePlacementRecord> before,
        List<FurniturePlacementRecord> after, string description)
    {
        maximumHistoryEntries = Mathf.Max(1, maximumHistoryEntries);
        LayoutHistoryEntry entry = new LayoutHistoryEntry();
        entry.isWholeLayout = true;
        entry.beforeLayout = before;
        entry.afterLayout = after;
        entry.description = description;
        undoHistory.Add(entry);
        if (undoHistory.Count > maximumHistoryEntries) undoHistory.RemoveAt(0);
        redoHistory.Clear();
        NotifyHistoryChanged();
    }

    private void RecordChange(FurniturePlacementRecord before, bool hadBefore,
        FurniturePlacementRecord after, bool hasAfter, string description)
    {
        maximumHistoryEntries = Mathf.Max(1, maximumHistoryEntries);
        LayoutHistoryEntry entry = new LayoutHistoryEntry();
        entry.before = before;
        entry.hadBefore = hadBefore;
        entry.after = after;
        entry.hasAfter = hasAfter;
        entry.description = description;
        undoHistory.Add(entry);
        if (undoHistory.Count > maximumHistoryEntries) undoHistory.RemoveAt(0);
        redoHistory.Clear();
        NotifyHistoryChanged();
    }

    private bool ApplyHistoryState(FurniturePlacementRecord target, bool targetExists,
        FurniturePlacementRecord other)
    {
        string instanceId = targetExists ? target.instanceId : other.instanceId;
        if (string.IsNullOrEmpty(instanceId)) return false;

        FurnitureInstance current = FindInstanceById(instanceId);
        if (!targetExists)
        {
            if (current != null)
            {
                if (selectedObject == current) ClearSelectedObject();
                current.gameObject.SetActive(false);
                Destroy(current.gameObject);
            }
            physicsStateDirty = true;
            return true;
        }

        FurnitureDefinition definition;
        if (!FurnitureCatalog.TryGetById(target.furnitureId, out definition)) return false;
        if (current != null && current.Type != definition.Type) return false;
        if (current == null)
        {
            if (FindInstanceById(instanceId) != null) return false;
            GameObject restored = FurnitureFactory.Create(definition.Type, placedObjectsRoot, materials, false);
            current = restored.GetComponent<FurnitureInstance>();
            current.SetInstanceId(instanceId);
            restored.name = definition.DisplayName;
        }

        current.transform.SetPositionAndRotation(target.position, Quaternion.Euler(target.rotationEuler));
        current.transform.localScale = target.scale;
        current.SetPreview(false);
        physicsStateDirty = true;
        return true;
    }

    private FurnitureInstance FindInstanceById(string instanceId)
    {
        if (placedObjectsRoot == null || string.IsNullOrEmpty(instanceId)) return null;
        for (int i = 0; i < placedObjectsRoot.childCount; i++)
        {
            FurnitureInstance instance = placedObjectsRoot.GetChild(i).GetComponent<FurnitureInstance>();
            if (instance != null && instance.gameObject.activeInHierarchy && !instance.IsPreview
                && string.Equals(instance.InstanceId, instanceId, StringComparison.Ordinal))
                return instance;
        }
        return null;
    }

    private void RefreshSelectedObjectAfterHistory()
    {
        if (selectedObject == null)
        {
            selectedObject = null;
            SetSelectionIndicatorVisible(false);
            SelectedObjectChanged?.Invoke(null);
            SelectionChanged?.Invoke("No object selected");
            return;
        }
        UpdateSelectionIndicator();
        SelectedObjectChanged?.Invoke(selectedObject);
        SelectionChanged?.Invoke("Selected: " + FurnitureCatalog.Get(selectedObject.Type).DisplayName);
    }

    private void ClearHistory()
    {
        undoHistory.Clear();
        redoHistory.Clear();
        NotifyHistoryChanged();
    }

    private void NotifyHistoryChanged()
    {
        HistoryChanged?.Invoke();
    }
}
