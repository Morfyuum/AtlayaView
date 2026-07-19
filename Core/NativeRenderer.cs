using System.Runtime.InteropServices;

namespace AtlayaView.Core;

internal static class NativeRenderer
{
    private const string LibraryName = "atlaya_renderer";

    private static bool _disabled;

    [DllImport(LibraryName, EntryPoint = "atlaya_render_scene", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe byte RenderScene(
        NativeLeaf* leaves,
        int leafCount,
        NativeBorder* borders,
        int borderCount,
        int width,
        int height,
        double ambientLight,
        int showBorders,
        byte* pixels,
        nuint pixelsLength);

    [DllImport(LibraryName, EntryPoint = "atlaya_render_overlay", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe byte RenderOverlay(
        NativeLeaf* leaves,
        int leafCount,
        NativeBorder* borders,
        int borderCount,
        int width,
        int height,
        double ambientLight,
        int showBorders,
        byte* pixels,
        nuint pixelsLength);

    [DllImport(LibraryName, EntryPoint = "atlaya_renderer_version", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetRendererVersionPtr();

    public static string RendererVersion
    {
        get
        {
            if (_disabled)
                return "nicht verfuegbar";

            try
            {
                var versionPtr = GetRendererVersionPtr();
                if (versionPtr == IntPtr.Zero)
                    return "unbekannt";

                return Marshal.PtrToStringAnsi(versionPtr) ?? "unbekannt";
            }
            catch (DllNotFoundException)
            {
                _disabled = true;
                return "nicht verfuegbar";
            }
            catch (EntryPointNotFoundException)
            {
                _disabled = true;
                return "nicht verfuegbar";
            }
            catch (BadImageFormatException)
            {
                _disabled = true;
                return "nicht verfuegbar";
            }
        }
    }

    public static bool TryRender(
        FileSystemNode root,
        int width,
        int height,
        IReadOnlySet<string>? activeCategories,
        out byte[] pixels)
        => TryRender([root], width, height, activeCategories, out pixels);

    public static bool TryRender(
        IReadOnlyList<FileSystemNode> roots,
        int width,
        int height,
        IReadOnlySet<string>? activeCategories,
        out byte[] pixels)
    {
        pixels = Array.Empty<byte>();

        if (_disabled || width <= 0 || height <= 0 || roots.Count == 0)
            return false;

        if (!TryFlatten(roots, activeCategories, out var leafArray, out var borderArray))
            return false;

        pixels = new byte[width * height * 4];

        try
        {
            unsafe
            {
                fixed (NativeLeaf* leafPtr = leafArray)
                fixed (NativeBorder* borderPtr = borderArray)
                fixed (byte* pixelPtr = pixels)
                {
                    var settings = AppSettings.Instance;
                    var ok = RenderScene(
                        leafArray.Length == 0 ? null : leafPtr,
                        leafArray.Length,
                        borderArray.Length == 0 ? null : borderPtr,
                        borderArray.Length,
                        width,
                        height,
                        settings.AmbientLight,
                        settings.ShowBorders ? 1 : 0,
                        pixelPtr,
                        (nuint)pixels.Length);

                    return ok != 0;
                }
            }
        }
        catch (DllNotFoundException)
        {
            _disabled = true;
            pixels = Array.Empty<byte>();
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            _disabled = true;
            pixels = Array.Empty<byte>();
            return false;
        }
        catch (BadImageFormatException)
        {
            _disabled = true;
            pixels = Array.Empty<byte>();
            return false;
        }
    }

    public static bool TryRenderIntoBuffer(
        byte[] pixels,
        int width,
        int height,
        FileSystemNode root,
        IReadOnlySet<string>? activeCategories)
    {
        if (_disabled || width <= 0 || height <= 0 || pixels.Length < width * height * 4)
            return false;

        if (!TryFlatten([root], activeCategories, out var leafArray, out var borderArray))
            return false;

        try
        {
            unsafe
            {
                fixed (NativeLeaf* leafPtr = leafArray)
                fixed (NativeBorder* borderPtr = borderArray)
                fixed (byte* pixelPtr = pixels)
                {
                    var settings = AppSettings.Instance;
                    var ok = RenderOverlay(
                        leafArray.Length == 0 ? null : leafPtr,
                        leafArray.Length,
                        borderArray.Length == 0 ? null : borderPtr,
                        borderArray.Length,
                        width,
                        height,
                        settings.AmbientLight,
                        settings.ShowBorders ? 1 : 0,
                        pixelPtr,
                        (nuint)pixels.Length);

                    return ok != 0;
                }
            }
        }
        catch (DllNotFoundException)
        {
            _disabled = true;
            pixels = Array.Empty<byte>();
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            _disabled = true;
            pixels = Array.Empty<byte>();
            return false;
        }
        catch (BadImageFormatException)
        {
            _disabled = true;
            pixels = Array.Empty<byte>();
            return false;
        }
    }

    private static bool TryFlatten(
        IReadOnlyList<FileSystemNode> roots,
        IReadOnlySet<string>? activeCategories,
        out NativeLeaf[] leafArray,
        out NativeBorder[] borderArray)
    {
        leafArray = [];
        borderArray = [];

        if (_disabled || roots.Count == 0)
            return false;

        var settings = AppSettings.Instance;
        var leaves = new List<NativeLeaf>(1024);
        var borders = settings.ShowBorders ? new List<NativeBorder>(256) : [];

        foreach (var root in roots)
            Flatten(root, 0, 0.0, 0.0, 0.0, 0.0, activeCategories, settings, leaves, borders);

        leafArray = leaves.ToArray();
        borderArray = borders.ToArray();
        return true;
    }

    private static void Flatten(
        FileSystemNode node,
        int depth,
        double ax,
        double bx,
        double ay,
        double by,
        IReadOnlySet<string>? activeCategories,
        AppSettings settings,
        List<NativeLeaf> leaves,
        List<NativeBorder> borders)
    {
        var bounds = node.Bounds;
        if (bounds.Width < 1 || bounds.Height < 1)
            return;

        double nextAx = ax;
        double nextBx = bx;
        double nextAy = ay;
        double nextBy = by;

        double factor = settings.CushionHeight * Math.Pow(settings.CushionDecay, depth);
        double centerX = (bounds.Left + bounds.Right) * 0.5;
        double centerY = (bounds.Top + bounds.Bottom) * 0.5;
        double radiusX = (bounds.Right - bounds.Left) * 0.5;
        double radiusY = (bounds.Bottom - bounds.Top) * 0.5;

        if (radiusX > 0.5 && radiusY > 0.5)
        {
            nextAx += -factor / radiusX;
            nextBx += 2.0 * factor * centerX / radiusX;
            nextAy += -factor / radiusY;
            nextBy += 2.0 * factor * centerY / radiusY;
        }

        if (node.IsDirectory)
        {
            if (settings.ShowBorders)
                borders.Add(new NativeBorder(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, depth));

            foreach (var child in node.Children)
                Flatten(child, depth + 1, nextAx, nextBx, nextAy, nextBy, activeCategories, settings, leaves, borders);

            return;
        }

        var color = ColorScheme.GetColor(node.Extension);
        // Eine explizit gesetzte Farbe (z. B. aus einem Farbprofil) bleibt sichtbar, auch wenn
        // der Legenden-Kategoriefilter gerade eine andere Kategorie isoliert -- sonst wirkt ein
        // gerade zugewiesenes Farbprofil so, als würde es gar nicht angewendet (Bugreport:
        // "Farbprofil wird nicht auf die Darstellung angewendet").
        if (activeCategories != null && !ColorScheme.HasOverride(node.Extension)
            && !activeCategories.Contains(ColorScheme.GetCategory(node.Extension)))
            color = System.Windows.Media.Color.FromRgb(45, 45, 52);

        leaves.Add(new NativeLeaf(
            bounds.Left,
            bounds.Top,
            bounds.Right,
            bounds.Bottom,
            nextAx,
            nextBx,
            nextAy,
            nextBy,
            color.B,
            color.G,
            color.R,
            0xFF));
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeLeaf(
        double left,
        double top,
        double right,
        double bottom,
        double ax,
        double bx,
        double ay,
        double by,
        byte blue,
        byte green,
        byte red,
        byte alpha)
    {
        public readonly double Left = left;
        public readonly double Top = top;
        public readonly double Right = right;
        public readonly double Bottom = bottom;
        public readonly double Ax = ax;
        public readonly double Bx = bx;
        public readonly double Ay = ay;
        public readonly double By = by;
        public readonly byte Blue = blue;
        public readonly byte Green = green;
        public readonly byte Red = red;
        public readonly byte Alpha = alpha;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeBorder(double left, double top, double right, double bottom, int depth)
    {
        public readonly double Left = left;
        public readonly double Top = top;
        public readonly double Right = right;
        public readonly double Bottom = bottom;
        public readonly int Depth = depth;
    }
}