using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;

namespace AtlayaView.Core;

/// <summary>
/// Lädt eine neue AtlayaView-Version herunter und tauscht die Dateien nach Prozessende
/// über ein generiertes PowerShell-Hilfsskript aus (AtlayaView ist portabel/ohne
/// Installer, kann sich also nicht selbst überschreiben, während es läuft). Ablauf:
/// 1) Download + Entpacken in einen Temp-Ordner (läuft NICHT am Installationsort).
/// 2) Erst nach vollständigem Erfolg: Hilfsskript schreiben + verdeckt starten.
/// 3) AtlayaView beendet sich selbst – das Skript wartet auf das Prozessende, kopiert
///    dann die neuen Dateien, startet die neue AtlayaView.exe und löscht sich selbst.
/// Schlägt Download/Entpacken fehl, bleibt die laufende Installation unangetastet.
/// </summary>
public static class SelfUpdater
{
    public static string InstallDir => AppContext.BaseDirectory.TrimEnd('\\', '/');

    /// <summary>true = framework-dependent Installation ("ohne .NET"-Download nötig hier
    /// NICHT gemeint – hier geht es um die AKTUELL installierte Variante: AtlayaView.dll
    /// liegt nur bei framework-dependent neben der EXE, self-contained hat sie nicht).</summary>
    public static bool IsFrameworkDependent() =>
        File.Exists(Path.Combine(InstallDir, "AtlayaView.dll"));

    /// <summary>Wählt die zur aktuell installierten Variante passende Download-URL.</summary>
    public static string? ResolveMatchingUrl(UpdateInfo info) =>
        IsFrameworkDependent() ? info.UrlFx : info.UrlFull;

    public static async Task<string> DownloadAndExtractAsync(
        string url, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Lade Update herunter …");
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        byte[] bytes = await client.GetByteArrayAsync(url, ct).ConfigureAwait(false);

        string stagingRoot = Path.Combine(
            Path.GetTempPath(), "AtlayaView_update_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingRoot);
        string zipPath = Path.Combine(stagingRoot, "update.zip");
        await File.WriteAllBytesAsync(zipPath, bytes, ct).ConfigureAwait(false);

        progress?.Report("Entpacke Update …");
        string extractDir = Path.Combine(stagingRoot, "extracted");
        ZipFile.ExtractToDirectory(zipPath, extractDir);
        return extractDir;
    }

    /// <summary>Prüft per Schreibprobe, ob <paramref name="dir"/> ohne erhöhte Rechte
    /// beschreibbar ist (z.B. NICHT der Fall bei "C:\Program Files\..." ohne Admin-Rechte).</summary>
    private static bool CanWriteTo(string dir)
    {
        try
        {
            string probe = Path.Combine(dir, $".atlayaview_write_probe_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "x");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Erzeugt und startet das Austausch-Skript (verdeckt, wartet auf Prozessende) und
    /// beendet AtlayaView danach. Muss auf dem UI-Thread aufgerufen werden. Liegt die
    /// Installation in einem geschützten Ordner (z.B. "Program Files"), wird das Skript
    /// mit Administratorrechten gestartet (UAC-Abfrage) -- sonst schlägt Copy-Item dort
    /// lautlos mit "Access Denied" fehl und AtlayaView startet unverändert neu.
    /// </summary>
    public static void LaunchSwapAndExit(string extractedDir)
    {
        int pid = Environment.ProcessId;
        string installDir = InstallDir;
        string exePath = Path.Combine(installDir, "AtlayaView.exe");
        string scriptPath = Path.Combine(Path.GetTempPath(), $"AtlayaView_apply_{Guid.NewGuid():N}.ps1");
        string logPath = Path.Combine(Path.GetTempPath(), "AtlayaView_apply.log");

        // Bei einem Fehler wird die alte Installation trotzdem neu gestartet (statt
        // spurlos zu verschwinden) -- der Fehler landet zusätzlich in logPath.
        string script =
            $"Wait-Process -Id {pid} -ErrorAction SilentlyContinue\r\n" +
            "Start-Sleep -Milliseconds 500\r\n" +
            "try {\r\n" +
            $"  Copy-Item -Path \"{extractedDir}\\*\" -Destination \"{installDir}\" -Recurse -Force -ErrorAction Stop\r\n" +
            "} catch {\r\n" +
            $"  \"$(Get-Date -Format o): $($_.Exception.Message)\" | Out-File -FilePath \"{logPath}\" -Encoding utf8 -Append\r\n" +
            "}\r\n" +
            $"Start-Process -FilePath \"{exePath}\"\r\n" +
            $"Remove-Item -LiteralPath \"{extractedDir}\" -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
            "Remove-Item -LiteralPath $MyInvocation.MyCommand.Path -Force -ErrorAction SilentlyContinue\r\n";
        File.WriteAllText(scriptPath, script);

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        if (!CanWriteTo(installDir))
        {
            psi.Verb = "runas";
        }
        Process.Start(psi);

        Application.Current.Shutdown();
    }
}
