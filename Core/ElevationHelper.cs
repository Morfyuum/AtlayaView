// Chris Deliga / CNS Capecter NetworXs System / atlaya.capecter.com
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace AtlayaView.Core;

/// <summary>
/// Prüft, ob der aktuelle Prozess mit Administratorrechten läuft, und kann sich bei Bedarf
/// selbst elevated neu starten (gleiches Muster wie <see cref="SelfUpdater"/>s
/// <c>Verb = "runas"</c>-Neustart für den Update-Installer).
/// </summary>
public static class ElevationHelper
{
    private static bool? _isElevated;

    /// <summary>True, wenn der aktuelle Prozess mit erhöhten Rechten läuft.</summary>
    public static bool IsElevated
    {
        get
        {
            if (_isElevated is bool cached) return cached;
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                _isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                _isElevated = false;
            }
            return _isElevated.Value;
        }
    }

    /// <summary>
    /// Startet AtlayaView per UAC-Anfrage (<c>runas</c>) elevated neu und beendet die aktuelle,
    /// nicht-erhöhte Instanz. Gibt <c>false</c> zurück, wenn der Neustart nicht angestoßen
    /// werden konnte (z. B. Benutzer bricht den UAC-Dialog ab) – die aktuelle Instanz läuft
    /// dann unverändert weiter.
    /// </summary>
    public static bool TryRelaunchElevated()
    {
        if (IsElevated) return true;

        try
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return false;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
            };
            Process.Start(psi);
            Application.Current.Shutdown();
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED: Benutzer hat den UAC-Dialog abgelehnt.
            return false;
        }
        catch
        {
            return false;
        }
    }
}
