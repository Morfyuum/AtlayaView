// Chris Deliga / CNS Capecter NetworXs System / atlaya.capecter.com
namespace AtlayaView.Core;

/// <summary>
/// Laufzeit-Einstellungen des Schnellscans (Singleton, Muster wie <see cref="UpdatePreferences"/>).
/// Persistiert über <see cref="SettingsStore"/>.
/// </summary>
public sealed class ScanPreferences
{
    private static readonly ScanPreferences _instance = new();
    public static ScanPreferences Instance => _instance;

    /// <summary>
    /// Opt-in: NTFS-Schnellscan (Vollvolumen-Enumeration + USN-Journal-Cache) statt des
    /// normalen, ordnerweisen Scans. Braucht erhöhte Rechte (Admin/SeBackupPrivilege für den
    /// Volume-Handle-Zugriff) – wird nur genutzt, wenn zusätzlich <see cref="ElevationHelper.IsElevated"/>
    /// zutrifft; sonst automatischer Fallback auf den normalen Scanner.
    /// </summary>
    public bool FastScanEnabled { get; set; } = false;
}
