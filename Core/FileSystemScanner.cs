using System.IO;
using System.Threading;

namespace AtlayaView.Core;

/// <summary>
/// Hochperformanter paralleler Dateibaum-Scanner.
///
/// Ablauf:
///   Phase 1 – Verzeichniszählung (EnumerateDirectories, kein Datei-Stat) → liefert Denominator
///   Phase 2 – Vollständiger Scan (Parallel.For für Tiefe 0-2, sequenziell tiefer)
///
/// Events: FilesScanned (alle 200 Dateien), StatusChanged (rate-limitiert ≤4/s),
///         ProgressChanged (0-100 %, ETA-Text).
/// </summary>
public sealed class FileSystemScanner
{
    public readonly record struct ReadProgress(long EntriesRead, double ProgressPercent, string Eta);

    public enum ScanPhase
    {
        Reading,
        Processing
    }

    // ── Events ───────────────────────────────────────────────────────────────
    public event Action<long>? FilesScanned;    // aktuelle Dateianzahl
    public event Action<string>? StatusChanged;   // aktueller Pfad (rate-limitiert)
    public event Action<double, string>? ProgressChanged; // Fortschritt 0-100, ETA-Text
    public event Action<ScanPhase>? PhaseChanged;
    public event Action<ReadProgress>? ReadProgressChanged;

    // ── Interne Felder ────────────────────────────────────────────────────────
    private long _fileCount;
    private long _dirsDone;
    private long _dirsTotal;
    private long _lastStatusMs;
    private DateTime _scanStart;
    private double? _readEtaSecondsEma;
    private double? _processEtaSecondsEma;
    private readonly SemaphoreSlim _parallelGate = new(Math.Clamp(Environment.ProcessorCount * 2, 8, 24));

    // Begrenzte Task-Parallelitaet fuer schnelle SSD-Scans ohne den Thread-Pool zu fluten.
    private static readonly int _parallelDepthLimit = 4;

    // Phase 1 (Zählung): eigene Interlocked-Zähler, da mehrere Tasks gleichzeitig zählen.
    private long _readDiscoveredDirs;
    private long _readProcessedDirs;
    private long _readDiscoveredEntries;
    private long _readLastProgressMs;

    // ── Öffentliche API ──────────────────────────────────────────────────────
    public async Task<FileSystemNode> ScanAsync(string rootPath, CancellationToken ct = default)
    {
        _fileCount = 0;
        _dirsDone = 0;
        _dirsTotal = 0;
        _lastStatusMs = 0;
        _scanStart = DateTime.UtcNow;
        _readEtaSecondsEma = null;
        _processEtaSecondsEma = null;

        // Phase 1: Verzeichnisstruktur zählen (nur Ordnernamen, kein Datei-Stat) – parallel,
        // damit dieser Vorpass nicht (wie früher) den Großteil der Zeit einzelsträngig läuft
        // und das Volumen effektiv zweimal einzelsträngig abgelaufen wird.
        PhaseChanged?.Invoke(ScanPhase.Reading);
        _dirsTotal = await CountAllDirsAsync(rootPath, ct);

        // Phase 2: Vollständiger Scan mit Fortschrittsmeldungen
        PhaseChanged?.Invoke(ScanPhase.Processing);
        return await ScanDirectoryAsync(rootPath, null, 0, ct);
    }

    // ── Phase 1: Schnelle, parallele Verzeichniszählung ───────────────────────
    private async Task<long> CountAllDirsAsync(string root, CancellationToken ct)
    {
        _readDiscoveredDirs = 1;
        _readProcessedDirs = 0;
        _readDiscoveredEntries = 0;
        _readLastProgressMs = 0;
        var scanStarted = DateTime.UtcNow;

        try
        {
            await CountDirAsync(root, 0, ct, scanStarted);
        }
        catch (OperationCanceledException) { throw; }
        catch { /* Zugriff verweigert, Netzwerkfehler o. Ä. */ }

        ReadProgressChanged?.Invoke(new ReadProgress(Interlocked.Read(ref _readDiscoveredEntries), 100, string.Empty));
        return Math.Max(Interlocked.Read(ref _readDiscoveredDirs), 1);
    }

    private async Task CountDirAsync(string path, int depth, CancellationToken ct, DateTime scanStarted)
    {
        ct.ThrowIfCancellationRequested();

        var enumOpts = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint   // keine Symlinks/Junctions
        };

        var subdirs = new List<string>();
        try
        {
            foreach (var subdir in Directory.EnumerateDirectories(path, "*", enumOpts))
            {
                ct.ThrowIfCancellationRequested();
                subdirs.Add(subdir);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        long fileCount = 0;
        try
        {
            foreach (var _ in Directory.EnumerateFiles(path, "*", enumOpts))
            {
                ct.ThrowIfCancellationRequested();
                fileCount++;
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        if (subdirs.Count > 0)
        {
            Interlocked.Add(ref _readDiscoveredDirs, subdirs.Count);
            Interlocked.Add(ref _readDiscoveredEntries, subdirs.Count);
        }
        if (fileCount > 0)
            Interlocked.Add(ref _readDiscoveredEntries, fileCount);

        Interlocked.Increment(ref _readProcessedDirs);
        ReportReadProgress(scanStarted);

        if (subdirs.Count == 0)
            return;

        var tasks = new Task[subdirs.Count];
        for (int i = 0; i < subdirs.Count; i++)
            tasks[i] = CountSubdirAsync(subdirs[i], depth + 1, ct, scanStarted);
        await Task.WhenAll(tasks);
    }

    private Task CountSubdirAsync(string path, int depth, CancellationToken ct, DateTime scanStarted)
    {
        ct.ThrowIfCancellationRequested();

        if (depth <= _parallelDepthLimit && _parallelGate.Wait(0))
        {
            return Task.Run(async () =>
            {
                try
                {
                    await CountDirAsync(path, depth, ct, scanStarted);
                }
                catch (OperationCanceledException) { throw; }
                catch { }
                finally { _parallelGate.Release(); }
            }, ct);
        }

        return CountDirInlineAsync(path, depth, ct, scanStarted);
    }

    private async Task CountDirInlineAsync(string path, int depth, CancellationToken ct, DateTime scanStarted)
    {
        try
        {
            await CountDirAsync(path, depth, ct, scanStarted);
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private void ReportReadProgress(DateTime scanStarted)
    {
        long nowMs = Environment.TickCount64;
        long prev = Interlocked.Read(ref _readLastProgressMs);
        if (nowMs - prev < 120) return;
        if (Interlocked.CompareExchange(ref _readLastProgressMs, nowMs, prev) != prev) return;

        long discoveredDirs = Interlocked.Read(ref _readDiscoveredDirs);
        long processedDirs = Interlocked.Read(ref _readProcessedDirs);
        long discoveredEntries = Interlocked.Read(ref _readDiscoveredEntries);
        long remaining = Math.Max(0, discoveredDirs - processedDirs);

        double progress = discoveredDirs > 0
            ? Math.Min(99.0, 100.0 * processedDirs / discoveredDirs)
            : 0;
        double elapsed = (DateTime.UtcNow - scanStarted).TotalSeconds;
        string eta = string.Empty;

        if (processedDirs >= 8 && remaining > 0 && elapsed > 1.5)
        {
            double avgNewDirsPerProcessed = processedDirs > 0
                ? Math.Max(0, (discoveredDirs - processedDirs) / (double)processedDirs)
                : 0;
            double estimatedRemainingDirs = remaining * (1.0 + avgNewDirsPerProcessed);
            double rawEtaSeconds = elapsed / processedDirs * estimatedRemainingDirs;
            double smoothedEta = SmoothEta(rawEtaSeconds, ref _readEtaSecondsEma, 0.18);
            eta = FormatEta(smoothedEta);
        }

        ReadProgressChanged?.Invoke(new ReadProgress(discoveredEntries, progress, eta));
    }

    // ── Phase 2: Vollständiger Scan ───────────────────────────────────────────
    private async Task<FileSystemNode> ScanDirectoryAsync(string path, FileSystemNode? parent,
                                                          int depth, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var info = new DirectoryInfo(path);
        var node = new FileSystemNode
        {
            Name = depth == 0 ? path : info.Name,
            FullPath = path,
            IsDirectory = true,
            LastModified = SafeGetLastWriteTime(info),
            Parent = parent,
            Depth = depth
        };

        // StatusChanged: rate-limitiert auf max 4× pro Sekunde
        long nowMs = Environment.TickCount64;
        long prev = Interlocked.Read(ref _lastStatusMs);
        if (nowMs - prev > 250 &&
            Interlocked.CompareExchange(ref _lastStatusMs, nowMs, prev) == prev)
            StatusChanged?.Invoke(path);

        // ── Dateien (EnumerateFiles startet sofort, kein Array-Alloc) ─────────
        try
        {
            foreach (var f in info.EnumerateFiles())
            {
                ct.ThrowIfCancellationRequested();
                var fileNode = new FileSystemNode
                {
                    Name = f.Name,
                    FullPath = f.FullName,
                    IsDirectory = false,
                    Size = f.Length,
                    Extension = f.Extension.ToLowerInvariant(),
                    LastModified = SafeGetLastWriteTime(f),
                    Parent = node,
                    Depth = depth + 1
                };
                node.Children.Add(fileNode);

                long c = Interlocked.Increment(ref _fileCount);
                if (c % 200 == 0) FilesScanned?.Invoke(c);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }

        // ── Unterordner ───────────────────────────────────────────────────────
        DirectoryInfo[] subdirInfos;
        try
        {
            // Reparse-Points (Symlinks, Junction Points) überspringen → keine Schleifen
            subdirInfos = Array.FindAll(
                info.GetDirectories(),
                d => (d.Attributes & FileAttributes.ReparsePoint) == 0);
        }
        catch { subdirInfos = []; }

        if (subdirInfos.Length > 0)
        {
            var childTasks = new Task<FileSystemNode?>[subdirInfos.Length];
            for (int i = 0; i < subdirInfos.Length; i++)
                childTasks[i] = ScanSubdirectoryAsync(subdirInfos[i], node, depth + 1, ct);

            var childNodes = await Task.WhenAll(childTasks);

            foreach (var child in childNodes)
                if (child != null) node.Children.Add(child);
        }

        node.Size = node.Children.Sum(c => c.Size);

        // ── Fortschritt & ETA ─────────────────────────────────────────────────
        long done = Interlocked.Increment(ref _dirsDone);
        long total = Interlocked.Read(ref _dirsTotal);
        if (total > 0 && (done % 8 == 0 || done == total))
        {
            double pct = Math.Min(99.5, 100.0 * done / total);
            double elapsed = (DateTime.UtcNow - _scanStart).TotalSeconds;
            string eta = string.Empty;

            if (done >= 12 && total > done && elapsed > 1.5)
            {
                double rawEtaSeconds = elapsed / done * (total - done);
                double smoothedEta = SmoothEta(rawEtaSeconds, ref _processEtaSecondsEma, 0.15);
                eta = FormatEta(smoothedEta);
            }

            ProgressChanged?.Invoke(pct, eta);
        }

        return node;
    }

    private Task<FileSystemNode?> ScanSubdirectoryAsync(
        DirectoryInfo subdirInfo,
        FileSystemNode parent,
        int depth,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (depth <= _parallelDepthLimit && _parallelGate.Wait(0))
        {
            return Task.Run(async () =>
            {
                try
                {
                    return await ScanDirectoryAsync(subdirInfo.FullName, parent, depth, ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    _parallelGate.Release();
                }
            }, ct);
        }

        return ScanSubdirectoryInlineAsync(subdirInfo.FullName, parent, depth, ct);
    }

    private async Task<FileSystemNode?> ScanSubdirectoryInlineAsync(
        string path,
        FileSystemNode parent,
        int depth,
        CancellationToken ct)
    {
        try
        {
            return await ScanDirectoryAsync(path, parent, depth, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    private static double SmoothEta(double currentSeconds, ref double? ema, double alpha)
    {
        if (double.IsNaN(currentSeconds) || double.IsInfinity(currentSeconds) || currentSeconds <= 0)
            return ema ?? 0;

        ema = ema.HasValue
            ? (ema.Value * (1.0 - alpha)) + (currentSeconds * alpha)
            : currentSeconds;

        return ema.Value;
    }

    private static string FormatEta(double seconds)
    {
        if (seconds <= 0) return string.Empty;
        if (seconds < 10) return $"~{Math.Max(1, (int)Math.Round(seconds))} s";
        if (seconds < 60) return $"~{(int)(Math.Round(seconds / 5) * 5)} s";
        if (seconds < 600) return $"~{Math.Max(1, (int)Math.Round(seconds / 10) * 10)} s";
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return $"~{m}:{s:D2} min";
    }

    private static DateTime SafeGetLastWriteTime(FileSystemInfo info)
    {
        try { return info.LastWriteTime; } catch { return DateTime.MinValue; }
    }
}
