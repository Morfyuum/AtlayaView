using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AtlayaView.Core;

namespace AtlayaView.Dialogs;

/// <summary>Modell für einen Eintrag in der Erweiterungsliste.</summary>
public sealed class ExtColorItem
{
    public string Extension { get; set; } = string.Empty;
    public SolidColorBrush Brush { get; set; } = new(Colors.Gray);
    public bool IsOverride { get; set; }
}

public partial class ColorSchemeDialog : Window
{
    // ── Zustand ───────────────────────────────────────────────────────────────
    private List<ExtColorItem> _allItems    = [];
    private ExtColorItem?      _current;
    private bool               _suppressUpdate;

    // Arbeitskopie der Overrides (nicht direkt in ColorScheme schreiben, erst bei OK)
    private readonly Dictionary<string, Color> _pending =
        new(StringComparer.OrdinalIgnoreCase);

    // Neu hinzugefügte Erweiterungen, die es noch nicht in ColorScheme gibt - erst bei OK
    // committet (ColorScheme.SetColor legt sie dann permanent an). Ohne diese Liste würde
    // LoadItems() sie nicht anzeigen, da ColorScheme.AllExtensions sie noch nicht kennt.
    private readonly HashSet<string> _pendingNewExtensions =
        new(StringComparer.OrdinalIgnoreCase);

    // Arbeitskopie der Datei-Öffner (Extension → EXE-Pfad)
    private readonly Dictionary<string, string> _pendingOpeners =
        new(StringComparer.OrdinalIgnoreCase);

    // Installierte Viewer (einmalig beim Öffnen ermittelt)
    private List<(string Name, string Path)> _installedViewers = [];

    // Schnellauswahl-Palette
    private static readonly Color[] Palette =
    [
        Color.FromRgb(220, 50, 50),   Color.FromRgb(200, 80, 30),
        Color.FromRgb(240,165, 30),   Color.FromRgb(240,210, 60),
        Color.FromRgb( 80,200, 80),   Color.FromRgb( 30,160, 80),
        Color.FromRgb( 50,200,180),   Color.FromRgb( 65,130,220),
        Color.FromRgb( 40, 80,200),   Color.FromRgb(140, 60,220),
        Color.FromRgb(200, 60,180),   Color.FromRgb(220,220,220),
        Color.FromRgb(160,160,160),   Color.FromRgb(100,100,130),
        Color.FromRgb( 50, 50, 70),   Color.FromRgb( 20, 20, 40),
    ];

    public ColorSchemeDialog()
    {
        InitializeComponent();
        AtlayaView.Core.WindowFrameFix.Apply(this);
        BuildPalette();
        // Bestehende Overrides als Ausgangszustand uebernehmen: BtnOk_Click ruft ColorScheme.ResetAll()
        // auf und spielt danach nur noch _pending zurueck -- ohne diese Vorbefuellung wuerden alle in
        // dieser Sitzung nicht angefassten, bereits gesetzten Farben (z. B. aus fruehreren Sitzungen
        // oder von einem zuvor angewendeten Farbprofil) beim naechsten OK stillschweigend verworfen.
        foreach (var kv in ColorScheme.Overrides)
            _pending[kv.Key] = kv.Value;
        _installedViewers = FileOpenerStore.FindInstalledViewers();
        // Aktuelle Opener-Zuordnungen als Ausgangszustand
        foreach (var kv in FileOpenerStore.Openers)
            _pendingOpeners[kv.Key] = kv.Value;
        LoadItems(string.Empty);
        PopulateOpenerCombo(null);
    }

    // ── Palette aufbauen ─────────────────────────────────────────────────────
    private void BuildPalette()
    {
        foreach (var c in Palette)
        {
            var btn = new Button
            {
                Width           = 24,
                Height          = 24,
                Margin          = new Thickness(2),
                Background      = new SolidColorBrush(c),
                BorderThickness = new Thickness(0),
                ToolTip         = $"#{c.R:X2}{c.G:X2}{c.B:X2}",
                Tag             = c,
                Style           = (Style)FindResource("ToolButtonStyle")
            };
            btn.Click += PaletteBtn_Click;
            palettePanel.Children.Add(btn);
        }
    }

    private void PaletteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Color c)
            ApplyColor(c);
    }

    // ── Liste füllen / filtern ───────────────────────────────────────────────
    private void LoadItems(string filter)
    {
        var effective = ColorScheme.EffectiveMap;
        var defaultColor = Color.FromRgb(100, 110, 130);
        bool profileActive = ColorScheme.ActiveProfileName != null;

        _allItems = ColorScheme.AllExtensions
            .Concat(_pendingNewExtensions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(ext => string.IsNullOrEmpty(filter) ||
                          ext.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
            .Select(ext =>
            {
                // Bei aktivem Farbprofil zeigt die Liste den echten Anzeige-Zustand: Profilfarbe
                // fuer Mitglieder, Silbergrau fuer alle uebrigen. Sonst Pending-Overrides zuerst.
                var color = profileActive ? ColorScheme.GetColor(ext)
                    : _pending.TryGetValue(ext, out var ov) ? ov
                    : effective.TryGetValue(ext, out var ec) ? ec
                    : defaultColor;
                return new ExtColorItem
                {
                    Extension  = ext,
                    Brush      = new SolidColorBrush(color),
                    IsOverride = profileActive
                        ? ColorScheme.HasOverride(ext)
                        : _pending.ContainsKey(ext) || ColorScheme.HasOverride(ext)
                };
            })
            .ToList();

        lstExtensions.ItemsSource = _allItems;
    }

    private void TxtSearch_Changed(object sender, TextChangedEventArgs e)
    {
        LoadItems(txtSearch.Text.Trim());
    }

    // ── Erweiterung hinzufügen ───────────────────────────────────────────────
    private void TxtNewExt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            AddNewExtension();
    }

    private void BtnAddExt_Click(object sender, RoutedEventArgs e) => AddNewExtension();

    private void AddNewExtension()
    {
        var raw = txtNewExt.Text.Trim();
        if (string.IsNullOrEmpty(raw))
            return;

        var ext = NormalizeExtension(raw);
        if (ext.Length < 2 || ext.Contains(' ') || ext.Any(c => "\\/:*?\"<>|".Contains(c)))
        {
            ShowAddExtStatus(App.Loc.ColorAddExtInvalid);
            return;
        }

        bool alreadyExists = ColorScheme.AllExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)
                           || _pendingNewExtensions.Contains(ext);
        if (alreadyExists)
        {
            ShowAddExtStatus(App.Loc.ColorAddExtDuplicate);
            SelectExtension(ext);
            return;
        }

        _pendingNewExtensions.Add(ext);
        _pending[ext] = Palette[4]; // sichtbare Startfarbe statt neutralem Grau
        txtNewExt.Text = string.Empty;
        HideAddExtStatus();
        LoadItems(txtSearch.Text.Trim());
        SelectExtension(ext);
    }

    private static string NormalizeExtension(string raw)
    {
        var ext = raw.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : "." + ext;
    }

    private void SelectExtension(string ext)
    {
        var match = _allItems.FirstOrDefault(i => string.Equals(i.Extension, ext, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            lstExtensions.SelectedItem = match;
    }

    private void ShowAddExtStatus(string text)
    {
        txtAddExtStatus.Text = text;
        txtAddExtStatus.Visibility = Visibility.Visible;
    }

    private void HideAddExtStatus() => txtAddExtStatus.Visibility = Visibility.Collapsed;

    // ── Farbprofile ──────────────────────────────────────────────────────────
    private void BtnColorProfiles_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ColorProfileDialog { Owner = this };
        // Feuert sowohl bei "Speichern" als auch bei "Auf Liste anwenden".
        dlg.ProfileApplied += ApplyProfileLive;
        dlg.ShowDialog();
    }

    /// <summary>
    /// Reagiert auf einen Profilwechsel im Farbprofil-Dialog (null = Startprofil). Die exklusive
    /// Anwendung ist zu diesem Zeitpunkt bereits in ColorScheme passiert
    /// (ApplyExclusiveProfile/ClearActiveProfile) -- hier wird nur noch die Erweiterungsliste
    /// aufgefrischt, der Treemap sofort neu gerendert und der Zustand persistiert, damit der
    /// Profilwechsel auch ein Schliessen ohne "OK" und den naechsten Programmstart ueberlebt.
    /// </summary>
    private void ApplyProfileLive(ColorProfile? profile)
    {
        // LoadItems() baut _allItems komplett neu auf (neue ExtColorItem-Instanzen) -- _current
        // zeigt danach noch auf das alte Objekt; ohne erneute Auswahl bliebe der Farbwaehler
        // rechts (Vorschau/RGB-Slider/Hex) auf dem alten Wert stehen.
        var reselectExt = _current?.Extension;

        HideAddExtStatus();
        LoadItems(txtSearch.Text.Trim());
        ShowAddExtStatus(profile == null
            ? App.Loc.ProfileDefaultAppliedStatus
            : string.Format(App.Loc.ProfileAppliedStatus, profile.Name, profile.ExtensionColors.Count));

        if (reselectExt != null)
            SelectExtension(reselectExt);

        if (Owner is MainWindow mw)
        {
            mw.RequestImmediateRerender();
            mw.PersistSettingsNow();
        }
    }

    // ── Auswahl-Änderung ─────────────────────────────────────────────────────
    private void LstExtensions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _current = lstExtensions.SelectedItem as ExtColorItem;
        if (_current == null) return;

        lblSelectedExt.Text = _current.Extension;
        btnResetOne.IsEnabled = true;

        // Aktuelle Farbe in Picker laden
        var c = _current.Brush.Color;
        LoadColorIntoControls(c);
        // Opener-Sektion aktualisieren
        PopulateOpenerCombo(_current.Extension);    }

    // ── Farbe in Steuerelemente laden ────────────────────────────────────────
    private void LoadColorIntoControls(Color c)
    {
        _suppressUpdate = true;
        slR.Value = c.R;
        slG.Value = c.G;
        slB.Value = c.B;
        _suppressUpdate = false;

        UpdatePreview();
        UpdateHexBox(c);
    }

    private void UpdatePreview()
    {
        if (_suppressUpdate) return;
        var c = GetCurrentColor();
        colorPreview.Background = new SolidColorBrush(c);
        lblR.Text = $"{(int)slR.Value}";
        lblG.Text = $"{(int)slG.Value}";
        lblB.Text = $"{(int)slB.Value}";
    }

    private Color GetCurrentColor()
        => Color.FromRgb((byte)slR.Value, (byte)slG.Value, (byte)slB.Value);

    private void UpdateHexBox(Color c)
    {
        _suppressUpdate = true;
        txtHex.Text = $"{c.R:X2}{c.G:X2}{c.B:X2}";
        _suppressUpdate = false;
    }

    // ── RGB Slider ───────────────────────────────────────────────────────────
    private void RgbSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressUpdate || _current == null) return;
        UpdatePreview();
        var c = GetCurrentColor();
        UpdateHexBox(c);
        ApplyColorToCurrent(c);
    }

    // ── Hex TextBox ──────────────────────────────────────────────────────────
    private void TxtHex_Changed(object sender, TextChangedEventArgs e)
    {
        if (_suppressUpdate || _current == null) return;
        if (txtHex.Text.Length != 6) return;

        try
        {
            byte r = Convert.ToByte(txtHex.Text[0..2], 16);
            byte g = Convert.ToByte(txtHex.Text[2..4], 16);
            byte b = Convert.ToByte(txtHex.Text[4..6], 16);
            var  c = Color.FromRgb(r, g, b);

            _suppressUpdate = true;
            slR.Value = r;
            slG.Value = g;
            slB.Value = b;
            _suppressUpdate = false;

            UpdatePreview();
            ApplyColorToCurrent(c);
        }
        catch { /* ungültige Hex-Eingabe ignorieren */ }
    }

    // ── Farbe auf aktuellen Eintrag anwenden ─────────────────────────────────
    private void ApplyColor(Color c)
    {
        if (_current == null) return;
        LoadColorIntoControls(c);
        ApplyColorToCurrent(c);
    }

    private void ApplyColorToCurrent(Color c)
    {
        if (_current == null) return;
        _pending[_current.Extension] = c;
        _current.Brush = new SolidColorBrush(c);
        _current.IsOverride = true;

        // Liste aktualisieren (RefreshItem)
        var idx = lstExtensions.SelectedIndex;
        LoadItems(txtSearch.Text.Trim());
        lstExtensions.SelectedIndex = idx;
    }

    // ── Reset ─────────────────────────────────────────────────────────────────
    private void BtnResetOne_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        _pending.Remove(_current.Extension);
        var defaultColor = ColorScheme.Map.TryGetValue(_current.Extension, out var dc)
            ? dc
            : Color.FromRgb(100, 110, 130);
        LoadColorIntoControls(defaultColor);
        ApplyColorToCurrent(defaultColor);
        _pending.Remove(_current.Extension); // Standard → kein Override
        LoadItems(txtSearch.Text.Trim());
    }

    private void BtnResetAll_Click(object sender, RoutedEventArgs e)
    {
        // Wie bei ApplyProfileLive: nicht nur die Zwischenablage leeren, sondern auch ColorScheme
        // selbst zuruecksetzen (inkl. eines aktiven Farbprofils) und sofort neu rendern -- sonst
        // blieb der Treemap auf den zuvor angewendeten Farben stehen, "Alle zuruecksetzen" wirkte
        // wie ein Reinfall.
        var reselectExt = _current?.Extension;

        _pending.Clear();
        ColorScheme.ResetAll();
        ColorScheme.ClearActiveProfile();
        LoadItems(txtSearch.Text.Trim());

        if (reselectExt != null)
            SelectExtension(reselectExt);
        else if (_current != null)
            LoadColorIntoControls(
                ColorScheme.Map.TryGetValue(_current.Extension, out var dc)
                    ? dc : Color.FromRgb(100, 110, 130));

        if (Owner is MainWindow mw)
        {
            mw.RequestImmediateRerender();
            mw.PersistSettingsNow();
        }
    }

    // ── Dialog-Buttons ────────────────────────────────────────────────────────
    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        // Pending-Farben in ColorScheme übertragen
        ColorScheme.ResetAll();
        foreach (var kv in _pending)
            ColorScheme.SetColor(kv.Key, kv.Value);
        // Pending-Opener in FileOpenerStore übertragen
        FileOpenerStore.Clear();
        foreach (var kv in _pendingOpeners)
            if (!string.IsNullOrEmpty(kv.Value))
                FileOpenerStore.SetOpener(kv.Key, kv.Value);
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    // ── Öffnen-mit-Sektion ────────────────────────────────────────────────
    private void PopulateOpenerCombo(string? extension)
    {
        bool suppressOld = _suppressUpdate;
        _suppressUpdate = true;
        try
        {
            cmbOpener.Items.Clear();
            cmbOpener.Items.Add(App.Loc.OpenerDefault); // Index 0 = Systemstandard

            foreach (var (name, path) in _installedViewers)
                cmbOpener.Items.Add(new OpenerEntry(name, path));

            if (extension == null)
            {
                cmbOpener.SelectedIndex = 0;
                txtOpenerPath.Text = string.Empty;
                cmbOpener.IsEnabled = false;
                btnBrowseOpener.IsEnabled = false;
                btnClearOpener.IsEnabled = false;
                return;
            }

            cmbOpener.IsEnabled = true;
            btnBrowseOpener.IsEnabled = true;
            btnClearOpener.IsEnabled = true;

            var current = _pendingOpeners.TryGetValue(extension, out var p) ? p : null;

            if (string.IsNullOrEmpty(current))
            {
                cmbOpener.SelectedIndex = 0;
                txtOpenerPath.Text = string.Empty;
            }
            else
            {
                // Bekanntes Programm?
                var match = _installedViewers.FindIndex(v =>
                    string.Equals(v.Path, current, StringComparison.OrdinalIgnoreCase));

                if (match >= 0)
                {
                    cmbOpener.SelectedIndex = match + 1;
                    txtOpenerPath.Text = current;
                }
                else
                {
                    // Benutzerdefiniertes Programm – als extra Eintrag hinzufügen
                    var custom = new OpenerEntry(System.IO.Path.GetFileNameWithoutExtension(current), current);
                    cmbOpener.Items.Add(custom);
                    cmbOpener.SelectedIndex = cmbOpener.Items.Count - 1;
                    txtOpenerPath.Text = current;
                }
            }
        }
        finally
        {
            _suppressUpdate = suppressOld;
        }
    }

    private void CmbOpener_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressUpdate || _current == null) return;

        if (cmbOpener.SelectedIndex == 0)
        {
            // Systemstandard
            _pendingOpeners.Remove(_current.Extension);
            txtOpenerPath.Text = string.Empty;
        }
        else if (cmbOpener.SelectedItem is OpenerEntry entry)
        {
            _pendingOpeners[_current.Extension] = entry.Path;
            txtOpenerPath.Text = entry.Path;
        }
    }

    private void BtnBrowseOpener_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;

        using var dlg = new System.Windows.Forms.OpenFileDialog
        {
            Title            = App.Loc.OpenerBrowseTip,
            Filter           = "Programme (*.exe)|*.exe|Alle Dateien (*.*)|*.*",
            CheckFileExists  = true,
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var path = dlg.FileName;
            _pendingOpeners[_current.Extension] = path;
            // Eintrag in ComboBox suchen oder neu hinzufügen
            var existing = _installedViewers.FindIndex(v =>
                string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing < 0)
                _installedViewers.Add((System.IO.Path.GetFileNameWithoutExtension(path), path));
            PopulateOpenerCombo(_current.Extension);
        }
    }

    private void BtnClearOpener_Click(object sender, RoutedEventArgs e)
    {
        if (_current == null) return;
        _pendingOpeners.Remove(_current.Extension);
        _suppressUpdate = true;
        cmbOpener.SelectedIndex = 0;
        txtOpenerPath.Text = string.Empty;
        _suppressUpdate = false;
    }

    /// <summary>ComboBox-Eintrag für ein Viewer-Programm.</summary>
    private sealed class OpenerEntry(string name, string path)
    {
        public string Name { get; } = name;
        public string Path { get; } = path;
        public override string ToString() => Name;
    }
}
