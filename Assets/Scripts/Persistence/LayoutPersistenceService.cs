using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;

/// <summary>Saves, loads, and resets the current furniture layout as versioned JSON.</summary>
public sealed class LayoutPersistenceService : MonoBehaviour
{
    private const int CurrentFormatVersion = 1;
    private const int MaximumFurnitureCount = 1000;
    private const long MaximumFileSizeBytes = 1024 * 1024;
    private const string LayoutDirectoryName = "VirtualAuditorium";
    private const string LayoutFileName = "layout.json";

    [SerializeField] private PlacementSystem placementSystem;

    public event Action<string> FeedbackChanged;

    public string LayoutFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, LayoutDirectoryName, LayoutFileName); }
    }

    public string NamedLayoutsDirectoryPath
    {
        get { return Path.Combine(Application.persistentDataPath, LayoutDirectoryName, "Layouts"); }
    }

    public void Configure(PlacementSystem placement)
    {
        placementSystem = placement;
    }

    public void SaveLayout()
    {
        if (placementSystem == null)
        {
            Report("Cannot save: the placement system is unavailable.");
            return;
        }

        string temporaryPath = null;
        try
        {
            // Save only confirmed furniture; cancel a live preview first, restoring any move.
            if (placementSystem.IsPlacing) placementSystem.CancelPlacement();
            string layoutPath = LayoutFilePath;
            temporaryPath = layoutPath + ".tmp";
            FurnitureInstance[] placed = placementSystem.GetPlacedFurniture();
            if (placed.Length > MaximumFurnitureCount)
            {
                Report("Save failed: a layout cannot contain more than " + MaximumFurnitureCount + " items.");
                return;
            }
            AuditoriumLayoutData layout = new AuditoriumLayoutData();
            layout.formatVersion = CurrentFormatVersion;
            layout.layoutName = "Virtual Auditorium Layout";
            for (int i = 0; i < placed.Length; i++)
            {
                FurnitureInstance instance = placed[i];
                if (instance == null) continue;
                FurniturePlacementRecord record = new FurniturePlacementRecord();
                record.instanceId = instance.InstanceId;
                record.furnitureId = FurnitureCatalog.Get(instance.Type).StableId;
                record.position = instance.transform.position;
                record.rotationEuler = instance.transform.rotation.eulerAngles;
                record.scale = instance.transform.localScale;
                layout.placements.Add(record);
            }

            string directory = Path.GetDirectoryName(layoutPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(layout, true), new UTF8Encoding(false));

            if (File.Exists(layoutPath)) File.Replace(temporaryPath, layoutPath, null);
            else File.Move(temporaryPath, layoutPath);

            Debug.Log("Virtual Auditorium layout saved to: " + layoutPath);
            Report("Saved " + layout.placements.Count + " item(s).");
        }
        catch (Exception exception)
        {
            Report("Save failed: " + exception.Message);
        }
        finally
        {
            try { if (!string.IsNullOrEmpty(temporaryPath) && File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (Exception cleanupException) { Debug.LogWarning("Could not remove temporary layout file: " + cleanupException.Message); }
        }
    }

    public void LoadLayout()
    {
        if (placementSystem == null)
        {
            Report("Cannot load: the placement system is unavailable.");
            return;
        }
        LoadLayoutFromPath(LayoutFilePath, false);
    }

    public void ResetLayout()
    {
        if (placementSystem == null)
        {
            Report("Cannot reset: the placement system is unavailable.");
            return;
        }

        placementSystem.ResetPlacedFurniture();
        Report("Current layout cleared; saved file unchanged.");
    }

    public bool SaveAsMyLayout(string name, string description, bool confirmOverwrite,
        out string savedFileName, out string error)
    {
        savedFileName = null;
        error = null;
        if (placementSystem == null)
        {
            error = "The placement system is unavailable.";
            Report("Save failed: " + error);
            return false;
        }

        string normalizedName;
        string fileName;
        error = ValidateCustomName(name, out normalizedName, out fileName);
        if (error != null)
        {
            Report("Save failed: " + error);
            return false;
        }

        try
        {
            string path = Path.Combine(NamedLayoutsDirectoryPath, fileName);
            AuditoriumLayoutData previous = null;
            if (File.Exists(path))
            {
                if (!confirmOverwrite)
                {
                    error = "A saved layout with this file name already exists. Confirm replacement to overwrite it.";
                    Report(error);
                    return false;
                }
                previous = ReadLayoutFile(path);
                if (previous == null)
                {
                    error = "The existing saved layout could not be read; it was not overwritten.";
                    Report(error);
                    return false;
                }
            }

            FurnitureInstance[] instances = placementSystem.GetPlacedFurniture();
            if (instances.Length > MaximumFurnitureCount)
            {
                error = "A custom layout cannot contain more than " + MaximumFurnitureCount + " items.";
                Report("Save failed: " + error);
                return false;
            }
            AuditoriumLayoutData data = CaptureCurrentLayout(normalizedName);
            data.description = (description ?? string.Empty).Trim();
            data.createdUtc = previous != null && !string.IsNullOrEmpty(previous.createdUtc)
                ? previous.createdUtc : DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            data.modifiedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            WriteLayoutFile(path, data);
            savedFileName = fileName;
            Report("Saved custom layout '" + normalizedName + "' (" + data.placements.Count + " item(s)).");
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            Report("Save failed: " + error);
            return false;
        }
    }

    public CustomLayoutSummary[] GetMyLayouts()
    {
        List<CustomLayoutSummary> layouts = new List<CustomLayoutSummary>();
        try
        {
            if (!Directory.Exists(NamedLayoutsDirectoryPath)) return layouts.ToArray();
            string[] paths = Directory.GetFiles(NamedLayoutsDirectoryPath, "*.json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < paths.Length; i++)
            {
                try
                {
                    FileInfo info = new FileInfo(paths[i]);
                    if (info.Length <= 0 || info.Length > MaximumFileSizeBytes) continue;
                    AuditoriumLayoutData data = ReadLayoutFile(paths[i]);
                    // A missing legacy name is display metadata, not a reason to hide an otherwise valid layout.
                    // CustomLayoutSummary supplies a readable filename-based fallback without changing the JSON.
                    if (data == null || ValidateLayoutData(data) != null) continue;
                    layouts.Add(new CustomLayoutSummary(Path.GetFileName(paths[i]), data));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Could not index a saved custom layout: " + exception.Message);
                }
            }
            layouts.Sort((left, right) => string.Compare(right.ModifiedUtc, left.ModifiedUtc, StringComparison.Ordinal));
        }
        catch (Exception exception)
        {
            Report("Could not list saved layouts: " + exception.Message);
        }
        return layouts.ToArray();
    }

    public bool LoadMyLayout(string fileName)
    {
        if (placementSystem == null)
        {
            Report("Cannot load: the placement system is unavailable.");
            return false;
        }
        string path = GetSafeCustomLayoutPath(fileName);
        if (path == null)
        {
            Report("Load failed: the saved layout file name is invalid.");
            return false;
        }
        return LoadLayoutFromPath(path, true);
    }

    public bool RenameMyLayout(string fileName, string newName, bool confirmOverwrite, out string error)
    {
        error = null;
        string oldPath = GetSafeCustomLayoutPath(fileName);
        if (oldPath == null || !File.Exists(oldPath))
        {
            error = "The saved layout no longer exists.";
            Report(error);
            return false;
        }
        string normalizedName;
        string newFileName;
        error = ValidateCustomName(newName, out normalizedName, out newFileName);
        if (error != null)
        {
            Report(error);
            return false;
        }
        string newPath = Path.Combine(NamedLayoutsDirectoryPath, newFileName);
        bool samePath = string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase);
        if (!samePath && File.Exists(newPath) && !confirmOverwrite)
        {
            error = "A saved layout with this file name already exists. Confirm replacement to continue.";
            Report(error);
            return false;
        }
        try
        {
            AuditoriumLayoutData data = ReadLayoutFile(oldPath);
            if (data == null || ValidateLayoutData(data) != null)
            {
                error = "The existing saved layout is invalid and cannot be renamed.";
                Report(error);
                return false;
            }
            data.layoutName = normalizedName;
            data.modifiedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            WriteLayoutFile(newPath, data);
            if (!samePath) File.Delete(oldPath);
            Report("Renamed saved layout to '" + normalizedName + "'.");
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            Report("Rename failed: " + error);
            return false;
        }
    }

    public bool DeleteMyLayout(string fileName)
    {
        string path = GetSafeCustomLayoutPath(fileName);
        if (path == null || !File.Exists(path))
        {
            Report("The saved layout no longer exists.");
            return false;
        }
        try
        {
            File.Delete(path);
            Report("Deleted saved layout.");
            return true;
        }
        catch (Exception exception)
        {
            Report("Delete failed: " + exception.Message);
            return false;
        }
    }

    private bool LoadLayoutFromPath(string path, bool namedLayout)
    {
        try
        {
            if (!File.Exists(path))
            {
                Report(namedLayout ? "Saved layout not found." : "No saved layout found. Save a layout first.");
                return false;
            }
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Length <= 0 || fileInfo.Length > MaximumFileSizeBytes)
            {
                Report("Load failed: the layout file is empty or larger than 1 MB.");
                return false;
            }

            AuditoriumLayoutData layout = ReadLayoutFile(path);
            string validationError = ValidateLayoutData(layout);
            if (validationError != null)
            {
                Report("Load failed: " + validationError);
                return false;
            }

            string placementError;
            if (!placementSystem.TryReplacePlacedFurniture(layout.placements, out placementError))
            {
                Report("Load failed: " + placementError + " Current layout was kept.");
                return false;
            }

            Debug.Log("Virtual Auditorium layout loaded from: " + path);
            Report("Loaded " + layout.placements.Count + " item(s)" +
                (namedLayout ? " from '" + layout.layoutName + "'." : "."));
            return true;
        }
        catch (Exception exception)
        {
            Report("Load failed: " + exception.Message + " Current layout was kept.");
            return false;
        }
    }

    private AuditoriumLayoutData CaptureCurrentLayout(string name)
    {
        if (placementSystem.IsPlacing) placementSystem.CancelPlacement();
        FurnitureInstance[] placed = placementSystem.GetPlacedFurniture();
        AuditoriumLayoutData data = new AuditoriumLayoutData();
        data.formatVersion = CurrentFormatVersion;
        data.layoutName = name;
        for (int i = 0; i < placed.Length; i++)
        {
            if (placed[i] == null) continue;
            FurniturePlacementRecord record = new FurniturePlacementRecord();
            record.instanceId = placed[i].InstanceId;
            record.furnitureId = FurnitureCatalog.Get(placed[i].Type).StableId;
            record.position = placed[i].transform.position;
            record.rotationEuler = placed[i].transform.rotation.eulerAngles;
            record.scale = placed[i].transform.localScale;
            data.placements.Add(record);
        }
        return data;
    }

    private string GetSafeCustomLayoutPath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || Path.GetFileName(fileName) != fileName
            || !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return null;
        return Path.Combine(NamedLayoutsDirectoryPath, fileName);
    }

    private static AuditoriumLayoutData ReadLayoutFile(string path)
    {
        return JsonUtility.FromJson<AuditoriumLayoutData>(File.ReadAllText(path, Encoding.UTF8));
    }

    private static void WriteLayoutFile(string path, AuditoriumLayoutData data)
    {
        string directory = Path.GetDirectoryName(path);
        Directory.CreateDirectory(directory);
        string temporaryPath = path + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporaryPath, path, null);
            else File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static string ValidateCustomName(string name, out string normalizedName, out string fileName)
    {
        normalizedName = (name ?? string.Empty).Trim();
        fileName = null;
        if (normalizedName.Length == 0) return "Enter a layout name.";
        if (normalizedName.Length > 64) return "Layout names must be 64 characters or fewer.";

        const string windowsInvalid = "<>:\"/\\|?*";
        StringBuilder slug = new StringBuilder(normalizedName.Length);
        bool previousUnderscore = false;
        for (int i = 0; i < normalizedName.Length; i++)
        {
            char character = normalizedName[i];
            if (char.IsControl(character) || windowsInvalid.IndexOf(character) >= 0)
                return "Layout names cannot contain file-system path characters.";
            if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
            {
                slug.Append(character);
                previousUnderscore = false;
            }
            else if (char.IsWhiteSpace(character) || character == '.')
            {
                if (!previousUnderscore && slug.Length > 0) slug.Append('_');
                previousUnderscore = true;
            }
        }

        string fileStem = slug.ToString().Trim('_', '-', '.');
        if (fileStem.Length == 0) return "Use at least one letter or number in the layout name.";
        fileName = fileStem + ".json";
        return null;
    }

    private static string ValidateLayoutData(AuditoriumLayoutData layout)
    {
        if (layout == null) return "the file does not contain readable layout JSON.";
        if (layout.formatVersion != CurrentFormatVersion) return "unsupported layout format version " + layout.formatVersion + ".";
        if (layout.placements == null) return "the furniture list is missing.";
        if (layout.placements.Count > MaximumFurnitureCount) return "the layout contains more than " + MaximumFurnitureCount + " items.";

        HashSet<string> instanceIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < layout.placements.Count; i++)
        {
            FurniturePlacementRecord record = layout.placements[i];
            FurnitureDefinition unused;
            if (!FurnitureCatalog.TryGetById(record.furnitureId, out unused))
                return "item " + (i + 1) + " has an unknown furniture identifier.";
            if (!string.IsNullOrEmpty(record.instanceId) && !instanceIds.Add(record.instanceId))
                return "item " + (i + 1) + " duplicates a furniture instance identifier.";
            if (!IsFinite(record.position) || !IsFinite(record.rotationEuler) || !IsFinite(record.scale))
                return "item " + (i + 1) + " contains a non-finite transform.";
            if (Mathf.Abs(Mathf.DeltaAngle(0f, record.rotationEuler.x)) > 0.01f
                || Mathf.Abs(Mathf.DeltaAngle(0f, record.rotationEuler.z)) > 0.01f)
                return "item " + (i + 1) + " has a tilted rotation; furniture must remain upright.";
            if (record.rotationEuler.y < 0f || record.rotationEuler.y > 360f)
                return "item " + (i + 1) + " has an unsupported rotation value.";
            if (record.scale.x <= 0f || record.scale.y <= 0f || record.scale.z <= 0f
                || record.scale.x > 5f || record.scale.y > 5f || record.scale.z > 5f)
                return "item " + (i + 1) + " has an unsupported scale.";
        }

        return null;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void Report(string message)
    {
        FeedbackChanged?.Invoke(message);
    }
}
