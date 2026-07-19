using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AtlayaView.Core;

/// <summary>
/// Cushion-Treemap-Renderer nach van Wijk &amp; van de Wetering (1999).
///
/// Rendert in ein einfaches byte[]-Puffer (BGRx, stride = width*4).
/// Thread-safe: keine WPF-Abhängigkeit während des Renderings.
/// BitmapSource wird vom Aufrufer auf dem UI-Thread erstellt.
/// </summary>
public sealed class CushionRenderer
{
    // ── Lichtvektor (fest, normiert): L = normalize(-1, -1, 2) ───────────────
    private static readonly double Lx, Ly, Lz;
    // H = normalize(L + V), V = (0, 0, 1)  – Half-Vektor für Blinn-Phong
    private static readonly double Hx, Hy, Hz;

    static CushionRenderer()
    {
        double len = Math.Sqrt(1.0 + 1.0 + 4.0);
        Lx = -1.0 / len;
        Ly = -1.0 / len;
        Lz =  2.0 / len;

        double hx = Lx, hy = Ly, hz = Lz + 1.0;   // L + V
        double hlen = Math.Sqrt(hx * hx + hy * hy + hz * hz);
        Hx = hx / hlen;
        Hy = hy / hlen;
        Hz = hz / hlen;
    }

    // ── Render-Kontext ────────────────────────────────────────────────────────
    private readonly struct RenderContext
    {
        public readonly double Ia, Is, CushionHeight, CushionDecay;
        public readonly bool   ShowBorders;
        public readonly IReadOnlySet<string>? ActiveCategories;

        public RenderContext(AppSettings s, IReadOnlySet<string>? activeCategories)
        {
            Ia               = s.AmbientLight;
            Is               = 1.0 - Ia;
            CushionHeight    = s.CushionHeight;
            CushionDecay     = s.CushionDecay;
            ShowBorders      = s.ShowBorders;
            ActiveCategories = activeCategories;
        }
    }

    // ── Öffentliche API ──────────────────────────────────────────────────────

    /// <summary>
    /// Wenn gesetzt, werden nur Dateien dieser Kategorien farbig dargestellt –
    /// alle anderen erscheinen dunkelgrau (ausgegraut).
    /// null = alle anzeigen.
    /// </summary>
    public IReadOnlySet<string>? ActiveCategories { get; set; }

    /// <summary>
    /// Rendert den Baum in einen BGRx-Bytepuffer (stride = width*4).
    /// Kann sicher auf einem Hintergrund-Thread aufgerufen werden.
    /// </summary>
    public byte[] Render(FileSystemNode root, int width, int height)
    {
        if (NativeRenderer.TryRender(root, width, height, ActiveCategories, out var nativePixels))
            return nativePixels;

        var ctx    = new RenderContext(AppSettings.Instance, ActiveCategories);
        int stride = width * 4;
        var pixels = new byte[stride * height];

        // Hintergrund füllen (#1A1A2E → BGRx)
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 0] = 0x2E;  // B
            pixels[i + 1] = 0x1A;  // G
            pixels[i + 2] = 0x1A;  // R
            pixels[i + 3] = 0xFF;
        }

        unsafe
        {
            fixed (byte* ptr = pixels)
            {
                if (ctx.ShowBorders)
                    DrawAllBorders(ptr, stride, width, height, root, 0);
                RenderAllLeaves(ptr, stride, width, height, root, 0, 0.0, 0.0, 0.0, 0.0, in ctx);
            }
        }

        return pixels;
    }

    /// <summary>
    /// Rendert <paramref name="root"/> in einen bereits bestehenden Pixel-Puffer hinein.
    /// Der Aufrufer ist für Hintergrund und Puffergröße verantwortlich.
    /// </summary>
    public void RenderIntoBuffer(byte[] pixels, int totalWidth, int totalHeight, FileSystemNode root)
    {
        if (NativeRenderer.TryRenderIntoBuffer(pixels, totalWidth, totalHeight, root, ActiveCategories))
            return;

        var ctx    = new RenderContext(AppSettings.Instance, ActiveCategories);
        int stride = totalWidth * 4;
        unsafe
        {
            fixed (byte* ptr = pixels)
            {
                if (ctx.ShowBorders)
                    DrawAllBorders(ptr, stride, totalWidth, totalHeight, root, 0);
                RenderAllLeaves(ptr, stride, totalWidth, totalHeight, root, 0, 0.0, 0.0, 0.0, 0.0, in ctx);
            }
        }
    }

    /// <summary>
    /// Rendert <paramref name="root"/> und danach den synthetischen
    /// <paramref name="freeNode"/> (freier Speicher) in denselben Puffer.
    /// Beide Knoten müssen vorher mit Bounds versehen worden sein.
    /// </summary>
    public byte[] RenderWithFreeNode(FileSystemNode root, FileSystemNode freeNode,
                                     int width, int height)
    {
        if (NativeRenderer.TryRender([root, freeNode], width, height, ActiveCategories, out var nativePixels))
            return nativePixels;

        var ctx    = new RenderContext(AppSettings.Instance, ActiveCategories);
        int stride = width * 4;
        var pixels = new byte[stride * height];

        // Hintergrund füllen (#1A1A2E → BGRx)
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i + 0] = 0x2E;
            pixels[i + 1] = 0x1A;
            pixels[i + 2] = 0x1A;
            pixels[i + 3] = 0xFF;
        }

        unsafe
        {
            fixed (byte* ptr = pixels)
            {
                if (ctx.ShowBorders)
                {
                    DrawAllBorders(ptr, stride, width, height, root,     0);
                    DrawAllBorders(ptr, stride, width, height, freeNode, 0);
                }
                RenderAllLeaves(ptr, stride, width, height, root,     0, 0.0, 0.0, 0.0, 0.0, in ctx);
                RenderAllLeaves(ptr, stride, width, height, freeNode, 0, 0.0, 0.0, 0.0, 0.0, in ctx);
            }
        }

        return pixels;
    }


    // -- Pass 1: alle Verzeichnis-Raender zeichnen --
    private static unsafe void DrawAllBorders(byte* ptr, int stride, int imgW, int imgH,
                                               FileSystemNode node, int depth)
    {
        if (!node.IsDirectory) return;
        var bounds = node.Bounds;
        if (bounds.Width < 1 || bounds.Height < 1) return;
        DrawBorder(ptr, stride, imgW, imgH, bounds, depth);
        foreach (var child in node.Children)
            DrawAllBorders(ptr, stride, imgW, imgH, child, depth + 1);
    }

    // -- Pass 2: alle Blattknoten rendern (immer ueber Raendern) --
    private static unsafe void RenderAllLeaves(byte* ptr, int stride, int imgW, int imgH,
                                               FileSystemNode node, int depth,
                                               double ax, double bx, double ay, double by,
                                               in RenderContext ctx)
    {
        var bounds = node.Bounds;
        if (bounds.Width < 1 || bounds.Height < 1) return;

        double f  = ctx.CushionHeight * Math.Pow(ctx.CushionDecay, depth);
        double cx = (bounds.Left + bounds.Right)  * 0.5;
        double cy = (bounds.Top  + bounds.Bottom) * 0.5;
        double rx = (bounds.Right  - bounds.Left) * 0.5;
        double ry = (bounds.Bottom - bounds.Top)  * 0.5;

        if (rx > 0.5 && ry > 0.5)
        {
            ax += -f / rx;
            bx +=  2.0 * f * cx / rx;
            ay += -f / ry;
            by +=  2.0 * f * cy / ry;
        }

        if (!node.IsDirectory)
            RenderLeaf(ptr, stride, imgW, imgH, node, ax, bx, ay, by, in ctx);
        else
            foreach (var child in node.Children)
                RenderAllLeaves(ptr, stride, imgW, imgH, child, depth + 1, ax, bx, ay, by, in ctx);
    }

    // -- Blattknoten pixel-genau rendern (metallischer Blinn-Phong Glanz) --
    private static unsafe void RenderLeaf(byte* ptr, int stride, int imgW, int imgH,
                                          FileSystemNode node,
                                          double ax, double bx, double ay, double by,
                                          in RenderContext ctx)
    {
        var bounds = node.Bounds;
        int x1 = Math.Max(0, (int)Math.Ceiling(bounds.Left));
        int y1 = Math.Max(0, (int)Math.Ceiling(bounds.Top));
        int x2 = Math.Min(imgW - 1, (int)Math.Floor(bounds.Right)  - 1);
        int y2 = Math.Min(imgH - 1, (int)Math.Floor(bounds.Bottom) - 1);

        if (x2 < x1 || y2 < y1) return;

        var baseColor = ColorScheme.GetColor(node.Extension);

        // Kategorie-Filter: inaktive Dateien dunkelgrau -- eine explizit gesetzte Override-Farbe
        // (z. B. aus einem Farbprofil) bleibt davon ausgenommen, siehe NativeRenderer.Flatten.
        bool dimmed = ctx.ActiveCategories != null
                   && !ColorScheme.HasOverride(node.Extension)
                   && !ctx.ActiveCategories.Contains(ColorScheme.GetCategory(node.Extension));
        if (dimmed) baseColor = Color.FromRgb(45, 45, 52);

        float cr = baseColor.R / 255f;
        float cg = baseColor.G / 255f;
        float cb = baseColor.B / 255f;

        for (int y = y1; y <= y2; y++)
        {
            double dhy = 2.0 * ay * y + by;
            byte*  row = ptr + y * stride;

            for (int x = x1; x <= x2; x++)
            {
                double dhx = 2.0 * ax * x + bx;

                // Oberflaechennormale (normiert)
                double nx = -dhx;
                double ny = -dhy;
                double invLen = 1.0 / Math.Sqrt(nx * nx + ny * ny + 1.0);
                nx *= invLen;
                ny *= invLen;
                double nz = invLen;

                // Diffuse (Lambert)
                double diff     = Math.Max(0.0, nx * Lx + ny * Ly + nz * Lz);
                double diffComp = ctx.Ia + ctx.Is * diff;

                // Spiegelreflexion (Blinn-Phong, metallischer Glanz) nh^20
                double nh  = Math.Max(0.0, nx * Hx + ny * Hy + nz * Hz);
                double nh2 = nh  * nh;
                double nh4 = nh2 * nh2;
                double spec = nh4 * nh4 * nh4 * nh4 * nh4;   // nh^20

                // Metallisches Specular: Basisfarbe 55% + Weiss 45%
                const double S = 0.70;
                double specR = S * spec * (cr * 0.55 + 0.45);
                double specG = S * spec * (cg * 0.55 + 0.45);
                double specB = S * spec * (cb * 0.55 + 0.45);

                byte* px = row + x * 4;
                px[0] = ClampToByte(cb * diffComp + specB);
                px[1] = ClampToByte(cg * diffComp + specG);
                px[2] = ClampToByte(cr * diffComp + specR);
                px[3] = 0xFF;
            }
        }
    }



    // ── Verzeichnis-Rand ─────────────────────────────────────────────────────
    private static unsafe void DrawBorder(byte* ptr, int stride, int imgW, int imgH,
                                          Rect bounds, int depth)
    {
        int thickness = depth <= 1 ? 2 : 1;

        int x1 = Math.Max(0, (int)bounds.Left);
        int y1 = Math.Max(0, (int)bounds.Top);
        int x2 = Math.Min(imgW - 1, (int)bounds.Right  - 1);
        int y2 = Math.Min(imgH - 1, (int)bounds.Bottom - 1);

        if (x2 <= x1 || y2 <= y1) return;

        byte bv = 0x0F, gv = 0x0F, rv = 0x1A;

        for (int t = 0; t < thickness; t++)
        {
            int tx1 = x1 + t, ty1 = y1 + t;
            int tx2 = x2 - t, ty2 = y2 - t;
            if (tx2 <= tx1 || ty2 <= ty1) break;

            for (int x = tx1; x <= tx2; x++)
            {
                WritePixel(ptr, stride, x, ty1, bv, gv, rv);
                WritePixel(ptr, stride, x, ty2, bv, gv, rv);
            }
            for (int y = ty1 + 1; y < ty2; y++)
            {
                WritePixel(ptr, stride, tx1, y, bv, gv, rv);
                WritePixel(ptr, stride, tx2, y, bv, gv, rv);
            }
        }
    }

    private static unsafe void WritePixel(byte* ptr, int stride,
                                          int x, int y, byte b, byte g, byte r)
    {
        byte* px = ptr + y * stride + x * 4;
        px[0] = b; px[1] = g; px[2] = r; px[3] = 0xFF;
    }
    private static byte ClampToByte(double v)
        => v <= 0.0 ? (byte)0 : v >= 1.0 ? (byte)255 : (byte)(v * 255.0 + 0.5);
    // ── Hit-Test ─────────────────────────────────────────────────────────────
    public static FileSystemNode? HitTest(FileSystemNode root, double px, double py)
        => HitTestNode(root, px, py);

    private static FileSystemNode? HitTestNode(FileSystemNode node, double px, double py)
    {
        if (!node.Bounds.Contains(new Point(px, py))) return null;

        foreach (var child in node.Children)
        {
            var hit = HitTestNode(child, px, py);
            if (hit != null) return hit;
        }
        return node;
    }

    public static IEnumerable<Rect> GetAncestorBounds(FileSystemNode node)
    {
        var n = node;
        while (n != null)
        {
            yield return n.Bounds;
            n = n.Parent;
        }
    }
}
