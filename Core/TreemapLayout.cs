using System.Windows;

namespace AtlayaView.Core;

/// <summary>
/// Squarified-Treemap-Layout (Bruls, Huizing, van Wijk 2000).
/// Berechnet Pixel-Bounds für jeden Knoten im Baum in-place.
/// </summary>
public sealed class TreemapLayout
{
    private const double MinSize = 1.0; // Pixel – kleinere Rechtecke werden ignoriert

    // ── Öffentliche API ──────────────────────────────────────────────────────
    public void Layout(FileSystemNode root, Rect canvas)
    {
        if (root.Size <= 0 || canvas.Width < 1 || canvas.Height < 1) return;
        // Alle alten Bounds zurücksetzen: Knoten die nicht platziert werden
        // (zu klein, gefiltert, überfüllte Bereiche) behalten sonst Stale-Bounds
        // aus einem früheren Layout-Durchlauf und werden vom Renderer fälschlich gezeichnet.
        ResetBounds(root);
        root.Bounds = canvas;
        LayoutChildren(root, canvas);
    }

    private static void ResetBounds(FileSystemNode node)
    {
        node.Bounds = Rect.Empty;
        foreach (var child in node.Children)
            ResetBounds(child);
    }

    // ── Layout-Schritt ────────────────────────────────────────────────────────
    private void LayoutChildren(FileSystemNode parent, Rect bounds)
    {
        var filter = AppFilter.Instance;
        var items  = parent.Children
            .Where(c => c.Size > 0 && filter.Passes(c))
            .OrderByDescending(c => c.Size)
            .ToList();

        if (items.Count == 0) return;

        long total = items.Sum(c => c.Size);
        Squarify(items, bounds, total);
    }

    // ── Squarify ──────────────────────────────────────────────────────────────
    private void Squarify(List<FileSystemNode> items, Rect bounds, long total)
    {
        if (items.Count == 0 || total <= 0) return;
        if (bounds.Width < MinSize || bounds.Height < MinSize) return;

        var remaining   = new List<FileSystemNode>(items);
        var currentRect = bounds;
        long remaining_total = total;

        while (remaining.Count > 0)
        {
            if (remaining_total <= 0) break;
            if (currentRect.Width < MinSize || currentRect.Height < MinSize) break;

            var row = new List<FileSystemNode>();

            for (int i = 0; i < remaining.Count; i++)
            {
                var candidate = remaining[i];
                var testRow   = new List<FileSystemNode>(row) { candidate };

                double worstBefore = row.Count == 0
                    ? double.MaxValue
                    : WorstAspectRatio(row, remaining_total, currentRect);

                double worstAfter = WorstAspectRatio(testRow, remaining_total, currentRect);

                if (worstAfter <= worstBefore || row.Count == 0)
                    row.Add(candidate);
                else
                    break; // Sortiert → weitere Items machen es nur schlechter
            }

            if (row.Count == 0) row.Add(remaining[0]);

            currentRect    = PlaceRow(row, remaining_total, currentRect);
            remaining_total -= row.Sum(n => n.Size);
            remaining.RemoveRange(0, row.Count);
        }
    }

    // ── Aspect-Ratio-Berechnung ──────────────────────────────────────────────
    private static double WorstAspectRatio(List<FileSystemNode> row,
                                           long totalRemaining, Rect bounds)
    {
        if (row.Count == 0) return double.MaxValue;

        double W         = bounds.Width;
        double H         = bounds.Height;
        double rowTotal  = row.Sum(n => (double)n.Size);
        double worst     = 0.0;

        foreach (var node in row)
        {
            double w, h;
            if (W >= H)
            {
                // Vertikaler Streifen
                w = W * rowTotal / totalRemaining;
                h = H * node.Size / rowTotal;
            }
            else
            {
                // Horizontaler Streifen
                h = H * rowTotal / totalRemaining;
                w = W * node.Size / rowTotal;
            }

            if (w <= 0 || h <= 0) continue;
            double ar = Math.Max(w / h, h / w);
            if (ar > worst) worst = ar;
        }

        return worst;
    }

    // ── Reihe platzieren ──────────────────────────────────────────────────────
    private Rect PlaceRow(List<FileSystemNode> row, long totalRemaining, Rect bounds)
    {
        if (row.Count == 0) return bounds;

        double W        = bounds.Width;
        double H        = bounds.Height;
        double rowTotal = row.Sum(n => (double)n.Size);

        Rect remaining;

        if (W >= H)
        {
            // Vertikaler Streifen an der linken Seite
            double stripWidth = W * rowTotal / totalRemaining;
            double y = bounds.Y;

            foreach (var node in row)
            {
                double h = H * node.Size / rowTotal;
                node.Bounds = new Rect(bounds.X, y, stripWidth, h);
                y += h;

                if (node.IsDirectory && node.Children.Count > 0)
                    LayoutChildren(node, ShrinkBounds(node.Bounds));
            }

            remaining = new Rect(bounds.X + stripWidth, bounds.Y,
                                 Math.Max(0, W - stripWidth), H);
        }
        else
        {
            // Horizontaler Streifen an der oberen Seite
            double stripHeight = H * rowTotal / totalRemaining;
            double x = bounds.X;

            foreach (var node in row)
            {
                double w = W * node.Size / rowTotal;
                node.Bounds = new Rect(x, bounds.Y, w, stripHeight);
                x += w;

                if (node.IsDirectory && node.Children.Count > 0)
                    LayoutChildren(node, ShrinkBounds(node.Bounds));
            }

            remaining = new Rect(bounds.X, bounds.Y + stripHeight,
                                 W, Math.Max(0, H - stripHeight));
        }

        return remaining;
    }

    /// <summary>
    /// Verkleinert die Bounds um 1 px Rand – gibt Verzeichnis-Trennlinien Platz.
    /// </summary>
    private static Rect ShrinkBounds(Rect r, double border = 1.0)
    {
        double w = r.Width  - 2 * border;
        double h = r.Height - 2 * border;
        if (w < 2 || h < 2) return r; // Zu klein: kein Rand
        return new Rect(r.X + border, r.Y + border, w, h);
    }
}
