using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace AtlayaView.Core;

/// <summary>
/// Speichert und lädt alle Benutzereinstellungen als JSON-Datei
/// im Programmverzeichnis: AtlayaView.settings.json
/// </summary>
public sealed class SettingsStore
{
    // ── Rendering ────────────────────────────────────────────────────────────
    public double CushionHeight           { get; set; } = AppSettings.DefaultCushionHeight;
    public double CushionDecay            { get; set; } = AppSettings.DefaultCushionDecay;
    public double AmbientLight            { get; set; } = AppSettings.DefaultAmbientLight;
    public double MinPixelSize            { get; set; } = AppSettings.DefaultMinPixelSize;
    public bool   ShowBorders             { get; set; } = AppSettings.DefaultShowBorders;

    // ── Filter ───────────────────────────────────────────────────────────────
    public long         MinFileSizeBytes    { get; set; } = 0;
    public List<string> ExcludedExtensions  { get; set; } = [];
    public bool         ExcludeHiddenFiles  { get; set; } = false;
    public bool         ExcludeSystemFiles  { get; set; } = false;

    // ── Farbschema-Overrides ─────────────────────────────────────────────────
    public Dictionary<string, string> ColorOverrides { get; set; } = [];

    // ── Datei-Öffner (Extension → EXE-Pfad) ─────────────────────────────────
    public Dictionary<string, string> FileOpeners { get; set; } = [];

    // ── UI-Zustand ───────────────────────────────────────────────────────────
    public string Language             { get; set; } = nameof(AppLanguage.Deutsch);
    public bool   ShowFreeSpaceCushion { get; set; } = true;

    // ── Update-Prüfung ───────────────────────────────────────────────────────
    public string UpdateCheckMode     { get; set; } = "manual";
    public string UpdateCheckInterval { get; set; } = "weekly";

    // ── Datei-Pfad ───────────────────────────────────────────────────────────
    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AtlayaView",
            "AtlayaView.settings.json");

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { WriteIndented = true };

    // ── Speichern ────────────────────────────────────────────────────────────
    public static void Save(bool showFreeSpaceCushion)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            var store = new SettingsStore();

            var s = AppSettings.Instance;
            store.CushionHeight = s.CushionHeight;
            store.CushionDecay  = s.CushionDecay;
            store.AmbientLight  = s.AmbientLight;
            store.MinPixelSize  = s.MinPixelSize;
            store.ShowBorders   = s.ShowBorders;

            var f = AppFilter.Instance;
            store.MinFileSizeBytes   = f.MinFileSizeBytes;
            store.ExcludedExtensions = [..f.ExcludedExtensions.OrderBy(e => e)];
            store.ExcludeHiddenFiles = f.ExcludeHiddenFiles;
            store.ExcludeSystemFiles = f.ExcludeSystemFiles;

            foreach (var (ext, color) in ColorScheme.Overrides)
                store.ColorOverrides[ext] = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            store.Language             = LocalizationManager.Instance.Language.ToString();
            store.ShowFreeSpaceCushion = showFreeSpaceCushion;

            store.UpdateCheckMode     = UpdatePreferences.Instance.CheckMode;
            store.UpdateCheckInterval = UpdatePreferences.Instance.CheckInterval;

            store.FileOpeners = new Dictionary<string, string>(FileOpenerStore.Openers);

            File.WriteAllText(FilePath, JsonSerializer.Serialize(store, _jsonOpts));
        }
        catch { /* Schreib-Fehler ignorieren */ }
    }

    // ── Laden ────────────────────────────────────────────────────────────────
    public static SettingsStore Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var store = JsonSerializer.Deserialize<SettingsStore>(
                    File.ReadAllText(FilePath), _jsonOpts);
                if (store != null) return store;
            }
        }
        catch { /* Fehlerhafte Datei → Standardwerte */ }
        return new SettingsStore();
    }

    // ── Anwenden (alle Singletons befüllen) ──────────────────────────────────
    public void Apply()
    {
        var s = AppSettings.Instance;
        s.CushionHeight = CushionHeight;
        s.CushionDecay  = CushionDecay;
        s.AmbientLight  = AmbientLight;
        s.MinPixelSize  = MinPixelSize;
        s.ShowBorders   = ShowBorders;

        var f = AppFilter.Instance;
        f.MinFileSizeBytes = MinFileSizeBytes;
        f.ExcludedExtensions.Clear();
        foreach (var ext in ExcludedExtensions)
            f.ExcludedExtensions.Add(ext);
        f.ExcludeHiddenFiles = ExcludeHiddenFiles;
        f.ExcludeSystemFiles = ExcludeSystemFiles;

        ColorScheme.ResetAll();
        foreach (var (ext, hex) in ColorOverrides)
        {
            if (TryParseHex(hex, out var c))
                ColorScheme.SetColor(ext, c);
        }

        if (Enum.TryParse<AppLanguage>(Language, out var lang))
            LocalizationManager.Instance.Language = lang;

        UpdatePreferences.Instance.CheckMode     = UpdateCheckMode;
        UpdatePreferences.Instance.CheckInterval = UpdateCheckInterval;

        FileOpenerStore.Load(FileOpeners);
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────
    private static bool TryParseHex(string hex, out Color c)
    {
        c = default;
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return false;
        if (!byte.TryParse(hex[0..2], NumberStyles.HexNumber, null, out byte r)) return false;
        if (!byte.TryParse(hex[2..4], NumberStyles.HexNumber, null, out byte g)) return false;
        if (!byte.TryParse(hex[4..6], NumberStyles.HexNumber, null, out byte b)) return false;
        c = Color.FromRgb(r, g, b);
        return true;
    }
}
