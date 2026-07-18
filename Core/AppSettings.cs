namespace AtlayaView.Core;

/// <summary>
/// Globale Rendering-Einstellungen (Singleton, nicht persistent).
/// </summary>
public sealed class AppSettings
{
    private static readonly AppSettings _instance = new();
    public static AppSettings Instance => _instance;

    // ── Standard-Werte ────────────────────────────────────────────────────────
    public const double DefaultCushionHeight = 0.50;
    public const double DefaultCushionDecay  = 0.75;
    public const double DefaultAmbientLight  = 0.20;
    public const double DefaultMinPixelSize  = 2.0;
    public const bool   DefaultShowBorders   = true;

    // ── Properties ───────────────────────────────────────────────────────────
    /// <summary>Kissenintensität der obersten Ebene (0,1 – 1,0).</summary>
    public double CushionHeight { get; set; } = DefaultCushionHeight;

    /// <summary>Dämpfungsfaktor pro Tiefenebene (0,3 – 0,95).</summary>
    public double CushionDecay  { get; set; } = DefaultCushionDecay;

    /// <summary>Umgebungslicht-Anteil (0,05 – 0,50).</summary>
    public double AmbientLight  { get; set; } = DefaultAmbientLight;

    /// <summary>Minimale Pixelgröße für Leaf-Rendering (1 – 5).</summary>
    public double MinPixelSize  { get; set; } = DefaultMinPixelSize;

    /// <summary>Verzeichnisgrenzen zeichnen.</summary>
    public bool   ShowBorders   { get; set; } = DefaultShowBorders;

    public void Reset()
    {
        CushionHeight = DefaultCushionHeight;
        CushionDecay  = DefaultCushionDecay;
        AmbientLight  = DefaultAmbientLight;
        MinPixelSize  = DefaultMinPixelSize;
        ShowBorders   = DefaultShowBorders;
    }
}
