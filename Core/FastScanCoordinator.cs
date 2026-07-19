// Chris Deliga / CNS Capecter NetworXs System / atlaya.capecter.com
using System.IO;
using System.Threading;

namespace AtlayaView.Core;

/// <summary>
/// Bindeglied zwischen Einstellung, Cache und <see cref="NtfsFastScanner"/>: entscheidet, ob
/// der NTFS-Schnellscan für einen angeforderten Pfad überhaupt greifen kann, und wählt
/// zwischen inkrementellem Update (Cache vorhanden) und vollem Volumen-Scan.
/// </summary>
public static class FastScanCoordinator
{
    /// <summary>
    /// Versucht den Schnellscan. Voraussetzungen: Einstellung aktiv, Prozess läuft erhöht,
    /// und der Pfad ist exakt eine NTFS-Laufwerkswurzel (FSCTL_ENUM_USN_DATA arbeitet immer
    /// volumenweit, nicht auf Unterordnern – Scans in einen Unterordner hinein laufen daher
    /// immer über den normalen <see cref="FileSystemScanner"/>). Gibt bei jedem Fehler oder
    /// jeder Inkonsistenz <c>false</c> zurück, ohne Nebenwirkungen außer einem ggf. verworfenen
    /// (nicht mehr gültigen) Cache.
    /// </summary>
    public static bool TryScan(string path, CancellationToken ct, out FileSystemNode? root)
    {
        root = null;

        if (!ScanPreferences.Instance.FastScanEnabled || !ElevationHelper.IsElevated)
            return false;

        string? driveRoot = NormalizeNtfsDriveRoot(path);
        if (driveRoot == null) return false;

        try
        {
            if (FileTreeCache.TryLoad(driveRoot, out var cachedRoot, out var index, out var journalId, out var lastUsn)
                && cachedRoot != null && index != null)
            {
                if (NtfsFastScanner.TryIncrementalUpdate(driveRoot, cachedRoot, index, journalId, lastUsn, ct, out long newCursor))
                {
                    FileTreeCache.Save(driveRoot, cachedRoot, journalId, newCursor);
                    root = cachedRoot;
                    return true;
                }

                // Cache/Journal nicht mehr verwendbar (z. B. übergelaufen oder neu erstellt) ->
                // verwerfen und unten komplett neu einlesen.
                FileTreeCache.Invalidate(driveRoot);
            }

            if (NtfsFastScanner.TryFullVolumeScan(driveRoot, ct, out var freshRoot, out var freshJournalId, out long freshCursor)
                && freshRoot != null)
            {
                FileTreeCache.Save(driveRoot, freshRoot, freshJournalId, freshCursor);
                root = freshRoot;
                return true;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* jeder unerwartete Fehler -> Aufrufer scannt normal weiter */ }

        return false;
    }

    private static string? NormalizeNtfsDriveRoot(string path)
    {
        try
        {
            string full = Path.GetFullPath(path);
            string? pathRoot = Path.GetPathRoot(full);
            if (string.IsNullOrEmpty(pathRoot)) return null;
            if (!string.Equals(full.TrimEnd('\\'), pathRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                return null; // Unterordner, keine Laufwerkswurzel -> Fast-Path nicht anwendbar

            var drive = new DriveInfo(pathRoot);
            if (!drive.IsReady || !string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                return null;

            return pathRoot;
        }
        catch
        {
            return null;
        }
    }
}
