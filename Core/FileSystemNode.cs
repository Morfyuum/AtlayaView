using System.Windows;

namespace AtlayaView.Core;

/// <summary>
/// Repräsentiert einen Knoten im Dateibaum (Datei oder Ordner).
/// </summary>
public sealed class FileSystemNode
{
    public string Name       { get; set; } = string.Empty;
    public string FullPath   { get; set; } = string.Empty;
    public bool   IsDirectory{ get; init; }
    public string Extension  { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }

    /// <summary>Eigengröße (Dateien) bzw. kumulierte Summe aller Kinder (Ordner).</summary>
    public long Size { get; set; }

    public FileSystemNode?        Parent   { get; set; }
    public List<FileSystemNode>   Children { get; } = [];

    // ── Layout-Ergebnis ──────────────────────────────────────────────────────
    /// <summary>Pixel-Bounds nach dem Treemap-Layout (relativ zum Canvas).</summary>
    public Rect Bounds { get; set; }

    // ── Cushion-Surface (akkumuliert von Vorfahren) ──────────────────────────
    /// <summary>Cushion-Surface-Koeffizienten: h(x,y) = Ax·x² + Bx·x + Ay·y² + By·y</summary>
    public double CushionAx { get; set; }
    public double CushionBx { get; set; }
    public double CushionAy { get; set; }
    public double CushionBy { get; set; }

    // ── Tiefeninfo ──────────────────────────────────────────────────────────
    public int Depth { get; set; }

    /// <summary>
    /// NTFS-Datensatznummer (MFT-Referenznummer) des Knotens – nur gesetzt, wenn der Knoten
    /// aus dem NTFS-Schnellscan (<see cref="NtfsFastScanner"/>) stammt; sonst 0.
    /// Dient dem Cache/USN-Journal-Abgleich als stabiler Schlüssel (Pfade können sich durch
    /// Umbenennen ändern, die Referenznummer nicht).
    /// </summary>
    public ulong FileReferenceNumber { get; set; }

    public override string ToString() => $"{(IsDirectory ? "[D]" : "[F]")} {Name} ({FormatSize(Size)})";

    public static string FormatSize(long bytes) => bytes switch
    {
        >= 1L << 40 => $"{bytes / (double)(1L << 40):F2} TB",
        >= 1L << 30 => $"{bytes / (double)(1L << 30):F2} GB",
        >= 1L << 20 => $"{bytes / (double)(1L << 20):F2} MB",
        >= 1L << 10 => $"{bytes / (double)(1L << 10):F1} KB",
        _            => $"{bytes} B"
    };
}
