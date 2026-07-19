using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AtlayaView.Core;

namespace AtlayaView.ViewModels;

/// <summary>
/// Haupt-ViewModel – verwaltet Scan, Navigation und aktuellen Zustand.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    // ── Felder ────────────────────────────────────────────────────────────────
    private static LocalizationManager Loc => LocalizationManager.Instance;

    private readonly TreemapLayout _layout = new();

    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _multiDriveCts;
    private int _scanGeneration;

    private FileSystemNode? _rootNode;
    private FileSystemNode? _displayRoot;
    private FileSystemNode? _hoveredNode;

    private bool _isScanning;
    private string _statusText = LocalizationManager.Instance.StatusReady;
    private string _scanInfo = string.Empty;
    private string _scanRootPath = string.Empty;
    private string _scanDriveInfo = string.Empty;
    private string _scanEtaText = string.Empty;
    private string _diskFreeText = string.Empty;
    private double _diskUsedPct = 0;
    private long _diskFreeBytes = 0;
    private long _diskTotalBytes = 0;
    private bool _showFreeSpaceCushion = true;
    private long _fileCount;
    private double _scanProgress = 0;
    private bool _isIndeterminate = false;
    private bool _isReadingPhase;

    private readonly Stack<FileSystemNode> _navBack = new();

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public MainViewModel()
    {
        // Commands
        OpenFolderCommand = new RelayCommand(_ => OpenFolder(), _ => !_isScanning);
        ScanDriveCommand = new RelayCommand(p => ScanDrive(p as string), _ => !_isScanning);
        NavigateIntoCommand = new RelayCommand(p => NavigateInto(p as FileSystemNode),
                                               p => p is FileSystemNode n && n.IsDirectory);
        NavigateUpCommand = new RelayCommand(_ => NavigateUp(), _ => CanNavigateUp);
        NavigateBackCommand = new RelayCommand(_ => NavigateBack(), _ => _navBack.Count > 0);
        CancelScanCommand = new RelayCommand(_ => CancelScan(), _ => _isScanning);

        // Laufwerke befüllen
        LoadDrives();

        // Sprache: berechnete Properties aktualisieren
        Loc.LanguageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TotalSizeText));
            OnPropertyChanged(nameof(HoverInfo));
            OnPropertyChanged(nameof(FileCountText));
            if (!_isScanning && _rootNode == null)
                StatusText = Loc.StatusReady;
            if (_diskTotalBytes > 0) RegenerateDiskInfo();
        };
    }

    // ── Properties ───────────────────────────────────────────────────────────
    public ObservableCollection<DriveInfo> Drives { get; } = [];
    public ObservableCollection<BreadcrumbItem> Breadcrumb { get; } = [];

    public FileSystemNode? DisplayRoot
    {
        get => _displayRoot;
        private set
        {
            Set(ref _displayRoot, value);
            OnPropertyChanged(nameof(CanNavigateUp));
            RaisedNavigateChanged();
            RefreshBreadcrumb();
            LayoutRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public FileSystemNode? HoveredNode
    {
        get => _hoveredNode;
        set { Set(ref _hoveredNode, value); OnPropertyChanged(nameof(HoverInfo)); }
    }

    public string HoverInfo => _hoveredNode == null
        ? string.Empty
        : $"{_hoveredNode.FullPath}   {FileSystemNode.FormatSize(_hoveredNode.Size)}" +
          (_hoveredNode.LastModified > DateTime.MinValue
              ? $"   {Loc.HoverModified} {_hoveredNode.LastModified:dd.MM.yyyy HH:mm}"
              : string.Empty);

    public bool IsScanning { get => _isScanning; private set => SetScanningState(value); }
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public string ScanInfo { get => _scanInfo; private set => Set(ref _scanInfo, value); }
    public string ScanEtaText { get => _scanEtaText; private set => Set(ref _scanEtaText, value); }
    public long FileCount { get => _fileCount; private set { Set(ref _fileCount, value); OnPropertyChanged(nameof(FileCountText)); } }
    public double ScanProgress { get => _scanProgress; private set => Set(ref _scanProgress, value); }
    public bool IsIndeterminate { get => _isIndeterminate; private set => Set(ref _isIndeterminate, value); }
    public string ScanRootPath { get => _scanRootPath; private set => Set(ref _scanRootPath, value); }
    public string ScanDriveInfo { get => _scanDriveInfo; private set => Set(ref _scanDriveInfo, value); }
    public bool CanNavigateUp => _displayRoot?.Parent != null && _displayRoot != _rootNode;
    public string DiskFreeText { get => _diskFreeText; private set => Set(ref _diskFreeText, value); }
    public double DiskUsedPct { get => _diskUsedPct; private set => Set(ref _diskUsedPct, value); }
    public long DiskFreeBytes { get => _diskFreeBytes; private set => Set(ref _diskFreeBytes, value); }
    public FileSystemNode? RootNode => _rootNode;
    public bool ShowFreeSpaceCushion
    {
        get => _showFreeSpaceCushion;
        set { Set(ref _showFreeSpaceCushion, value); LayoutRequested?.Invoke(this, EventArgs.Empty); }
    }

    public string TotalSizeText => _displayRoot != null
        ? $"{Loc.ScanTotal} {FileSystemNode.FormatSize(_displayRoot.Size)}   |   {CountFiles(_displayRoot):N0} {Loc.ScanFileCount}"
        : string.Empty;

    public string FileCountText => _isReadingPhase
        ? $"{FileCount:N0} {Loc.ScanReadCount}"
        : $"{FileCount:N0} {Loc.ScanFileCount}";

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand OpenFolderCommand { get; }
    public ICommand ScanDriveCommand { get; }
    public ICommand NavigateIntoCommand { get; }
    public ICommand NavigateUpCommand { get; }
    public ICommand NavigateBackCommand { get; }
    public ICommand CancelScanCommand { get; }

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Wird ausgelöst, wenn das Layout neu berechnet und gerendert werden soll.</summary>
    public event EventHandler? LayoutRequested;

    // ── Ordner öffnen ─────────────────────────────────────────────────────────
    private void OpenFolder()
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = Loc.OpenFolderDesc,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            _ = ScanPathAsync(dlg.SelectedPath);
    }

    private void ScanDrive(string? letter)
    {
        if (!string.IsNullOrEmpty(letter))
            _ = ScanPathAsync(letter);
    }

    // ── Scan ─────────────────────────────────────────────────────────────────
    public async Task ScanPathAsync(string path)
    {
        if (_isScanning) CancelScan();

        int generation = Interlocked.Increment(ref _scanGeneration);

        _navBack.Clear();
        _rootNode = null;
        _displayRoot = null;
        IsScanning = true;
        IsIndeterminate = false;
        ScanProgress = 0;
        FileCount = 0;
        _isReadingPhase = true;
        OnPropertyChanged(nameof(FileCountText));
        ScanInfo = Loc.StatusReadingPhase;
        ScanRootPath = path;
        StatusText = $"{Loc.StatusScanningPrefix} {path} …";

        UpdateDriveInfoForPath(path);

        RaisedNavigateChanged();

        await EnsureScanOverlayVisibleAsync();

        _scanCts = new CancellationTokenSource();
        ScanEtaText = string.Empty;
        try
        {
            // NTFS-Schnellscan (Opt-in, braucht Adminrechte): nur für Laufwerkswurzeln, sonst/
            // bei jedem Fehler automatisch normaler Scanner (unverändertes Verhalten).
            // Token vorab in eine lokale Variable kopieren: _scanCts ist ein Feld und könnte
            // durch einen neu gestarteten Scan neu zugewiesen werden, bevor das Lambda unten
            // tatsächlich auf einem Threadpool-Thread ausgeführt wird.
            var fastScanToken = _scanCts.Token;
            FileSystemNode? rootNode = await Task.Run(() =>
                FastScanCoordinator.TryScan(path, fastScanToken, out var fastRoot) ? fastRoot : null,
                fastScanToken);

            if (rootNode == null)
            {
                var scanner = CreateInteractiveScanner(generation);
                rootNode = await scanner.ScanAsync(path, _scanCts.Token);
            }
            if (generation != _scanGeneration) return;

            ApplyScanResult(rootNode, path);
        }
        catch (OperationCanceledException)
        {
            if (generation == _scanGeneration)
                StatusText = Loc.StatusCancelled;
        }
        catch (Exception ex)
        {
            if (generation == _scanGeneration)
                StatusText = $"Fehler: {ex.Message}";
        }
        finally
        {
            if (generation == _scanGeneration)
            {
                _isReadingPhase = false;
                OnPropertyChanged(nameof(FileCountText));
                IsScanning = false;
                IsIndeterminate = false;
                ScanProgress = 0;
                _scanCts?.Dispose();
                _scanCts = null;
            }
        }
    }

    private void CancelScan()
    {
        _scanCts?.Cancel();
        _multiDriveCts?.Cancel();
    }

    public void CancelActiveScan()
    {
        if (_isScanning)
            CancelScan();
    }

    // ── Navigation ───────────────────────────────────────────────────────────
    public void NavigateInto(FileSystemNode? node)
    {
        if (node == null || !node.IsDirectory || node.Children.Count == 0) return;
        if (_displayRoot != null) _navBack.Push(_displayRoot);
        DisplayRoot = node;
        OnPropertyChanged(nameof(TotalSizeText));
    }

    public void NavigateUp()
    {
        if (_displayRoot?.Parent != null && _displayRoot != _rootNode)
        {
            if (_displayRoot != null) _navBack.Push(_displayRoot);
            DisplayRoot = _displayRoot!.Parent;
            OnPropertyChanged(nameof(TotalSizeText));
        }
    }

    public void NavigateBack()
    {
        if (_navBack.Count > 0)
        {
            DisplayRoot = _navBack.Pop();
            OnPropertyChanged(nameof(TotalSizeText));
        }
    }

    private void RaisedNavigateChanged()
    {
        ((RelayCommand)NavigateUpCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NavigateBackCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NavigateIntoCommand).RaiseCanExecuteChanged();
    }

    // ── Breadcrumb ────────────────────────────────────────────────────────────
    private void RefreshBreadcrumb()
    {
        Breadcrumb.Clear();
        if (_displayRoot == null) return;

        var chain = new Stack<FileSystemNode>();
        var n = _displayRoot;
        while (n != null) { chain.Push(n); n = n.Parent; }

        foreach (var node in chain)
            Breadcrumb.Add(new BreadcrumbItem(node.Name, node));
    }

    // ── Laufwerks-Info regenerieren (bei Sprachwechsel) ──────────────────────
    private void RegenerateDiskInfo()
    {
        ScanDriveInfo = $"{Loc.ScanDriveSizeLabel} {FileSystemNode.FormatSize(_diskTotalBytes)}   ·   {Loc.ScanDriveFreeLabel} {FileSystemNode.FormatSize(_diskFreeBytes)}";
        long used = _diskTotalBytes - _diskFreeBytes;
        DiskUsedPct = _diskTotalBytes > 0 ? (double)used / _diskTotalBytes * 100.0 : 0;
        DiskFreeText = $"{Loc.ScanDriveFreeLabel} {FileSystemNode.FormatSize(_diskFreeBytes)}  {Loc.ScanDiskFreeOf}  {FileSystemNode.FormatSize(_diskTotalBytes)}";
        DiskFreeBytes = _diskFreeBytes;
    }

    // ── Laufwerke laden ───────────────────────────────────────────────────────
    private void LoadDrives()
    {
        Drives.Clear();
        foreach (var d in DriveInfo.GetDrives())
        {
            try { if (d.IsReady) Drives.Add(d); }
            catch { /* gesperrte Laufwerke überspringen */ }
        }
    }

    // ── Stiller Scan (ohne DisplayRoot/RootNode zu ändern) ───────────────────
    /// <summary>Scannt einen Pfad und gibt den Baum zurück, ohne den aktuellen View zu ändern.</summary>
    public async Task<FileSystemNode?> SilentScanAsync(string path, CancellationToken ct)
    {
        try { return await new FileSystemScanner().ScanAsync(path, ct); }
        catch { return null; }
    }

    public async Task<CancellationToken> BeginMultiDriveScanAsync(IReadOnlyList<string> paths)
    {
        if (_isScanning) CancelScan();

        // Eigene CTS fuer Multi-Laufwerk-Scans: vorher lief das hier mit
        // CancellationToken.None, wodurch "Scan abbrechen" beim gleichzeitigen
        // Einlesen mehrerer Platten wirkungslos war (CancelScan() cancelte nur
        // _scanCts, das bei Multi-Drive-Scans nie gesetzt wurde) - der Scan lief
        // dann unsichtbar weiter, bis auch die langsamste Platte fertig war.
        _multiDriveCts?.Dispose();
        _multiDriveCts = new CancellationTokenSource();

        // Ohne dies blieb ein Breadcrumb aus einer vorherigen Einzellaufwerk-Ansicht (z. B.
        // "G:\ >") ueber der neuen Multi-Laufwerk-Ansicht stehen, obwohl er dort nicht mehr
        // zur Navigation gehoert -- verwirrend zusammen mit dem Bugreport zu "Kissen nicht
        // aktivierbar" (Klick auf ein Multi-Laufwerk-Kissen navigiert nicht wie im Breadcrumb
        // suggeriert, sondern wechselt in die Einzelansicht dieses Laufwerks).
        _navBack.Clear();
        Breadcrumb.Clear();

        Interlocked.Increment(ref _scanGeneration);
        IsScanning = true;
        IsIndeterminate = false;
        ScanProgress = 0;
        FileCount = 0;
        _isReadingPhase = true;
        OnPropertyChanged(nameof(FileCountText));
        ScanEtaText = string.Empty;
        ScanRootPath = paths.Count == 1 ? paths[0] : string.Join("  |  ", paths.Take(3)) + (paths.Count > 3 ? "  …" : string.Empty);
        ScanDriveInfo = paths.Count == 1 ? string.Empty : $"{paths.Count} {Loc.StatusScanningMulti}";
        ScanInfo = Loc.StatusReadingPhase;
        StatusText = paths.Count == 1
            ? $"{Loc.StatusScanningPrefix} {paths[0]} …"
            : $"{Loc.StatusScanningPrefix} {paths.Count} {Loc.StatusScanningMulti}";

        await EnsureScanOverlayVisibleAsync();

        return _multiDriveCts.Token;
    }

    public void UpdateMultiDriveScanProgress(int completed, int total, string currentPath, long scannedFiles)
    {
        FileCount = scannedFiles;
        _isReadingPhase = false;
        OnPropertyChanged(nameof(FileCountText));
        ScanInfo = currentPath;
        ScanProgress = total > 0 ? 100.0 * completed / total : 0;
        IsIndeterminate = false;
        ScanEtaText = string.Empty;
        StatusText = total <= 1
            ? $"{Loc.StatusScanningPrefix} {currentPath} …"
            : $"{Loc.StatusScanningPrefix} {completed}/{total} {Loc.StatusScanningMulti}";
    }

    public void CompleteMultiDriveScan(string statusText)
    {
        _isReadingPhase = false;
        OnPropertyChanged(nameof(FileCountText));
        StatusText = statusText;
        IsScanning = false;
        IsIndeterminate = false;
        ScanProgress = 0;
        ScanEtaText = string.Empty;
        _multiDriveCts?.Dispose();
        _multiDriveCts = null;
    }

    public void ApplyScanResult(FileSystemNode rootNode, string path)
    {
        _rootNode = rootNode;
        FileCount = CountFiles(rootNode);
        UpdateDriveInfoForPath(path);
        StatusText = $"{Loc.StatusDone} – {FileCount:N0} {Loc.ScanFileCount}, {FileSystemNode.FormatSize(rootNode.Size)}";
        DisplayRoot = rootNode;
        OnPropertyChanged(nameof(TotalSizeText));
    }

    // ── Layout-Anfrage (nach Resize) ─────────────────────────────────────────
    public void RequestLayout() => LayoutRequested?.Invoke(this, EventArgs.Empty);

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────
    private static long CountFiles(FileSystemNode node)
    {
        long count = 0;
        CountFilesRec(node, ref count);
        return count;
    }
    private static void CountFilesRec(FileSystemNode node, ref long count)
    {
        if (!node.IsDirectory) count++;
        else foreach (var c in node.Children) CountFilesRec(c, ref count);
    }

    private FileSystemScanner CreateInteractiveScanner(int generation)
    {
        var scanner = new FileSystemScanner();

        scanner.PhaseChanged += phase => Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (generation != _scanGeneration) return;

            if (phase == FileSystemScanner.ScanPhase.Reading)
            {
                _isReadingPhase = true;
                OnPropertyChanged(nameof(FileCountText));
                ScanInfo = Loc.StatusReadingPhase;
                return;
            }

            _isReadingPhase = false;
            OnPropertyChanged(nameof(FileCountText));
            IsIndeterminate = false;
            ScanProgress = Math.Max(ScanProgress, 2);
            ScanInfo = Loc.StatusProcessingPhase;
        }, DispatcherPriority.Background);

        scanner.ReadProgressChanged += progress => Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (generation != _scanGeneration) return;
            _isReadingPhase = true;
            FileCount = progress.EntriesRead;
            ScanProgress = progress.ProgressPercent;
            ScanEtaText = string.IsNullOrEmpty(progress.Eta) ? string.Empty : $"{Loc.StatusEtaPrefix} {progress.Eta}";
            OnPropertyChanged(nameof(FileCountText));
            ScanInfo = Loc.StatusReadingPhase;
        }, DispatcherPriority.Background);

        scanner.FilesScanned += n => Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (generation != _scanGeneration) return;
            FileCount = n;
        }, DispatcherPriority.Background);

        scanner.StatusChanged += s => Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (generation != _scanGeneration) return;
            ScanInfo = string.IsNullOrWhiteSpace(s) ? Loc.StatusProcessingPhase : s;
        }, DispatcherPriority.Background);

        scanner.ProgressChanged += (pct, eta) => Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (generation != _scanGeneration) return;
            _isReadingPhase = false;
            ScanProgress = pct;
            IsIndeterminate = false;
            ScanEtaText = string.IsNullOrEmpty(eta) ? string.Empty : $"{Loc.StatusEtaPrefix} {eta}";
            OnPropertyChanged(nameof(FileCountText));
        }, DispatcherPriority.Background);

        return scanner;
    }

    private void UpdateDriveInfoForPath(string path)
    {
        try
        {
            var drive = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(path) ?? path);
            if (drive.IsReady)
            {
                _diskTotalBytes = drive.TotalSize;
                _diskFreeBytes = drive.AvailableFreeSpace;
                RegenerateDiskInfo();
                return;
            }
        }
        catch { }

        ScanDriveInfo = string.Empty;
    }

    private async Task EnsureScanOverlayVisibleAsync()
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
        {
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
            await dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        }
        else
            await Task.Yield();
    }

    private void SetScanningState(bool value)
    {
        // [CallerMemberName] wuerde hier "SetScanningState" statt "IsScanning" einsetzen -
        // Name explizit angeben, sonst reagiert MainWindow's PropertyChanged-Handler nie
        // auf Scan-Ende (Abbruch/Fehler lassen ohne DisplayRoot-Aenderung das Scan-Overlay
        // fuer immer sichtbar, siehe Bugreport "Scan kommt nicht zu Ende").
        if (!Set(ref _isScanning, value, nameof(IsScanning))) return;

        ((RelayCommand)OpenFolderCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ScanDriveCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CancelScanCommand).RaiseCanExecuteChanged();
    }

}

// ── BreadcrumbItem ────────────────────────────────────────────────────────────
public sealed record BreadcrumbItem(string Label, FileSystemNode Node);
