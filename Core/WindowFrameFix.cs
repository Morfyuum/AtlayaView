using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace AtlayaView.Core;

/// <summary>
/// Bekannter WPF/PerMonitorV2-Bug (vgl. dotnet/wpf#6103): startet ein Fenster (Haupt- oder
/// Dialogfenster) auf einem nicht-primären Monitor mit abweichender DPI-Skalierung, bleibt der
/// Fensterrahmen zunächst nicht interaktiv – Titelleiste lässt sich nicht anfassen, das Fenster
/// nicht auf einen anderen Monitor verschieben, bis man tatsächlich am Fensterrand zieht.
/// Ursache ist ein intern veralteter DPI-Transform von WPFs HwndTarget, der erst durch eine
/// ECHTE Größenänderung (WM_SIZE) neu berechnet wird – ein reines SWP_FRAMECHANGED (nur
/// Neuzeichnen des Rahmens, keine echte Größenänderung) reicht dafür NICHT aus (per Bugreport
/// von Chris am 2026-07-19 verifiziert: v2.0.23 löste das Problem trotz SWP_FRAMECHANGED nicht,
/// erst manuelles Ziehen am rechten Rand half). Apply() automatisiert stattdessen genau das:
/// eine 1-Pixel-Breitenänderung und sofort zurück, unsichtbar für den Nutzer, aber mit
/// echtem WM_SIZE-Zyklus, der den DPI-Transform korrekt neu berechnet.
/// </summary>
internal static class WindowFrameFix
{
    public static void Apply(Window window)
    {
        window.Loaded += (_, _) =>
            window.Dispatcher.InvokeAsync(() => Nudge(window), DispatcherPriority.ApplicationIdle);
    }

    private static void Nudge(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        const uint flags = NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE;

        // Echte Größenänderung um 1px und sofort zurück -- erzwingt den WM_SIZE-Zyklus, den
        // WPFs HwndTarget braucht, um seinen DPI-Transform fuer diesen Monitor neu zu berechnen.
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, width + 1, height, flags);
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, width, height, flags | NativeMethods.SWP_FRAMECHANGED);
    }

    private static class NativeMethods
    {
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }
    }
}
