using System;
using System.Globalization;
using System.IO;

[Serializable]
public sealed class CustomLayoutSummary
{
    public string FileName { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string CreatedUtc { get; private set; }
    public string ModifiedUtc { get; private set; }
    public int FurnitureCount { get; private set; }

    public CustomLayoutSummary(string fileName, AuditoriumLayoutData data)
    {
        FileName = fileName;
        Name = !string.IsNullOrWhiteSpace(data.layoutName)
            ? data.layoutName.Trim() : CreateFallbackName(fileName);
        Description = data.description ?? string.Empty;
        CreatedUtc = data.createdUtc ?? string.Empty;
        ModifiedUtc = data.modifiedUtc ?? string.Empty;
        FurnitureCount = data.placements != null ? data.placements.Count : 0;
    }

    private static string CreateFallbackName(string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(stem) || Guid.TryParse(stem, out _))
            return "Untitled Saved Layout";

        string readable = stem.Replace('_', ' ').Replace('-', ' ').Trim();
        return readable.Length == 0
            ? "Untitled Saved Layout"
            : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(readable);
    }
}
