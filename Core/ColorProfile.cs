namespace AtlayaView.Core;

/// <summary>
/// Benutzerdefiniertes Farbprofil: eine benannte Gruppe von Datei-Erweiterungen mit
/// je eigener Farbe (z. B. "Videos" → jede Video-Endung mit ihrer eigenen Farbe).
/// Wird als Vorlage gespeichert und kann jederzeit erneut angewendet werden.
/// </summary>
public sealed class ColorProfile
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Startfarbe für neu im Editor hinzugefügte Erweiterungen sowie Farbmuster in der Profilliste.</summary>
    public string ColorHex { get; set; } = "#647882";

    /// <summary>Erweiterung → Hex-Farbe. Jede Erweiterung im Profil kann eine eigene Farbe haben.</summary>
    public Dictionary<string, string> ExtensionColors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Nur für die einmalige Migration von Profilen aus Versionen vor 2.0.24 (eine
    /// gemeinsame Farbe für alle Erweiterungen). <see cref="ColorProfileStore.Load"/>
    /// füllt daraus <see cref="ExtensionColors"/> und leert dieses Feld danach wieder.
    /// </summary>
    public List<string>? Extensions { get; set; }
}
