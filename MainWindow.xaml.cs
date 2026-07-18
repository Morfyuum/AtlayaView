using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AtlayaView.Core;
using AtlayaView.ViewModels;

namespace AtlayaView;

public partial class MainWindow : Window
{
    // ── Felder ────────────────────────────────────────────────────────────────
    private readonly MainViewModel _vm;
    private readonly TreemapLayout _layout = new();
    private readonly CushionRenderer _renderer = new();
    private readonly Core.UpdateScheduler _updateScheduler;

    private static LocalizationManager Loc => LocalizationManager.Instance;

    private MenuItem? _drivesHeaderMenuItem;
    private ContextMenu? _drivePickerMenu;

    private FileSystemNode? _lastHit;
    private FileSystemNode? _freeSpaceNode;   // synthetischer Knoten für freien Speicher
    private Rectangle? _hoverRect;
    private Point _mousePos;

    // Debounce für Resize
    private System.Threading.Timer? _resizeTimer;
    private const int ResizeDebounceMs = 150;
    private readonly System.Windows.Threading.DispatcherTimer _driveSelectionTimer;
    private int _multiDriveScanGeneration;

    // ── Multi-Laufwerk ────────────────────────────────────────────────────────
    private readonly Dictionary<string, FileSystemNode?> _driveCache
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _selectedDrives = new();
    private readonly List<MenuItem> _driveMenuItems = new();
    private readonly List<UIElement> _driveLabels = new();
    private List<(string Path, FileSystemNode Node, Rect Region)> _multiDriveRegions = new();
    private bool IsMultiDriveMode => _selectedDrives.Count >= 2;

    // ── Konstruktor ───────────────────────────────────────────────────────────
    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        _driveSelectionTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        _driveSelectionTimer.Tick += DriveSelectionTimer_Tick;

        // Layout-Anfragen vom ViewModel – Background-Priorität: nach Layout-Pass ausführen
        _vm.LayoutRequested += (_, _) => Dispatcher.InvokeAsync(DoLayoutAndRender,
            System.Windows.Threading.DispatcherPriority.Background);

        // Wechsel in Einzellaufwerk-Modus beendet Multi-Drive-Ansicht
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.DisplayRoot)
                && _vm.DisplayRoot != null
                && IsMultiDriveMode)
                ExitMultiDriveMode();

            if (e.PropertyName == nameof(MainViewModel.IsScanning)
                || e.PropertyName == nameof(MainViewModel.DisplayRoot))
                UpdateScanVisualState();
        };

        // Legende aufbauen
        BuildLegend();

        // Sprache: Legende + Laufwerks-Header bei Sprachwechsel aktualisieren
        Loc.LanguageChanged += (_, _) =>
        {
            BuildLegend();
            RefreshLegendState();
            if (_drivesHeaderMenuItem != null)
                _drivesHeaderMenuItem.Header = Loc.CtxDrivesHeader;
            if (_selectedDrives.Count > 0)
                UpdateDriveSelectionStatus();
        };

        // Gespeicherte Einstellungen auf UI-Zustand übertragen
        var saved = App.Settings;
        _vm.ShowFreeSpaceCushion = saved.ShowFreeSpaceCushion;
        menuShowFreeSpaceCushion.IsChecked = saved.ShowFreeSpaceCushion;
        ApplyLanguageCheckmarks();

        // Kommandozeile: Pfad als erstes Argument
        var args = Environment.GetCommandLineArgs();
        bool startingFromArgs = args.Length > 1 && System.IO.Directory.Exists(args[1]);
        if (startingFromArgs)
            _ = StartSingleScanAsync(args[1]);
        else
            // Nur im Leerlauf-Fall aufrufen - sonst ueberschreibt dieser synchrone Aufruf
            // den Sichtbarkeits-Zustand, den ShowScanUiPending() im obigen Fire-and-forget-
            // Aufruf soeben gesetzt hat (der Scan haengt zu diesem Zeitpunkt noch am ersten
            // await, IsScanning ist hier also faelschlich noch false).
            UpdateScanVisualState();

        _updateScheduler = new Core.UpdateScheduler(HandleScheduledUpdateCheckAsync);
        _updateScheduler.Start();
    }

    // ── Automatische Update-Prüfung (nur wenn check_mode != manual) ─────────────
    private async Task HandleScheduledUpdateCheckAsync()
    {
        var info = await Core.UpdateChecker.FetchLatestAsync().ConfigureAwait(false);
        Core.UpdateScheduler.WriteLastCheck(DateTime.UtcNow);
        if (info is null || string.IsNullOrEmpty(info.Version)) return;
        if (!Core.UpdateChecker.IsNewer(info.Version, Core.LocalizationManager.CurrentVersion)) return;

        if (Core.UpdatePreferences.Instance.CheckMode == "auto_apply")
        {
            var url = Core.SelfUpdater.ResolveMatchingUrl(info);
            if (url != null)
            {
                string extractedDir = await Core.SelfUpdater.DownloadAndExtractAsync(url).ConfigureAwait(false);
                Dispatcher.Invoke(() => Core.SelfUpdater.LaunchSwapAndExit(extractedDir));
                return;
            }
        }

        // auto_check, oder auto_apply ohne zur Installation passende Variante: nur benachrichtigen
        Dispatcher.Invoke(() => new Dialogs.UpdateDialog { Owner = this }.ShowDialog());
    }

    // ── Beim Schließen speichern ──────────────────────────────────────────────
    protected override void OnClosed(EventArgs e)
    {
        _driveSelectionTimer.Stop();
        _updateScheduler.Dispose();
        base.OnClosed(e);
        SettingsStore.Save(_vm.ShowFreeSpaceCushion);
    }

    // ── Sprach-Checkmarks synchronisieren ────────────────────────────────────
    private void ApplyLanguageCheckmarks()
    {
        var lang = Loc.Language;
        miLangDe.IsChecked = lang == AppLanguage.Deutsch;
        miLangEn.IsChecked = lang == AppLanguage.English;
        miLangFr.IsChecked = lang == AppLanguage.Français;
        miLangIt.IsChecked = lang == AppLanguage.Italiano;
        miLangEs.IsChecked = lang == AppLanguage.Español;
    }

    private void ForceScanOverlayVisible()
    {
        if (FindName("scanOverlay") is not FrameworkElement overlay)
            return;

        overlay.UpdateLayout();
        UpdateLayout();
        Dispatcher.Invoke(() =>
        {
            overlay.UpdateLayout();
            UpdateLayout();
        }, System.Windows.Threading.DispatcherPriority.Render);
    }

    // Einzige Stelle, die ueber die Sichtbarkeit des Leer-Zustand-Panels entscheidet -
    // vorher setzten sechs verschiedene Stellen (Konstruktor, ShowScanUiPending,
    // DoMultiDriveLayoutAndRender, ExitMultiDriveMode, ...) diese Visibility unabhaengig
    // voneinander, was bei ungluecklicher Dispatcher-Reihenfolge dazu fuehren konnte, dass
    // das Panel nach einem fertigen Scan sichtbar blieb (Bugreport: "Programminfo bleibt
    // ueber dem fertigen Treemap stehen"). Jetzt: einmal zentral berechnen, ueberall
    // aufrufen statt lokal zu setzen.
    private void UpdateScanVisualState()
    {
        SetCancelButtonsVisibility(_vm.IsScanning ? Visibility.Visible : Visibility.Collapsed);
        scanOverlay.Visibility = _vm.IsScanning ? Visibility.Visible : Visibility.Collapsed;

        if (_vm.IsScanning)
        {
            emptyStatePanel.Visibility = Visibility.Collapsed;
            return;
        }

        bool hasContent = _vm.DisplayRoot != null
                        || (IsMultiDriveMode && _multiDriveRegions.Count > 0);
        emptyStatePanel.Visibility = hasContent ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ShowScanUiPending()
    {
        SetCancelButtonsVisibility(Visibility.Visible);
        emptyStatePanel.Visibility = Visibility.Collapsed;
        scanOverlay.Visibility = Visibility.Visible;
        ForceScanOverlayVisible();
    }

    private void SetCancelButtonsVisibility(Visibility visibility)
    {
        if (FindName("btnCancelScanToolbar") is UIElement toolbarButton)
            toolbarButton.Visibility = visibility;

        if (FindName("btnCancelScanOverlay") is UIElement overlayButton)
            overlayButton.Visibility = visibility;
    }

    private async Task StartSingleScanAsync(string path)
    {
        ShowScanUiPending();
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        try
        {
            await _vm.ScanPathAsync(path);
        }
        finally
        {
            UpdateScanVisualState();
        }
    }

    private async Task StartSelectedScanAsync()
    {
        ShowScanUiPending();
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        try
        {
            await RefreshMultiDriveAsync();
        }
        finally
        {
            UpdateScanVisualState();
        }
    }

    private void CancelScanButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.CancelActiveScan();
        SetCancelButtonsVisibility(Visibility.Collapsed);
    }

    // ── Layout + Rendering ────────────────────────────────────────────────────
    private async void DoLayoutAndRender()
    {
        if (IsMultiDriveMode)
        {
            await DoMultiDriveLayoutAndRender();
            return;
        }

        var root = _vm.DisplayRoot;
        if (root == null) { imgTreemap.Source = null; return; }

        double w = imgTreemap.ActualWidth;
        double h = imgTreemap.ActualHeight;

        // Fallback: Image noch nicht gemessen → Eltern-Container befragen
        if (w < 4 && imgTreemap.Parent is FrameworkElement parent)
            w = parent.ActualWidth;
        if (h < 4 && imgTreemap.Parent is FrameworkElement parent2)
            h = parent2.ActualHeight;
        if (w < 4 || h < 4) return;

        int pw = (int)w;
        int ph = (int)h;

        byte[] pixels;
        try
        {
            bool showFree = _vm.ShowFreeSpaceCushion
                         && _vm.DiskFreeBytes > 0
                         && root == _vm.RootNode;

            FileSystemNode? localFreeNode = null;
            if (showFree)
            {
                localFreeNode = new FileSystemNode
                {
                    Name = string.Format(Loc.FreeSpaceNodeFmt, FileSystemNode.FormatSize(_vm.DiskFreeBytes)),
                    FullPath = "(Freier Speicher)",
                    Extension = ".__free__",
                    Size = _vm.DiskFreeBytes,
                };
            }

            // Snapshot: vor dem Background-Thread lesen
            long freeBytes = _vm.DiskFreeBytes;

            pixels = await Task.Run(() =>
            {
                if (localFreeNode == null)
                {
                    // Normales Layout über den vollen Canvas
                    _layout.Layout(root, new Rect(0, 0, pw, ph));
                    if (root.Bounds.Width < 1)
                        throw new InvalidOperationException(
                            $"Layout fehlgeschlagen – Gesamtgröße: {root.Size} Bytes, " +
                            $"Kinder: {root.Children.Count}");
                    return _renderer.Render(root, pw, ph);
                }
                else
                {
                    // Freier Speicher als Kissen rechts:
                    // Root bekommt den proportionalen linken Teil, freeNode den Rest
                    long total = root.Size + freeBytes;
                    double ratio = total > 0 ? (double)root.Size / total : 1.0;
                    double dataW = Math.Max(4, Math.Round(pw * ratio));
                    double freeW = pw - dataW;

                    _layout.Layout(root, new Rect(0, 0, dataW, ph));
                    localFreeNode.Bounds = new Rect(dataW, 0, freeW, ph);

                    if (root.Bounds.Width < 1)
                        throw new InvalidOperationException(
                            $"Layout fehlgeschlagen – Gesamtgröße: {root.Size} Bytes, " +
                            $"Kinder: {root.Children.Count}");

                    return _renderer.RenderWithFreeNode(root, localFreeNode, pw, ph);
                }
            });

            _freeSpaceNode = localFreeNode;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Loc.MsgRenderError}\n{ex.Message}", "AtlayaView",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // BitmapSource.Create auf UI-Thread (nach await bereits im richtigen Kontext)
        int stride = pw * 4;
        var bmp = BitmapSource.Create(pw, ph, 96, 96,
                                      PixelFormats.Bgr32, null,
                                      pixels, stride);
        bmp.Freeze();
        imgTreemap.Source = bmp;
        ClearOverlay();
    }

    // ── Größenänderung ────────────────────────────────────────────────────────
    private void ImgTreemap_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Neu rendern sobald das Image-Control seine endgültige Größe kennt
        if (e.NewSize.Width > 4 && e.NewSize.Height > 4)
            DoLayoutAndRender();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);

        // Debounce: erst nach 150 ms Stillstand neu rendern
        _resizeTimer?.Dispose();
        _resizeTimer = new System.Threading.Timer(_ =>
        {
            Dispatcher.InvokeAsync(DoLayoutAndRender);
        }, null, ResizeDebounceMs, System.Threading.Timeout.Infinite);
    }

    // ── Maus: Hover ───────────────────────────────────────────────────────────
    private void TreemapGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (imgTreemap.Source == null) return;
        var pos = e.GetPosition(imgTreemap);

        FileSystemNode? node = null;

        if (_multiDriveRegions.Count > 0)
        {
            // Multi-Laufwerk: HitTest in der passenden Region
            foreach (var (_, driveNode, region) in _multiDriveRegions)
            {
                if (region.Contains(pos))
                {
                    node = CushionRenderer.HitTest(driveNode, pos.X, pos.Y);
                    break;
                }
            }
        }
        else if (_vm.DisplayRoot != null)
        {
            node = CushionRenderer.HitTest(_vm.DisplayRoot, pos.X, pos.Y);
            // Freier-Speicher-Knoten: liegt außerhalb von DisplayRoot.Bounds → separat prüfen
            if (node == null && _freeSpaceNode != null)
            {
                var b = _freeSpaceNode.Bounds;
                if (pos.X >= b.Left && pos.X <= b.Right && pos.Y >= b.Top && pos.Y <= b.Bottom)
                    node = _freeSpaceNode;
            }
        }

        if (node == _lastHit) return;
        _lastHit = node;
        _mousePos = pos;
        _vm.HoveredNode = node;

        UpdateOverlay(node);
    }

    private void TreemapGrid_MouseLeave(object sender, MouseEventArgs e)
    {
        _lastHit = null;
        _vm.HoveredNode = null;
        ClearOverlay();
    }

    // ── Maus: Klick ───────────────────────────────────────────────────────────
    private void TreemapGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (imgTreemap.Source == null) return;
        var pos = e.GetPosition(imgTreemap);

        // Multi-Laufwerk: Klick auf eine Region → Einzelansicht für dieses Laufwerk
        if (_multiDriveRegions.Count > 0)
        {
            foreach (var (drivePath, _, region) in _multiDriveRegions)
            {
                if (region.Contains(pos))
                {
                    ExitMultiDriveMode();
                    _ = StartSingleScanAsync(drivePath);
                    return;
                }
            }
            return;
        }

        if (_vm.DisplayRoot == null) return;
        var node = CushionRenderer.HitTest(_vm.DisplayRoot, pos.X, pos.Y);

        if (node == null) return;

        if (e.ClickCount == 2 && !node.IsDirectory)
        {
            // Doppelklick auf Datei → mit konfiguriertem Programm öffnen
            FileOpenerStore.OpenFile(node.FullPath);
            return;
        }

        if (e.ClickCount == 2 && node.IsDirectory)
        {
            // Doppelklick → in Ordner hineinnavigieren
            _vm.NavigateInto(node);
        }
        else if (node.IsDirectory)
        {
            // Einfacher Klick auf Ordner: in Ordner navigieren
            _vm.NavigateInto(node);
        }
    }

    private void TreemapGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm.DisplayRoot == null) return;

        var pos = e.GetPosition(imgTreemap);
        var node = CushionRenderer.HitTest(_vm.DisplayRoot, pos.X, pos.Y);
        if (node == null) return;

        // Kontextmenü
        var menu = new ContextMenu();

        if (!node.IsDirectory)
        {
            var openFile = new MenuItem { Header = Loc.CtxOpenFile };
            openFile.Click += (_, _) => FileOpenerStore.OpenFile(node.FullPath);
            menu.Items.Add(openFile);
            menu.Items.Add(new Separator { Style = (Style)FindResource("CtxSeparatorStyle") });
        }

        var openExplorer = new MenuItem { Header = Loc.CtxOpenExplorer };
        openExplorer.Click += (_, _) => OpenInExplorer(node);
        menu.Items.Add(openExplorer);

        var copyPath = new MenuItem { Header = Loc.CtxCopyPath };
        copyPath.Click += (_, _) => Clipboard.SetText(node.FullPath);
        menu.Items.Add(copyPath);

        if (node.IsDirectory && node.Children.Count > 0)
        {
            menu.Items.Add(new Separator { Style = (Style)FindResource("CtxSeparatorStyle") });
            var navInto = new MenuItem { Header = Loc.CtxNavigateInto };
            navInto.Click += (_, _) => _vm.NavigateInto(node);
            menu.Items.Add(navInto);
        }

        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    // ── Overlay (Hover-Highlight) ─────────────────────────────────────────────
    private void UpdateOverlay(FileSystemNode? node)
    {
        overlayCanvas.Children.Clear();
        if (node == null || imgTreemap.Source == null) return;

        // Highlight-Rechteck für den gefundenen Knoten
        var r = node.Bounds;
        var rect = new Rectangle
        {
            Width = r.Width,
            Height = r.Height,
            Stroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
            StrokeThickness = 2,
            Fill = new System.Windows.Media.SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rect, r.X);
        Canvas.SetTop(rect, r.Y);
        overlayCanvas.Children.Add(rect);
        _hoverRect = rect;

        // Tooltip-Popup nahe dem Cursor
        string name = node.Name;
        string size = AtlayaView.Core.FileSystemNode.FormatSize(node.Size);
        string type = node.IsDirectory ? Loc.TypeFolder : (string.IsNullOrEmpty(node.Extension) ? Loc.TypeFile : node.Extension.ToUpperInvariant());
        string date = node.LastModified > DateTime.MinValue
            ? node.LastModified.ToString("dd.MM.yyyy HH:mm") : string.Empty;

        var sp = new System.Windows.Controls.StackPanel();
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = name,
            FontWeight = FontWeights.SemiBold,
            Foreground = new System.Windows.Media.SolidColorBrush(Color.FromRgb(236, 240, 241)),
            FontSize = 11,
            MaxWidth = 260,
            TextTrimming = System.Windows.TextTrimming.CharacterEllipsis
        });
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"{size}  ·  {type}",
            Foreground = new System.Windows.Media.SolidColorBrush(Color.FromRgb(176, 190, 197)),
            FontSize = 10
        });
        if (!string.IsNullOrEmpty(date))
            sp.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"{Loc.HoverModified} {date}",
                Foreground = new System.Windows.Media.SolidColorBrush(Color.FromRgb(176, 190, 197)),
                FontSize = 10
            });

        var tip = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(Color.FromArgb(230, 10, 16, 35)),
            BorderBrush = new System.Windows.Media.SolidColorBrush(Color.FromRgb(80, 110, 160)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 5, 8, 5),
            IsHitTestVisible = false,
            Child = sp
        };
        overlayCanvas.Children.Add(tip);

        // Position: rechts/unten vom Cursor, am Rand spiegeln
        double tx = _mousePos.X + 14;
        double ty = _mousePos.Y + 14;
        double cw = overlayCanvas.ActualWidth;
        double ch = overlayCanvas.ActualHeight;
        if (tx + 200 > cw) tx = _mousePos.X - 210;
        if (ty + 65 > ch) ty = _mousePos.Y - 70;
        Canvas.SetLeft(tip, tx);
        Canvas.SetTop(tip, ty);
    }

    private void ClearOverlay()
    {
        // Nur Hover-Elemente entfernen – persistente Laufwerks-Labels behalten
        var toRemove = overlayCanvas.Children.Cast<UIElement>()
            .Where(c => !_driveLabels.Contains(c))
            .ToList();
        foreach (var el in toRemove)
            overlayCanvas.Children.Remove(el);
        _hoverRect = null;
    }

    // ── Breadcrumb-Klick ─────────────────────────────────────────────────────
    private void BreadcrumbBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FileSystemNode node)
            _vm.NavigateInto(node.IsDirectory ? node : node.Parent!);
    }

    // ── Sprache ───────────────────────────────────────────────────────────────
    private void MenuLang_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem clicked) return;
        Loc.Language = clicked.Tag switch
        {
            "1" => AppLanguage.English,
            "2" => AppLanguage.Français,
            "3" => AppLanguage.Italiano,
            "4" => AppLanguage.Español,
            _ => AppLanguage.Deutsch,
        };
        ApplyLanguageCheckmarks();
    }

    // ── Laufwerk-ComboBox ─────────────────────────────────────────────────────
    private void CmbDrives_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbDrives.SelectedItem is System.IO.DriveInfo drive && drive.IsReady)
        {
            ExitMultiDriveMode();
            _ = StartSingleScanAsync(drive.RootDirectory.FullName);
            cmbDrives.SelectedIndex = -1; // Reset, damit gleiches Laufwerk erneut gewählt werden kann
        }
    }

    // ── Explorer öffnen ──────────────────────────────────────────────────────
    private static void OpenInExplorer(FileSystemNode node)
    {
        try
        {
            string arg = node.IsDirectory
                ? $"\"{node.FullPath}\""
                : $"/select,\"{node.FullPath}\"";
            System.Diagnostics.Process.Start("explorer.exe", arg);
        }
        catch { /* Explorer-Fehler ignorieren */ }
    }

    // ── Legende aufbauen ─────────────────────────────────────────────────────
    // Aktiv-Filter: null = alle; befüllt = nur diese Kategorien farbig
    private readonly HashSet<string> _activeCategories = new();

    private static readonly List<(string Key, Color Color)> _legendCategories = new()
    {
        ("Bilder",       Color.FromRgb( 65, 130, 220)),
        ("Videos",       Color.FromRgb( 50, 200,  80)),
        ("Audio",        Color.FromRgb(255, 165,  30)),
        ("Dokumente",    Color.FromRgb(220,  60,  30)),
        ("Archive",      Color.FromRgb(155,  50, 220)),
        ("Ausführbar",   Color.FromRgb(220,  30,  50)),
        ("Quellcode",    Color.FromRgb( 80, 190, 210)),
        ("Datenbank",    Color.FromRgb( 70, 120, 180)),
        ("Schriften",    Color.FromRgb(180, 140,  90)),
        ("Sonstiges",    Color.FromRgb(100, 110, 130)),
    };

    private static string GetLocLabel(string key) => key switch
    {
        "Bilder" => Loc.LegImages,
        "Videos" => Loc.LegVideos,
        "Audio" => Loc.LegAudio,
        "Dokumente" => Loc.LegDocuments,
        "Archive" => Loc.LegArchives,
        "Ausführbar" => Loc.LegExecutables,
        "Quellcode" => Loc.LegSourceCode,
        "Datenbank" => Loc.LegDatabase,
        "Schriften" => Loc.LegFonts,
        _ => Loc.LegOther,
    };

    private void BuildLegend()
    {
        legendPanel.ItemsSource = _legendCategories.Select(c => new LegendItem
        {
            CategoryKey = c.Key,
            Label = GetLocLabel(c.Key),
            Color = new SolidColorBrush(c.Color),
            TextBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xBE, 0xC5)),
        }).ToList();
    }

    /// Legende optisch aktualisieren nach Filteränderung
    private void RefreshLegendState()
    {
        bool anyActive = _activeCategories.Count > 0;
        if (legendPanel.ItemsSource is not IEnumerable<LegendItem> items) return;
        foreach (var item in items)
        {
            bool active = !anyActive || _activeCategories.Contains(item.CategoryKey);
            item.Color.Opacity = active ? 1.0 : 0.25;
            item.TextBrush.Opacity = active ? 1.0 : 0.35;
        }
        // Renderer-Filter setzen
        _renderer.ActiveCategories = _activeCategories.Count > 0
            ? (IReadOnlySet<string>)_activeCategories
            : null;
    }

    private void LegendItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string label) return;

        bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        if (!ctrl)
        {
            // Einfachklick: nur diese Kategorie, oder zurücksetzen wenn schon exklusiv aktiv
            if (_activeCategories.Count == 1 && _activeCategories.Contains(label))
            {
                _activeCategories.Clear();
            }
            else
            {
                _activeCategories.Clear();
                _activeCategories.Add(label);
            }
        }
        else
        {
            // Strg+Klick: Toggle addieren/entfernen
            if (!_activeCategories.Add(label))
                _activeCategories.Remove(label);
        }

        RefreshLegendState();
        Dispatcher.InvokeAsync(DoLayoutAndRender);
    }

    private void MenuShowAll_Click(object sender, RoutedEventArgs e)
    {
        _activeCategories.Clear();
        RefreshLegendState();
        Dispatcher.InvokeAsync(DoLayoutAndRender);
    }

    // ── Mausrad: Ebene hoch/tiefer ───────────────────────────────────────────
    private void TreemapGrid_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            // Rad hoch → eine Ebene hoch
            _vm.NavigateUpCommand.Execute(null);
        }
        else
        {
            // Rad runter → frischen Hit-Test an aktueller Mausposition
            if (_vm.DisplayRoot == null || imgTreemap.Source == null) { e.Handled = true; return; }
            var pos = e.GetPosition(imgTreemap);
            var hit = CushionRenderer.HitTest(_vm.DisplayRoot, pos.X, pos.Y);
            // Datei → in den übergeordneten Ordner navigieren; Ordner → direkt rein
            var target = (hit?.IsDirectory == true) ? hit : hit?.Parent;
            if (target != null && target != _vm.DisplayRoot && target.Children.Count > 0)
                _vm.NavigateInto(target);
        }
        e.Handled = true;
    }

    // ── Keyboard ─────────────────────────────────────────────────────────────
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Back:
            case Key.BrowserBack:
                _vm.NavigateBackCommand.Execute(null);
                break;
            case Key.Up:
                _vm.NavigateUpCommand.Execute(null);
                break;
            case Key.Down:
                // Pfeil runter → in hovered Ordner hinein (falls vorhanden)
                if (_lastHit?.IsDirectory == true && _lastHit.Children.Count > 0)
                    _vm.NavigateInto(_lastHit);
                break;
            case Key.F5:
                MenuRefresh_Click(this, new RoutedEventArgs());
                break;
        }
    }

    // ── Menü-Handler ─────────────────────────────────────────────────────────
    private void MenuExit_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    private void MenuRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (IsMultiDriveMode || _selectedDrives.Count == 1)
        {
            _ = StartSelectedScanAsync();
            return;
        }
        if (_vm.DisplayRoot != null)
            _ = StartSingleScanAsync(_vm.DisplayRoot.FullPath);
    }

    private async Task RefreshMultiDriveAsync()
    {
        var paths = _selectedDrives.ToList();
        if (paths.Count == 0) return;

        int generation = Interlocked.Increment(ref _multiDriveScanGeneration);

        var ct = await _vm.BeginMultiDriveScanAsync(paths);

        var pending = paths.ToDictionary(
            path => path,
            path => _vm.SilentScanAsync(path, ct),
            StringComparer.OrdinalIgnoreCase);

        long scannedFiles = 0;
        int completed = 0;

        while (pending.Count > 0)
        {
            var finishedTask = await Task.WhenAny(pending.Values);
            if (generation != _multiDriveScanGeneration)
                return;

            var entry = pending.First(kvp => kvp.Value == finishedTask);
            pending.Remove(entry.Key);

            var result = await finishedTask;
            if (generation != _multiDriveScanGeneration)
                return;

            completed++;

            if (result != null)
            {
                _driveCache[entry.Key] = result;
                scannedFiles += CountFiles(result);
                await DoMultiDriveLayoutAndRender();
            }
            else
            {
                _driveCache.Remove(entry.Key);
            }

            _vm.UpdateMultiDriveScanProgress(completed, paths.Count, entry.Key, scannedFiles);
        }

        if (paths.Count == 1)
        {
            ClearDriveLabels();
            _multiDriveRegions.Clear();
            if (!ct.IsCancellationRequested && _driveCache.TryGetValue(paths[0], out var node) && node != null)
                _vm.ApplyScanResult(node, paths[0]);

            _vm.CompleteMultiDriveScan(ct.IsCancellationRequested ? Loc.StatusCancelled : _vm.StatusText);
            return;
        }

        _vm.CompleteMultiDriveScan(ct.IsCancellationRequested ? Loc.StatusCancelled : Loc.StatusDone);
        await DoMultiDriveLayoutAndRender();
    }

    private void MenuShowLegend_Click(object sender, RoutedEventArgs e)
    {
        bool show = menuShowLegend.IsChecked;
        legendBorder.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MenuShowDiskSpace_Click(object sender, RoutedEventArgs e)
    {
        bool show = menuShowDiskSpace.IsChecked;
        diskSpaceBorder.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MenuShowFreeSpaceCushion_Click(object sender, RoutedEventArgs e)
    {
        _vm.ShowFreeSpaceCushion = menuShowFreeSpaceCushion.IsChecked;
    }

    private void MenuOptions_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Dialogs.OptionsDialog(_vm.ShowFreeSpaceCushion) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _vm.ShowFreeSpaceCushion = dlg.ShowFreeSpaceCushionResult;
            menuShowFreeSpaceCushion.IsChecked = _vm.ShowFreeSpaceCushion;
            Dispatcher.InvokeAsync(DoLayoutAndRender);
        }
    }

    private void MenuColors_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Dialogs.ColorSchemeDialog { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            BuildLegend(); // Legende aktualisieren
            RefreshLegendState();
            Dispatcher.InvokeAsync(DoLayoutAndRender);
        }
    }

    private void MenuFilters_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Dialogs.FilterDialog { Owner = this };
        if (dlg.ShowDialog() == true)
            Dispatcher.InvokeAsync(DoLayoutAndRender);
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        new Dialogs.AboutDialog { Owner = this }.ShowDialog();
    }

    private const string ImprintUrl = "https://atlaya.capecter.com/impressum.html";
    private const string PrivacyUrl = "https://atlaya.capecter.com/datenschutz.html";

    private void MenuImprint_Click(object sender, RoutedEventArgs e) => OpenLegalUrl(ImprintUrl);

    private void MenuPrivacy_Click(object sender, RoutedEventArgs e) => OpenLegalUrl(PrivacyUrl);

    private static void OpenLegalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Kein Absturz, falls kein Standardbrowser verfügbar ist.
        }
    }

    private void MenuCheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        new Dialogs.UpdateDialog { Owner = this }.ShowDialog();
    }

    private void MenuSaveBitmap_Click(object sender, RoutedEventArgs e)
    {
        if (imgTreemap.Source is not BitmapSource bmp)
        {
            MessageBox.Show(Loc.MsgNoTreemap, "AtlayaView",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = Loc.SaveTitle,
            Filter = Loc.SaveFilter,
            DefaultExt = ".png",
            FileName = "AtlayaView-Treemap"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            BitmapEncoder encoder = System.IO.Path.GetExtension(dlg.FileName)
                .Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                    ? new BmpBitmapEncoder()
                    : new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = System.IO.File.Create(dlg.FileName);
            encoder.Save(fs);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{Loc.MsgSaveError}\n{ex.Message}", "AtlayaView",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    // ── Multi-Laufwerk: Initialisierung ──────────────────────────────────────
    private void Window_Loaded(object sender, RoutedEventArgs e)
        => PopulateDriveMenuItems();

    private void PopulateDriveMenuItems()
    {
        var ansicht = (MenuItem)mainMenu.Items[1];
        ansicht.Items.Add(new Separator());

        var header = new MenuItem
        {
            Header = Loc.CtxDrivesHeader,
            IsEnabled = false,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 140, 170)),
        };
        _drivesHeaderMenuItem = header;
        ansicht.Items.Add(header);
        _drivePickerMenu = new ContextMenu();

        var popupHeader = new MenuItem
        {
            Header = Loc.CtxDrivesHeader,
            IsEnabled = false,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 140, 170))
        };
        _drivePickerMenu.Items.Add(popupHeader);
        _drivePickerMenu.Items.Add(new Separator());

        foreach (var drive in _vm.Drives)
        {
            try
            {
                string label = string.IsNullOrEmpty(drive.VolumeLabel)
                    ? $"{drive.Name}  ({FileSystemNode.FormatSize(drive.TotalSize)})"
                    : $"{drive.Name}  {drive.VolumeLabel}  ({FileSystemNode.FormatSize(drive.TotalSize)})";

                var item = new MenuItem
                {
                    Header = label,
                    IsCheckable = true,
                    IsChecked = false,
                    StaysOpenOnClick = true,
                    Tag = drive.RootDirectory.FullName
                };
                item.Click += DriveMenuItem_Click;
                ansicht.Items.Add(item);
                _driveMenuItems.Add(item);

                var popupItem = new MenuItem
                {
                    Header = label,
                    IsCheckable = true,
                    IsChecked = false,
                    StaysOpenOnClick = true,
                    Tag = drive.RootDirectory.FullName
                };
                popupItem.Click += DriveMenuItem_Click;
                _drivePickerMenu.Items.Add(popupItem);
                _driveMenuItems.Add(popupItem);
            }
            catch { /* gesperrtes/nicht bereites Laufwerk überspringen */ }
        }
    }

    // ── Multi-Laufwerk: Menü-Klick (nur Auswahl pflegen, kein Scan) ──────────
    private void DriveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem item || item.Tag is not string drivePath) return;

        if (item.IsChecked)
        {
            if (!_selectedDrives.Contains(drivePath, StringComparer.OrdinalIgnoreCase))
                _selectedDrives.Add(drivePath);
        }
        else
        {
            _selectedDrives.RemoveAll(d => d.Equals(drivePath, StringComparison.OrdinalIgnoreCase));
            _driveCache.Remove(drivePath);
        }

        _vm.StatusText = _selectedDrives.Count switch
        {
            0 => Loc.StatusNoDrive,
            1 => $"{_selectedDrives[0]}  {Loc.StatusDriveSelected}",
            _ => $"{_selectedDrives.Count} {Loc.StatusDrivesSelected}",
        };

        SyncDriveMenuItems(drivePath, item.IsChecked);

        ScheduleDriveSelectionScan();
    }

    private void DrivePickerBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_drivePickerMenu == null)
            return;

        _drivePickerMenu.PlacementTarget = btnDrivePicker;
        _drivePickerMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        _drivePickerMenu.IsOpen = true;
    }

    private void UpdateDriveSelectionStatus()
    {
        _vm.StatusText = _selectedDrives.Count switch
        {
            0 => Loc.StatusNoDrive,
            1 => $"{_selectedDrives[0]}  {Loc.StatusDriveSelected}",
            _ => $"{_selectedDrives.Count} {Loc.StatusDrivesSelected}",
        };
    }

    // ── Multi-Laufwerk: ExitMode ──────────────────────────────────────────────
    private void ExitMultiDriveMode()
    {
        _driveSelectionTimer.Stop();
        _selectedDrives.Clear();
        _multiDriveRegions.Clear();
        ClearDriveLabels();
        foreach (var mi in _driveMenuItems)
            mi.IsChecked = false;
        // Disk-Space-Bar auf Einzelmodus zurücksetzen
        diskSpaceMulti.Children.Clear();
        diskSpaceMulti.ColumnDefinitions.Clear();
        diskSpaceSingle.Visibility = Visibility.Visible;
        diskSpaceMulti.Visibility = Visibility.Collapsed;

        UpdateScanVisualState();
    }

    private void ClearDriveLabels()
    {
        foreach (var lbl in _driveLabels)
            overlayCanvas.Children.Remove(lbl);
        _driveLabels.Clear();
    }

    private void SyncDriveMenuItems(string drivePath, bool isChecked)
    {
        foreach (var menuItem in _driveMenuItems)
        {
            if (menuItem.Tag is string tag && tag.Equals(drivePath, StringComparison.OrdinalIgnoreCase))
                menuItem.IsChecked = isChecked;
        }
    }

    private void ScheduleDriveSelectionScan()
    {
        if (_selectedDrives.Count == 0)
        {
            _driveSelectionTimer.Stop();
            return;
        }

        _driveSelectionTimer.Stop();
        _driveSelectionTimer.Start();
    }

    private void DriveSelectionTimer_Tick(object? sender, EventArgs e)
    {
        _driveSelectionTimer.Stop();

        if (_selectedDrives.Count == 0)
            return;

        if (_selectedDrives.Count == 1)
        {
            _driveCache.Remove(_selectedDrives[0]);
            _ = StartSingleScanAsync(_selectedDrives[0]);
            return;
        }

        _ = StartSelectedScanAsync();
    }

    private void UpdateMultiDiskSpaceBar(List<(string Path, FileSystemNode Node)> drives)
    {
        diskSpaceMulti.Children.Clear();
        diskSpaceMulti.ColumnDefinitions.Clear();

        var driveInfoMap = _vm.Drives.ToDictionary(
            d => d.RootDirectory.FullName,
            StringComparer.OrdinalIgnoreCase);

        var textBrush = (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
        var accentBrush = (System.Windows.Media.Brush)FindResource("AccentBrush");
        var bgBrush = (System.Windows.Media.Brush)FindResource("BgDeepBrush");

        for (int i = 0; i < drives.Count; i++)
        {
            driveInfoMap.TryGetValue(drives[i].Path, out var di);
            long total = di?.TotalSize ?? drives[i].Node.Size;
            long free = di?.AvailableFreeSpace ?? 0;
            double pct = total > 0 ? (double)(total - free) / total * 100.0 : 0;
            string name = drives[i].Path.TrimEnd(
                System.IO.Path.DirectorySeparatorChar,
                System.IO.Path.AltDirectorySeparatorChar);

            diskSpaceMulti.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(Math.Max(total, 1), GridUnitType.Star)
            });

            var cell = new Grid { Margin = new Thickness(i == 0 ? 4 : 8, 0, 4, 0) };
            cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            cell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cell.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = new System.Windows.Controls.TextBlock
            {
                Text = name,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = textBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var pb = new System.Windows.Controls.ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Value = pct,
                Foreground = accentBrush,
                Background = bgBrush,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var txt = new System.Windows.Controls.TextBlock
            {
                Text = $"{FileSystemNode.FormatSize(free)} {Loc.MultiDriveFreeOf} {FileSystemNode.FormatSize(total)}",
                FontSize = 10,
                Foreground = textBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(pb, 1);
            Grid.SetColumn(txt, 2);
            cell.Children.Add(lbl);
            cell.Children.Add(pb);
            cell.Children.Add(txt);
            Grid.SetColumn(cell, i);
            diskSpaceMulti.Children.Add(cell);
        }

        diskSpaceSingle.Visibility = Visibility.Collapsed;
        diskSpaceMulti.Visibility = Visibility.Visible;
    }

    // ── Multi-Laufwerk: Layout & Render ───────────────────────────────────────
    private async Task DoMultiDriveLayoutAndRender()
    {
        var activeDrives = _selectedDrives
            .Where(d => _driveCache.TryGetValue(d, out var n) && n != null)
            .Select(d => (Path: d, Node: _driveCache[d]!))
            .ToList();

        if (activeDrives.Count == 0) return;

        double w = imgTreemap.ActualWidth;
        double h = imgTreemap.ActualHeight;
        if (w < 4 && imgTreemap.Parent is FrameworkElement p1) w = p1.ActualWidth;
        if (h < 4 && imgTreemap.Parent is FrameworkElement p2) h = p2.ActualHeight;
        if (w < 4 || h < 4) return;

        int pw = (int)w, ph = (int)h;
        var regions = CalculateDriveRegions(activeDrives, pw, ph);

        // Laufwerks-Infos für Freier-Speicher-Kissen VOR dem Hintergrundthread ermitteln
        bool showFreeLocal = _vm.ShowFreeSpaceCushion;
        var driveInfoMap = _vm.Drives.ToDictionary(
            d => d.RootDirectory.FullName, StringComparer.OrdinalIgnoreCase);
        var freeBytesList = activeDrives.Select(d =>
        {
            driveInfoMap.TryGetValue(d.Path, out var di);
            return di?.AvailableFreeSpace ?? 0L;
        }).ToList();

        byte[] pixels = await Task.Run(() =>
        {
            int stride = pw * 4;
            var buf = new byte[stride * ph];
            // Hintergrund
            for (int i = 0; i < buf.Length; i += 4)
            { buf[i] = 0x2E; buf[i + 1] = 0x1A; buf[i + 2] = 0x1A; buf[i + 3] = 0xFF; }

            for (int i = 0; i < activeDrives.Count; i++)
            {
                var region = regions[i];
                var node = activeDrives[i].Node;
                long freeBytes = freeBytesList[i];

                if (showFreeLocal && freeBytes > 0)
                {
                    // Region aufteilen: links Daten, rechts Freier Speicher
                    long totalBytes = node.Size + freeBytes;
                    double ratio = totalBytes > 0 ? (double)node.Size / totalBytes : 1.0;
                    double dataW = Math.Max(4, Math.Round(region.Width * ratio));
                    double freeW = region.Width - dataW;

                    _layout.Layout(node, new Rect(region.X, region.Y, dataW, region.Height));
                    _renderer.RenderIntoBuffer(buf, pw, ph, node);

                    if (freeW >= 1)
                    {
                        var freeNode = new FileSystemNode
                        {
                            Name = string.Format(Loc.FreeSpaceNodeFmt, FileSystemNode.FormatSize(freeBytes)),
                            FullPath = "(Freier Speicher)",
                            Extension = ".__free__",
                            Size = freeBytes,
                            Bounds = new Rect(region.X + dataW, region.Y, freeW, region.Height)
                        };
                        _renderer.RenderIntoBuffer(buf, pw, ph, freeNode);
                    }
                }
                else
                {
                    _layout.Layout(node, region);
                    _renderer.RenderIntoBuffer(buf, pw, ph, node);
                }
            }
            DrawDriveSeparators(buf, stride, regions);
            return buf;
        });

        _freeSpaceNode = null;
        _multiDriveRegions = regions
            .Zip(activeDrives, (r, d) => (d.Path, d.Node, r))
            .ToList();

        var bmp = BitmapSource.Create(pw, ph, 96, 96, PixelFormats.Bgr32, null, pixels, pw * 4);
        bmp.Freeze();
        imgTreemap.Source = bmp;
        UpdateScanVisualState();
        ClearOverlay();
        DrawDriveLabels(regions, activeDrives);
        UpdateMultiDiskSpaceBar(activeDrives);
    }

    private static List<Rect> CalculateDriveRegions(
        List<(string Path, FileSystemNode Node)> drives, int width, int height)
    {
        int n = drives.Count;
        var result = new List<Rect>(n);
        if (n == 0) return result;
        if (n == 1) { result.Add(new Rect(0, 0, width, height)); return result; }

        if (n <= 4)
        {
            // Einzeilig – Breiten proportional zur belegten Größe
            long total = drives.Sum(d => d.Node.Size);
            double x = 0;
            for (int i = 0; i < n; i++)
            {
                double colW = total > 0
                    ? Math.Round((double)drives[i].Node.Size / total * width)
                    : Math.Round((double)width / n);
                if (i == n - 1) colW = width - x; // letzter bekommt Restpixel
                colW = Math.Max(4, colW);
                result.Add(new Rect(x, 0, colW, height));
                x += colW;
            }
            return result;
        }

        // n >= 5 → zwei Reihen, erste Reihe bekommt ceil(n/2) Laufwerke
        int row1Count = (int)Math.Ceiling(n / 2.0);
        int row2Count = n - row1Count;

        long row1Size = drives.Take(row1Count).Sum(d => d.Node.Size);
        long row2Size = drives.Skip(row1Count).Sum(d => d.Node.Size);
        long totalSize = row1Size + row2Size;

        double h1 = totalSize > 0
            ? Math.Round((double)row1Size / totalSize * height)
            : Math.Round(height * 0.5);
        h1 = Math.Max(4, Math.Min(h1, height - 4));
        double h2 = height - h1;

        // Reihe 1
        double x1 = 0;
        for (int i = 0; i < row1Count; i++)
        {
            double colW = row1Size > 0
                ? Math.Round((double)drives[i].Node.Size / row1Size * width)
                : Math.Round((double)width / row1Count);
            if (i == row1Count - 1) colW = width - x1;
            colW = Math.Max(4, colW);
            result.Add(new Rect(x1, 0, colW, h1));
            x1 += colW;
        }

        // Reihe 2
        double x2 = 0;
        for (int i = row1Count; i < n; i++)
        {
            int idx = i - row1Count;
            double colW = row2Size > 0
                ? Math.Round((double)drives[i].Node.Size / row2Size * width)
                : Math.Round((double)width / row2Count);
            if (idx == row2Count - 1) colW = width - x2;
            colW = Math.Max(4, colW);
            result.Add(new Rect(x2, h1, colW, h2));
            x2 += colW;
        }

        return result;
    }

    private static void DrawDriveSeparators(byte[] pixels, int stride, List<Rect> regions)
    {
        for (int i = 1; i < regions.Count; i++)
        {
            var r = regions[i];
            int x0 = (int)r.Left;
            if (x0 <= 0) continue;
            int y0 = (int)r.Top;
            int y1 = y0 + (int)r.Height;
            // 4 Pixel breiter Trenner in hellem Grau
            for (int dx = 0; dx < 4; dx++)
            {
                int x = x0 - 2 + dx;
                if (x < 0 || x * 4 >= stride) continue;
                for (int y = y0; y < y1; y++)
                {
                    int idx = y * stride + x * 4;
                    if (idx + 3 >= pixels.Length) break;
                    pixels[idx] = 90;
                    pixels[idx + 1] = 90;
                    pixels[idx + 2] = 90;
                    pixels[idx + 3] = 255;
                }
            }
        }
    }

    private void DrawDriveLabels(List<Rect> regions,
                                  List<(string Path, FileSystemNode Node)> drives)
    {
        ClearDriveLabels();
        for (int i = 0; i < drives.Count; i++)
        {
            var region = regions[i];
            var drivePath = drives[i].Path;
            var driveSize = drives[i].Node.Size;

            var di = _vm.Drives.FirstOrDefault(d =>
                string.Equals(d.RootDirectory.FullName, drivePath, StringComparison.OrdinalIgnoreCase));

            string label = string.IsNullOrEmpty(di?.VolumeLabel)
                ? $"{drivePath}  {FileSystemNode.FormatSize(driveSize)}"
                : $"{drivePath} {di.VolumeLabel}  {FileSystemNode.FormatSize(driveSize)}";

            var border = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromArgb(205, 10, 16, 35)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(180, 80, 110, 160)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                IsHitTestVisible = false,
                Child = new System.Windows.Controls.TextBlock
                {
                    Text = label,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 220, 235)),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                }
            };
            Canvas.SetLeft(border, region.Left + 4);
            Canvas.SetTop(border, region.Top + 4);
            overlayCanvas.Children.Add(border);
            _driveLabels.Add(border);
        }
    }

    private static long CountFiles(FileSystemNode node)
    {
        long count = 0;
        CountFilesRec(node, ref count);
        return count;
    }

    private static void CountFilesRec(FileSystemNode node, ref long count)
    {
        if (!node.IsDirectory)
        {
            count++;
            return;
        }

        foreach (var child in node.Children)
            CountFilesRec(child, ref count);
    }
}

// ── Legende-Datenmodell ───────────────────────────────────────────────────────
public sealed class LegendItem
{
    public string CategoryKey { get; set; } = string.Empty; // interner dt. Schlüssel
    public string Label { get; set; } = string.Empty; // lokalisierter Anzeigename
    public SolidColorBrush Color { get; set; } = Brushes.Gray;
    public SolidColorBrush TextBrush { get; set; } = Brushes.Gray;
}
