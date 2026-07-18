namespace AtlayaView.Core;

/// <summary>
/// Laufzeit-Einstellungen der Update-Prüfung (Singleton, Muster wie <see cref="AppSettings"/>).
/// Persistiert über <see cref="SettingsStore"/>.
/// </summary>
public sealed class UpdatePreferences
{
    private static readonly UpdatePreferences _instance = new();
    public static UpdatePreferences Instance => _instance;

    /// <summary>"manual" | "auto_check" | "auto_apply".</summary>
    public string CheckMode { get; set; } = "manual";

    /// <summary>"daily" | "weekly" | "monthly" | "yearly".</summary>
    public string CheckInterval { get; set; } = "weekly";
}
