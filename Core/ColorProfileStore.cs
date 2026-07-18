using System.IO;
using System.Text.Json;

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
        new() { WriteIndented = true };

    public static List<ColorProfile> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var profiles = JsonSerializer.Deserialize<List<ColorProfile>>(
                    File.ReadAllText(FilePath), _jsonOpts);
                if (profiles != null) return profiles;
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

    // ── Zwei Beispielprofile als Startpunkt ─────────────────────────────────
    private static List<ColorProfile> DefaultProfiles() =>
    [
        new ColorProfile
        {
            Name = "Videos",
            ColorHex = "#28A050",
            Extensions = [".mpg", ".mpeg", ".avi", ".mov", ".mp4", ".mkv", ".wmv", ".flv", ".m4v", ".webm"]
        },
        new ColorProfile
        {
            Name = "Office-Dokumente",
            ColorHex = "#3878C8",
            Extensions = [".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".rtf"]
        }
    ];
}
