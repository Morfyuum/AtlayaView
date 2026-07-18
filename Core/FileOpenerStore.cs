using System.Diagnostics;
using System.IO;

namespace AtlayaView.Core;

/// <summary>
/// Verwaltet die Zuordnung Dateiendung → Programm-Pfad.
/// Leerer Eintrag = Windows-Systemstandard (ShellExecute).
/// </summary>
public static class FileOpenerStore
{
    private static readonly Dictionary<string, string> _openers =
        new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, string> Openers => _openers;

    public static string? GetOpener(string extension) =>
        _openers.TryGetValue(extension, out var exe) ? exe : null;

    public static void SetOpener(string extension, string exePath) =>
        _openers[extension] = exePath;

    public static void RemoveOpener(string extension) =>
        _openers.Remove(extension);

    public static void Clear() => _openers.Clear();

    public static void Load(Dictionary<string, string> data)
    {
        _openers.Clear();
        foreach (var kv in data)
            _openers[kv.Key] = kv.Value;
    }

    // ── Bekannte Viewer-Programme ermitteln ──────────────────────────────────
    public static List<(string Name, string Path)> FindInstalledViewers()
    {
        var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<(string, string)>();

        var candidates = new (string Name, string[] Paths)[]
        {
            ("IrfanView",
             [@"C:\Program Files\IrfanView\i_view64.exe",
              @"C:\Program Files (x86)\IrfanView\i_view64.exe",
              @"C:\Program Files (x86)\IrfanView\i_view32.exe"]),
            ("VLC",
             [@"C:\Program Files\VideoLAN\VLC\vlc.exe",
              @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"]),
            ("Notepad++",
             [@"C:\Program Files\Notepad++\notepad++.exe",
              @"C:\Program Files (x86)\Notepad++\notepad++.exe"]),
            ("Notepad",
             [@"C:\Windows\System32\notepad.exe",
              @"C:\Windows\notepad.exe"]),
            ("Paint",
             [@"C:\Windows\System32\mspaint.exe"]),
            ("WordPad",
             [@"C:\Program Files\Windows NT\Accessories\wordpad.exe"]),
            ("Windows Media Player",
             [@"C:\Program Files\Windows Media Player\wmplayer.exe",
              @"C:\Program Files (x86)\Windows Media Player\wmplayer.exe"]),
        };

        foreach (var (name, paths) in candidates)
        {
            foreach (var path in paths)
            {
                if (File.Exists(path) && seen.Add(path))
                {
                    results.Add((name, path));
                    break;
                }
            }
        }

        return results;
    }

    // ── Datei öffnen ─────────────────────────────────────────────────────────
    public static void OpenFile(string filePath)
    {
        try
        {
            var ext    = Path.GetExtension(filePath);
            var opener = string.IsNullOrEmpty(ext) ? null : GetOpener(ext);

            if (!string.IsNullOrEmpty(opener) && File.Exists(opener))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName        = opener,
                    Arguments       = $"\"{filePath}\"",
                    UseShellExecute = false
                });
            }
            else
            {
                // Fallback: Windows-Standardprogramm
                Process.Start(new ProcessStartInfo
                {
                    FileName        = filePath,
                    UseShellExecute = true
                });
            }
        }
        catch { /* Öffnen fehlgeschlagen – ignorieren */ }
    }
}
