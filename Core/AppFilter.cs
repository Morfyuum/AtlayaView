namespace AtlayaView.Core;

/// <summary>
/// Globale Filter-Einstellungen für den Treemap-Display (Singleton, nicht persistent).
/// </summary>
public sealed class AppFilter
{
    private static readonly AppFilter _instance = new();
    public static AppFilter Instance => _instance;

    // ── Properties ───────────────────────────────────────────────────────────
    /// <summary>Dateien kleiner als dieser Wert werden nicht dargestellt (Bytes).</summary>
    public long MinFileSizeBytes { get; set; } = 0;

    /// <summary>Erweiterungen, die aus dem Treemap ausgeblendet werden.</summary>
    public HashSet<string> ExcludedExtensions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Versteckte Dateien (Hidden-Attribut) ausblenden.</summary>
    public bool ExcludeHiddenFiles { get; set; } = false;

    /// <summary>Systemdateien (System-Attribut) ausblenden.</summary>
    public bool ExcludeSystemFiles { get; set; } = false;

    // ── Filterpruefung ────────────────────────────────────────────────────────
    /// <summary>
    /// Gibt true zurück, wenn der Knoten den aktuellen Filtern entspricht
    /// und im Treemap angezeigt werden soll.
    /// </summary>
    public bool Passes(FileSystemNode node)
    {
        if (node.IsDirectory) return true;   // Ordner immer durchlassen
        if (node.Size < MinFileSizeBytes) return false;
        if (ExcludedExtensions.Count > 0 && ExcludedExtensions.Contains(node.Extension)) return false;
        return true;
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────
    public bool IsDefault =>
        MinFileSizeBytes == 0 &&
        ExcludedExtensions.Count == 0 &&
        !ExcludeHiddenFiles &&
        !ExcludeSystemFiles;

    public void Reset()
    {
        MinFileSizeBytes = 0;
        ExcludedExtensions.Clear();
        ExcludeHiddenFiles = false;
        ExcludeSystemFiles = false;
    }
}
