using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum AuditoriumCameraMode
{
    Walkthrough,
    Planning
}

/// <summary>
/// Switches between persistent first-person and orbit camera rigs.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class CameraModeController : MonoBehaviour
{
    [SerializeField] private WalkthroughCameraController walkthroughController;
    [SerializeField] private PlanningCameraController planningController;
    [SerializeField] private Camera walkthroughCamera;
    [SerializeField] private Camera planningCamera;
    [SerializeField] private GameObject ceilingRoot;
    [SerializeField] private AuditoriumCameraMode initialMode = AuditoriumCameraMode.Planning;
    [SerializeField] private bool placementActive;
    private PlacementSystem placementSystem;

    public event Action<AuditoriumCameraMode> ModeChanged;

    public AuditoriumCameraMode CurrentMode { get; private set; }
    public Camera ActiveCamera { get { return CurrentMode == AuditoriumCameraMode.Walkthrough ? walkthroughCamera : planningCamera; } }

    public void Configure(
        WalkthroughCameraController walkController,
        PlanningCameraController orbitController,
        Camera walkCamera,
        Camera orbitCamera,
        AuditoriumCameraMode mode = AuditoriumCameraMode.Planning)
    {
        walkthroughController = walkController;
        planningController = orbitController;
        walkthroughCamera = walkCamera;
        planningCamera = orbitCamera;
        initialMode = mode;
        ApplyMode(mode, false);
    }

    public void ConfigureCeiling(GameObject ceiling)
    {
        ceilingRoot = ceiling;
        ApplyCeilingVisibility();
    }

    public void ConfigurePlacementSystem(PlacementSystem placement)
    {
        placementSystem = placement;
    }

    public void SetInitialMode(AuditoriumCameraMode mode)
    {
        initialMode = mode;
        ApplyMode(mode, false);
    }

    private void Awake()
    {
        ApplyMode(initialMode, false);
    }

    private void Update()
    {
        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool focusedText = HasFocusedTextInput();
        if (CurrentMode == AuditoriumCameraMode.Walkthrough && !focusedText && Input.GetKeyDown(KeyCode.Escape))
        {
            ShowPlanning();
            return;
        }
        if (!placementActive && !pointerOverUi && !focusedText && Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMode();
        }
    }

    public void ShowWalkthrough()
    {
        SetMode(AuditoriumCameraMode.Walkthrough);
    }

    public void ShowPlanning()
    {
        SetMode(AuditoriumCameraMode.Planning);
    }

    public void ResetCamera()
    {
        if (CurrentMode == AuditoriumCameraMode.Walkthrough && walkthroughController != null)
            walkthroughController.ResetView();
        else if (planningController != null)
            planningController.ResetView();
    }

    public void ShowTopView()
    {
        ShowPlanning();
        if (planningController != null) planningController.SetTopView();
    }

    public void ShowFrontView()
    {
        ShowPlanning();
        if (planningController != null) planningController.SetFrontView();
    }

    public void ShowIsometricView()
    {
        ShowPlanning();
        if (planningController != null) planningController.SetIsometricView();
    }

    public void ToggleMode()
    {
        SetMode(CurrentMode == AuditoriumCameraMode.Walkthrough
            ? AuditoriumCameraMode.Planning
            : AuditoriumCameraMode.Walkthrough);
    }

    public void SetPlacementActive(bool active)
    {
        placementActive = active;
        if (walkthroughController != null) walkthroughController.SetInputBlocked(active);
        if (planningController != null) planningController.SetInputBlocked(active);
    }

    public void SetMode(AuditoriumCameraMode mode)
    {
        if (mode == AuditoriumCameraMode.Walkthrough && placementSystem != null && placementSystem.IsPlacing)
            placementSystem.CancelPlacement();
        if (mode == CurrentMode)
        {
            return;
        }

        ApplyMode(mode, true);
    }

    private void ApplyMode(AuditoriumCameraMode mode, bool notify)
    {
        CurrentMode = mode;
        bool walkthroughActive = mode == AuditoriumCameraMode.Walkthrough;

        if (walkthroughCamera != null) walkthroughCamera.enabled = walkthroughActive;
        if (planningCamera != null) planningCamera.enabled = !walkthroughActive;
        if (walkthroughController != null) walkthroughController.enabled = walkthroughActive;
        if (planningController != null) planningController.enabled = !walkthroughActive;
        if (walkthroughController != null) walkthroughController.SetInputBlocked(placementActive);
        if (planningController != null) planningController.SetInputBlocked(placementActive);
        ApplyCeilingVisibility();

        if (notify)
        {
            ModeChanged?.Invoke(CurrentMode);
        }
    }

    private void ApplyCeilingVisibility()
    {
        if (ceilingRoot != null)
            ceilingRoot.SetActive(CurrentMode == AuditoriumCameraMode.Walkthrough);
    }

    private static bool HasFocusedTextInput()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.GetComponentInParent<UnityEngine.UI.InputField>() != null;
    }
}
