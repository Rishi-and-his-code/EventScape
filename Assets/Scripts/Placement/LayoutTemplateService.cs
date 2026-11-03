using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Coordinates built-in template data with the auditorium frame and placement transaction.</summary>
public sealed class LayoutTemplateService : MonoBehaviour
{
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private AuditoriumDemoSettings settings;
    [SerializeField] private Transform auditoriumRoot;
    [SerializeField] private Transform stageTransform;

    public event Action<string> FeedbackChanged;

    public void Configure(PlacementSystem placement, AuditoriumDemoSettings demoSettings,
        Transform roomRoot, Transform stage)
    {
        placementSystem = placement;
        settings = demoSettings;
        auditoriumRoot = roomRoot;
        stageTransform = stage;
    }

    public LayoutTemplateDefinition[] GetTemplates()
    {
        if (settings == null || auditoriumRoot == null || stageTransform == null)
        {
            FeedbackChanged?.Invoke("Templates are unavailable until the auditorium is built.");
            return new LayoutTemplateDefinition[0];
        }
        return LayoutTemplateCatalog.Generate(settings, auditoriumRoot, stageTransform);
    }

    public bool ApplyTemplate(string templateId, bool replaceCurrentLayout, out string message)
    {
        LayoutTemplateDefinition[] templates = GetTemplates();
        LayoutTemplateDefinition selected = null;
        for (int i = 0; i < templates.Length; i++)
            if (string.Equals(templates[i].Id, templateId, StringComparison.Ordinal)) selected = templates[i];
        return ApplyTemplate(selected, replaceCurrentLayout, out message);
    }

    /// <summary>Applies the exact freshly generated definition currently displayed by the UI preview.</summary>
    public bool ApplyTemplate(LayoutTemplateDefinition selected, bool replaceCurrentLayout, out string message)
    {
        message = null;
        if (placementSystem == null)
        {
            message = "The placement system is unavailable.";
            FeedbackChanged?.Invoke(message);
            return false;
        }
        if (selected == null)
        {
            message = "The selected event template is unavailable.";
            FeedbackChanged?.Invoke(message);
            return false;
        }
        if (selected.Placements == null || selected.Placements.Length == 0)
        {
            message = "The selected event template contains no furniture placements.";
            FeedbackChanged?.Invoke(message);
            return false;
        }

        List<FurniturePlacementRecord> records = new List<FurniturePlacementRecord>(selected.Placements.Length);
        for (int i = 0; i < selected.Placements.Length; i++)
        {
            TemplateFurniturePlacement item = selected.Placements[i];
            FurniturePlacementRecord record = new FurniturePlacementRecord();
            record.furnitureId = FurnitureCatalog.Get(item.furnitureType).StableId;
            record.position = auditoriumRoot.TransformPoint(item.auditoriumLocalPosition);
            Quaternion localRotation = Quaternion.Euler(0f, item.localYawDegrees, 0f);
            record.rotationEuler = (auditoriumRoot.rotation * localRotation).eulerAngles;
            record.scale = item.scale;
            record.instanceId = string.Empty;
            records.Add(record);
        }

        string failureReason;
        if (!placementSystem.TryApplyTemplate(records, replaceCurrentLayout, out failureReason))
        {
            message = "Could not apply " + selected.Name + ": " + failureReason;
            FeedbackChanged?.Invoke(message);
            return false;
        }

        message = "Applied " + selected.Name + " with " + records.Count + " furniture items.";
        FeedbackChanged?.Invoke(message);
        return true;
    }
}
