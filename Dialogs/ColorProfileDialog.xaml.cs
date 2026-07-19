using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AtlayaView.Core;

namespace AtlayaView.Dialogs;

/// <summary>Listeneintrag für ein Farbprofil (Name, Farbmuster, Erweiterungs-Anzahl).</summary>
public sealed class ProfileListItem
{
    public required ColorProfile Profile { get; init; }
    public SolidColorBrush Brush { get; init; } = new(Colors.Gray);
    public string CountText { get; init; } = string.Empty;
}

/// <summary>Listeneintrag für eine Erweiterung innerhalb des gerade bearbeiteten Profils.</summary>
public sealed class ProfileExtItem
{
    public string Extension { get; init; } = string.Empty;
    public string ColorHex { get; init; } = "#647882";
    public SolidColorBrush Brush { get; init; } = new(Colors.Gray);
}

public partial class ColorProfileDialog : Window
{
    private static readonly Color[] Palette =
    [
        Color.FromRgb(220, 50, 50),   Color.FromRgb(200, 80, 30),
        Color.FromRgb(240,165, 30),   Color.FromRgb(240,210, 60),
        Color.FromRgb( 80,200, 80),   Color.FromRgb( 30,160, 80),
        Color.FromRgb( 50,200,180),   Color.FromRgb( 65,130,220),
        Color.FromRgb( 40, 80,200),   Color.FromRgb(140, 60,220),
        Color.FromRgb(200, 60,180),   Color.FromRgb(220,220,220),
        Color.FromRgb(160,160,160),   Color.FromRgb(100,100,130),
    ];

    private List<ColorProfile> _profiles = [];
    private ColorProfile? _selected;
    private bool _suppressUpdate;

    // Arbeitskopie der Erweiterungen des gerade bearbeiteten Profils (Erweiterung → Hex-Farbe),
    // unabhängig vom gespeicherten Profil bis "Speichern" bzw. "Auf Liste anwenden" geklickt wird.
    private readonly Dictionary<string, string> _editingExtensions =
        new(StringComparer.OrdinalIgnoreCase);

    // Standardfarbe des Profils (Muster in der Profilliste + Startfarbe für neue Erweiterungen).
    private string _defaultColorHex = "#647882";

    // Wenn gesetzt: der Farbwähler bearbeitet die Farbe dieser Erweiterung statt der Profil-Standardfarbe.
    private string? _selectedRowExt;

    /// <summary>Wird gesetzt, wenn der Nutzer "Auf Liste anwenden" geklickt hat.</summary>
    public ColorProfile? AppliedProfile { get; private set; }

    public ColorProfileDialog()
    {
        InitializeComponent();
        AtlayaView.Core.WindowFrameFix.Apply(this);
        BuildPalette();
        _profiles = ColorProfileStore.Load();
        RefreshList(null);
        ClearEditor();
    }

    private void BuildPalette()
    {
        foreach (var c in Palette)
        {
            var btn = new Button
            {
                Width = 22, Height = 22, Margin = new Thickness(2),
                Background = new SolidColorBrush(c),
                BorderThickness = new Thickness(0),
                ToolTip = $"#{c.R:X2}{c.G:X2}{c.B:X2}",
                Tag = c,
                Style = (Style)FindResource("ToolButtonStyle")
            };
            btn.Click += (_, _) => { if (btn.Tag is Color pc) { SetColor(pc); ApplyColorToTarget(pc); } };
            palettePanel.Children.Add(btn);
        }
    }

    // ── Profilliste ──────────────────────────────────────────────────────────
    private void RefreshList(ColorProfile? selectAfter)
    {
        var items = _profiles.Select(p => new ProfileListItem
        {
            Profile = p,
            Brush = new SolidColorBrush(ParseHex(p.ColorHex)),
            CountText = string.Format(App.Loc.ProfileExtCountFmt, p.ExtensionColors.Count)
        }).ToList();

        lstProfiles.ItemsSource = items;

        if (selectAfter != null)
        {
            var match = items.FirstOrDefault(i => ReferenceEquals(i.Profile, selectAfter));
            lstProfiles.SelectedItem = match;
        }
    }

    private void LstProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstProfiles.SelectedItem is not ProfileListItem item)
        {
            _selected = null;
            btnDelete.IsEnabled = false;
            return;
        }

        _selected = item.Profile;
        btnDelete.IsEnabled = true;
        btnApply.IsEnabled = true;

        _suppressUpdate = true;
        txtName.Text = item.Profile.Name;
        _suppressUpdate = false;

        _editingExtensions.Clear();
        foreach (var kv in item.Profile.ExtensionColors)
            _editingExtensions[kv.Key] = kv.Value;

        _selectedRowExt = null;
        RefreshAddCombo();
        RefreshExtensionList(null);
        SetDefaultColor(ParseHex(item.Profile.ColorHex));
        HideStatus();
    }

    // ── Editor ───────────────────────────────────────────────────────────────
    private void ClearEditor()
    {
        _selected = null;
        lstProfiles.SelectedItem = null;
        _suppressUpdate = true;
        txtName.Text = string.Empty;
        _suppressUpdate = false;
        _editingExtensions.Clear();
        _selectedRowExt = null;
        RefreshAddCombo();
        RefreshExtensionList(null);
        SetDefaultColor(Palette[4]);
        btnDelete.IsEnabled = false;
        btnApply.IsEnabled = false;
        HideStatus();
        txtName.Focus();
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e) => ClearEditor();

    // ── Farbwähler ───────────────────────────────────────────────────────────
    private void SetColor(Color c)
    {
        colorPreview.Background = new SolidColorBrush(c);
        _suppressUpdate = true;
        txtHex.Text = $"{c.R:X2}{c.G:X2}{c.B:X2}";
        _suppressUpdate = false;
        btnApply.IsEnabled = true;
    }

    private void SetDefaultColor(Color c)
    {
        _defaultColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        lblColorTarget.Text = App.Loc.ProfileColorTargetDefault;
        SetColor(c);
    }

    private Color CurrentDefaultColor()
    {
        try { return ParseHex(_defaultColorHex); }
        catch { return Palette[4]; }
    }

    private void TxtHex_Changed(object sender, TextChangedEventArgs e)
    {
        if (_suppressUpdate || txtHex.Text.Length != 6) return;
        try
        {
            var c = ParseHex("#" + txtHex.Text);
            colorPreview.Background = new SolidColorBrush(c);
            ApplyColorToTarget(c);
        }
        catch { /* ungueltige Hex-Eingabe waehrend des Tippens ignorieren */ }
    }

    /// <summary>Überträgt die aktuelle Picker-Farbe auf die ausgewählte Zeile bzw. die Profil-Standardfarbe.</summary>
    private void ApplyColorToTarget(Color c)
    {
        var hex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        if (_selectedRowExt != null)
        {
            _editingExtensions[_selectedRowExt] = hex;
            RefreshExtensionList(_selectedRowExt);
        }
        else
        {
            _defaultColorHex = hex;
        }
        btnApply.IsEnabled = true;
    }

    // ── Erweiterungsliste des Profils ────────────────────────────────────────
    private void RefreshExtensionList(string? selectExt)
    {
        var items = _editingExtensions
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new ProfileExtItem
            {
                Extension = kv.Key,
                ColorHex  = kv.Value,
                Brush     = new SolidColorBrush(ParseHex(kv.Value))
            })
            .ToList();

        lstProfileExtensions.ItemsSource = items;

        if (selectExt != null)
        {
            var match = items.FirstOrDefault(i => string.Equals(i.Extension, selectExt, StringComparison.OrdinalIgnoreCase));
            lstProfileExtensions.SelectedItem = match;
        }
    }

    private void RefreshAddCombo()
    {
        var available = ColorScheme.AllExtensions
            .Where(ext => !_editingExtensions.ContainsKey(ext))
            .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
            .ToList();
        cmbAddExtension.ItemsSource = available;
        cmbAddExtension.Text = string.Empty;
    }

    private void CmbAddExtension_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            AddExtension();
    }

    private void BtnAddExtension_Click(object sender, RoutedEventArgs e) => AddExtension();

    private void AddExtension()
    {
        var raw = cmbAddExtension.Text;
        if (string.IsNullOrWhiteSpace(raw)) return;

        var ext = NormalizeExtension(raw);
        if (ext.Length < 2 || ext.Contains(' ') || ext.Any(c => "\\/:*?\"<>|".Contains(c)))
        {
            ShowStatus(App.Loc.ColorAddExtInvalid);
            return;
        }

        if (_editingExtensions.ContainsKey(ext))
        {
            ShowStatus(App.Loc.ColorAddExtDuplicate);
            RefreshExtensionList(ext);
            return;
        }

        // Vorhandene Erweiterung: ihre aktuelle Listenfarbe übernehmen (Startpunkt, änderbar).
        // Neue, unbekannte Erweiterung: aktuelle Standardfarbe des Profils übernehmen.
        var startColor = ColorScheme.EffectiveMap.TryGetValue(ext, out var existing)
            ? existing
            : CurrentDefaultColor();
        _editingExtensions[ext] = $"#{startColor.R:X2}{startColor.G:X2}{startColor.B:X2}";

        HideStatus();
        RefreshAddCombo();
        RefreshExtensionList(ext);
    }

    private void BtnRemoveExtension_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string ext }) return;

        _editingExtensions.Remove(ext);
        if (string.Equals(_selectedRowExt, ext, StringComparison.OrdinalIgnoreCase))
            _selectedRowExt = null;

        RefreshAddCombo();
        RefreshExtensionList(null);
        if (_selectedRowExt == null)
            SetDefaultColor(CurrentDefaultColor());
    }

    private void LstProfileExtensions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstProfileExtensions.SelectedItem is ProfileExtItem item)
        {
            _selectedRowExt = item.Extension;
            lblColorTarget.Text = string.Format(App.Loc.ProfileColorTargetExtFmt, item.Extension);
            SetColor(ParseHex(item.ColorHex));
        }
        else
        {
            _selectedRowExt = null;
            lblColorTarget.Text = App.Loc.ProfileColorTargetDefault;
        }
    }

    private static string NormalizeExtension(string raw)
    {
        var ext = raw.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : "." + ext;
    }

    // ── Speichern / Löschen / Anwenden ──────────────────────────────────────
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var name = txtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowStatus(App.Loc.ProfileNameRequired);
            return;
        }

        if (_editingExtensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileExtensionsRequired);
            return;
        }

        // Neue, dem globalen Farbschema noch unbekannte Erweiterungen dort ebenfalls anlegen,
        // damit sie künftig auch außerhalb dieses Profils in der Liste erscheinen.
        foreach (var kv in _editingExtensions)
        {
            if (!ColorScheme.AllExtensions.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                ColorScheme.SetColor(kv.Key, ParseHex(kv.Value));
        }

        if (_selected != null)
        {
            _selected.Name = name;
            _selected.ColorHex = _defaultColorHex;
            _selected.ExtensionColors = new Dictionary<string, string>(_editingExtensions, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _selected = new ColorProfile
            {
                Name = name,
                ColorHex = _defaultColorHex,
                ExtensionColors = new Dictionary<string, string>(_editingExtensions, StringComparer.OrdinalIgnoreCase)
            };
            _profiles.Add(_selected);
        }

        ColorProfileStore.Save(_profiles);
        RefreshList(_selected);
        HideStatus();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        var result = MessageBox.Show(
            string.Format(App.Loc.ProfileDeleteConfirm, _selected.Name),
            App.Loc.ProfileWindowTitle,
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        _profiles.Remove(_selected);
        ColorProfileStore.Save(_profiles);
        RefreshList(null);
        ClearEditor();
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_editingExtensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileExtensionsRequired);
            return;
        }

        var name = string.IsNullOrWhiteSpace(txtName.Text) ? "?" : txtName.Text.Trim();

        AppliedProfile = new ColorProfile
        {
            Name = name,
            ColorHex = _defaultColorHex,
            ExtensionColors = new Dictionary<string, string>(_editingExtensions, StringComparer.OrdinalIgnoreCase)
        };
        DialogResult = true;
    }

    // ── Status-Zeile ─────────────────────────────────────────────────────────
    private void ShowStatus(string text)
    {
        txtStatus.Text = text;
        txtStatus.Visibility = Visibility.Visible;
    }

    private void HideStatus() => txtStatus.Visibility = Visibility.Collapsed;

    private static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex[0..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        return Color.FromRgb(r, g, b);
    }
}
