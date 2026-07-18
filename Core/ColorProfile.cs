namespace AtlayaView.Core;

/// <summary>
/// Benutzerdefiniertes Farbprofil: eine benannte Gruppe von Datei-Erweiterungen,
/// die gemeinsam auf eine Farbe gesetzt werden (z. B. "Videos" → alle Video-Endungen
/// in Grün). Wird als Vorlage gespeichert und kann jederzeit erneut angewendet werden.
/// </summary>
public sealed class ColorProfile
{
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#647882";
    public List<string> Extensions { get; set; } = [];
}
