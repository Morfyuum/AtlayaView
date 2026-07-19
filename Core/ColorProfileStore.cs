using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AtlayaView.Core;

/// <summary>
/// Speichert und lädt benutzerdefinierte Farbprofile als JSON-Datei im
/// AppData-Ordner (analog zu <see cref="SettingsStore"/>, aber eigene Datei, da
/// Profile unabhängig vom Rest der Einstellungen bearbeitet/gelöscht werden).
/// Beim allerersten Aufruf wird die Datei mit zwei Beispielprofilen angelegt,
/// die der Nutzer danach frei umbenennen, erweitern oder löschen kann.
/// </summary>
public static class ColorProfileStore
{
    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AtlayaView",
            "AtlayaView.colorprofiles.json");

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public static List<ColorProfile> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var profiles = JsonSerializer.Deserialize<List<ColorProfile>>(
                    File.ReadAllText(FilePath), _jsonOpts);
                if (profiles != null)
                {
                    MigrateLegacyExtensions(profiles);
                    return profiles;
                }
            }
        }
        catch { /* Fehlerhafte Datei -> mit Beispielprofilen neu anlegen */ }

        var defaults = DefaultProfiles();
        Save(defaults);
        return defaults;
    }

    public static void Save(List<ColorProfile> profiles)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(profiles, _jsonOpts));
        }
        catch { /* Schreib-Fehler ignorieren */ }
    }

    /// <summary>
    /// Profile aus Versionen vor 2.0.24 kannten nur eine gemeinsame Farbe für alle
    /// Erweiterungen (Extensions-Liste + ein ColorHex). Migriert sie einmalig in die
    /// neue Pro-Erweiterung-Struktur, ohne das bisherige Aussehen zu verändern.
    /// </summary>
    private static void MigrateLegacyExtensions(List<ColorProfile> profiles)
    {
        bool changed = false;
        foreach (var p in profiles)
        {
            if (p.ExtensionColors.Count == 0 && p.Extensions is { Count: > 0 })
            {
                foreach (var ext in p.Extensions)
                    p.ExtensionColors[NormalizeExtension(ext)] = p.ColorHex;
                changed = true;
            }
            if (p.Extensions != null)
            {
                p.Extensions = null;
                changed = true;
            }
        }
        if (changed) Save(profiles);
    }

    private static string NormalizeExtension(string raw)
    {
        var ext = raw.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : "." + ext;
    }

    // ── Zwei Beispielprofile als Startpunkt ─────────────────────────────────
    private static List<ColorProfile> DefaultProfiles() =>
    [
        new ColorProfile
        {
            Name = "Videos",
            ColorHex = "#28A050",
            ExtensionColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".mpg"] = "#28A050", [".mpeg"] = "#28A050", [".avi"] = "#28A050", [".mov"] = "#28A050",
                [".mp4"] = "#28A050", [".mkv"] = "#28A050", [".wmv"] = "#28A050", [".flv"] = "#28A050",
                [".m4v"] = "#28A050", [".webm"] = "#28A050",
            }
        },
        new ColorProfile
        {
            Name = "Office-Dokumente",
            ColorHex = "#3878C8",
            ExtensionColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".doc"] = "#3878C8", [".docx"] = "#3878C8", [".xls"] = "#3878C8", [".xlsx"] = "#3878C8",
                [".ppt"] = "#3878C8", [".pptx"] = "#3878C8", [".odt"] = "#3878C8", [".ods"] = "#3878C8",
                [".rtf"] = "#3878C8",
            }
        }
    ];
}
