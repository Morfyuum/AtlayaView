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

    /// <summary>true = fest eingebautes „Startprofil“ (Standardfarben, nicht lösch-/editierbar).</summary>
    public bool IsDefault { get; init; }
}

/// <summary>Listeneintrag für eine auswählbare (ankreuzbare) Erweiterung im Profil-Editor.</summary>
public sealed class ProfileExtItem
{
    public string Extension { get; init; } = string.Empty;
    public string ColorHex { get; init; } = "#647882";
    public bool IsChecked { get; init; }
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

    // Angehakte Erweiterungen = Mitglieder des gerade bearbeiteten Profils.
    private readonly HashSet<string> _checkedExtensions = new(StringComparer.OrdinalIgnoreCase);

    // Farben, die in dieser Editier-Session für eine Erweiterung gesetzt wurden - bleibt auch
    // erhalten, wenn die Erweiterung zwischenzeitlich abgehakt wird, damit ein erneutes Ankreuzen
    // die Farbe nicht verliert (nur _checkedExtensions bestimmt, was am Ende gespeichert wird).
    private readonly Dictionary<string, string> _editingExtensions = new(StringComparer.OrdinalIgnoreCase);

    // Musterfarbe für die Profilliste (nicht pro Erweiterung, sondern für das Profil als Ganzes).
    private string _profileColorHex = "#647882";

    /// <summary>
    /// Feuert, sobald sich das aktive Farbprofil geändert hat (Auswahl in der Liste, Speichern,
    /// "Auf Liste anwenden" oder Rückkehr zum Startprofil). Die exklusive Anwendung selbst passiert
    /// bereits über ColorScheme.ApplyExclusiveProfile/ClearActiveProfile -- der Empfänger
    /// (ColorSchemeDialog) muss nur noch seine Anzeige auffrischen, neu rendern und persistieren.
    /// null = Startprofil (Standardfarben) wurde gewählt.
    /// </summary>
    public event Action<ColorProfile?>? ProfileApplied;

    // Unterdrückt das Anwenden während der Vorselektion beim Öffnen des Dialogs (das dort
    // selektierte Profil IST bereits aktiv -- erneutes Anwenden + Speichern wäre nur Lärm).
    private bool _initializing;

    public ColorProfileDialog()
    {
        InitializeComponent();
        AtlayaView.Core.WindowFrameFix.Apply(this);
        BuildPalette();
        _profiles = ColorProfileStore.Load();

        // Beim Öffnen das gerade aktive Profil vorselektieren (bzw. das Startprofil).
        _initializing = true;
        var active = ColorScheme.ActiveProfileName == null ? null
            : _profiles.FirstOrDefault(p => p.Name.Equals(ColorScheme.ActiveProfileName, StringComparison.OrdinalIgnoreCase));
        if (active != null)
        {
            RefreshList(active);
        }
        else
        {
            RefreshList(null, selectDefault: true);
            ClearEditorFields();
            btnDelete.IsEnabled = false;
            btnApply.IsEnabled = false;
        }
        _initializing = false;
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
            btn.Click += (_, _) => { if (btn.Tag is Color pc) { SetColor(pc); ApplyColorToChecked(pc); } };
            palettePanel.Children.Add(btn);
        }
    }

    // ── Profilliste ──────────────────────────────────────────────────────────
    private void RefreshList(ColorProfile? selectAfter, bool selectDefault = false)
    {
        // Position 0 ist immer das fest eingebaute Startprofil (= Standardfarben, kein
        // exklusives Profil aktiv) -- nicht lösch- oder editierbar, dient nur als Umschalter.
        var items = new List<ProfileListItem>
        {
            new()
            {
                Profile = new ColorProfile { Name = App.Loc.ProfileDefaultName },
                Brush = new SolidColorBrush(Color.FromRgb(150, 150, 158)),
                CountText = App.Loc.ProfileDefaultCount,
                IsDefault = true
            }
        };
        items.AddRange(_profiles.Select(p => new ProfileListItem
        {
            Profile = p,
            Brush = new SolidColorBrush(ParseHex(p.ColorHex)),
            CountText = string.Format(App.Loc.ProfileExtCountFmt, p.ExtensionColors.Count)
        }));

        lstProfiles.ItemsSource = items;

        if (selectDefault)
            lstProfiles.SelectedItem = items[0];
        else if (selectAfter != null)
            lstProfiles.SelectedItem = items.FirstOrDefault(i => ReferenceEquals(i.Profile, selectAfter));
    }

    private void LstProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lstProfiles.SelectedItem is not ProfileListItem item)
        {
            _selected = null;
            btnDelete.IsEnabled = false;
            return;
        }

        // Startprofil gewählt: exklusives Profil beenden, Standardfarben gelten wieder.
        if (item.IsDefault)
        {
            _selected = null;
            btnDelete.IsEnabled = false;
            btnApply.IsEnabled = false;
            ClearEditorFields();
            if (!_initializing)
            {
                ColorScheme.ClearActiveProfile();
                ProfileApplied?.Invoke(null);
                ShowStatus(App.Loc.ProfileDefaultAppliedStatus);
            }
            return;
        }

        _selected = item.Profile;
        btnDelete.IsEnabled = true;
        btnApply.IsEnabled = true;

        _suppressUpdate = true;
        txtName.Text = item.Profile.Name;
        _suppressUpdate = false;

        _checkedExtensions.Clear();
        _editingExtensions.Clear();
        foreach (var kv in item.Profile.ExtensionColors)
        {
            _checkedExtensions.Add(kv.Key);
            _editingExtensions[kv.Key] = kv.Value;
        }
        _profileColorHex = item.Profile.ColorHex;

        RefreshExtensionList();
        SetColor(ParseHex(_profileColorHex));

        // Umschalten = anwenden: Schon die Auswahl eines Profils in der Liste aktiviert es
        // EXKLUSIV -- seine Erweiterungen bekommen die Profilfarben, alle uebrigen Silbergrau.
        // Kein zusaetzlicher "Speichern"/"Auf Liste anwenden"-Klick noetig; die Profilliste ist
        // die alleinige Umschaltung (Startprofil an Position 0 = zurueck zu Standardfarben).
        if (!_initializing)
        {
            ColorScheme.ApplyExclusiveProfile(item.Profile.Name, item.Profile.ExtensionColors);
            ProfileApplied?.Invoke(item.Profile);
            ShowStatus(string.Format(App.Loc.ProfileAppliedStatus, item.Profile.Name, item.Profile.ExtensionColors.Count));
        }
    }

    // ── Editor ───────────────────────────────────────────────────────────────
    /// <summary>Leert nur die Editor-Felder, ohne die Listen-Selektion anzufassen.</summary>
    private void ClearEditorFields()
    {
        _suppressUpdate = true;
        txtName.Text = string.Empty;
        _suppressUpdate = false;
        _checkedExtensions.Clear();
        _editingExtensions.Clear();
        _profileColorHex = $"#{Palette[4].R:X2}{Palette[4].G:X2}{Palette[4].B:X2}";
        RefreshExtensionList();
        SetColor(Palette[4]);
    }

    private void ClearEditor()
    {
        _selected = null;
        lstProfiles.SelectedItem = null;
        ClearEditorFields();
        btnDelete.IsEnabled = false;
        btnApply.IsEnabled = false;
        HideStatus();
        txtName.Focus();
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e) => ClearEditor();

    // ── Erweiterungs-Auswahlliste ────────────────────────────────────────────
    private void RefreshExtensionList()
    {
        var filter = txtExtSearch.Text.Trim();
        var all = ColorScheme.AllExtensions
            .Concat(_checkedExtensions)
            .Concat(_editingExtensions.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(ext => string.IsNullOrEmpty(filter) || ext.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
            .Select(ext =>
            {
                var hex = _editingExtensions.TryGetValue(ext, out var h) ? h : HexOf(ColorScheme.GetBaseColor(ext));
                return new ProfileExtItem
                {
                    Extension = ext,
                    ColorHex = hex,
                    IsChecked = _checkedExtensions.Contains(ext),
                    Brush = new SolidColorBrush(ParseHex(hex))
                };
            })
            .ToList();

        lstProfileExtensions.ItemsSource = all;
        UpdateApplyEnabled();
    }

    private void TxtExtSearch_Changed(object sender, TextChangedEventArgs e) => RefreshExtensionList();

    private void ExtCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: string ext } cb) return;

        if (cb.IsChecked == true)
        {
            _checkedExtensions.Add(ext);
            // Erstmalig angehakt (nie zuvor in dieser Session eine eigene Farbe bekommen):
            // Startfarbe aus der Grundliste übernehmen.
            if (!_editingExtensions.ContainsKey(ext))
                _editingExtensions[ext] = HexOf(ColorScheme.GetBaseColor(ext));
        }
        else
        {
            _checkedExtensions.Remove(ext);
        }

        UpdateApplyEnabled();
        HideStatus();
    }

    private void UpdateApplyEnabled() => btnApply.IsEnabled = _checkedExtensions.Count > 0;

    private void TxtNewExtension_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            AddNewExtension();
    }

    private void BtnAddExtension_Click(object sender, RoutedEventArgs e) => AddNewExtension();

    private void AddNewExtension()
    {
        var raw = txtNewExtension.Text;
        if (string.IsNullOrWhiteSpace(raw)) return;

        var ext = NormalizeExtension(raw);
        if (ext.Length < 2 || ext.Contains(' ') || ext.Any(c => "\\/:*?\"<>|".Contains(c)))
        {
            ShowStatus(App.Loc.ColorAddExtInvalid);
            return;
        }

        bool alreadyKnown = ColorScheme.AllExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)
                          || _editingExtensions.ContainsKey(ext);
        if (!_editingExtensions.ContainsKey(ext))
            _editingExtensions[ext] = alreadyKnown ? HexOf(ColorScheme.GetBaseColor(ext)) : _profileColorHex;
        _checkedExtensions.Add(ext);

        txtNewExtension.Text = string.Empty;
        HideStatus();
        RefreshExtensionList();
    }

    private static string NormalizeExtension(string raw)
    {
        var ext = raw.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : "." + ext;
    }

    private static string HexOf(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // ── Farbwähler ───────────────────────────────────────────────────────────
    private void SetColor(Color c)
    {
        colorPreview.Background = new SolidColorBrush(c);
        _suppressUpdate = true;
        txtHex.Text = $"{c.R:X2}{c.G:X2}{c.B:X2}";
        _suppressUpdate = false;
    }

    private void TxtHex_Changed(object sender, TextChangedEventArgs e)
    {
        if (_suppressUpdate || txtHex.Text.Length != 6) return;
        try
        {
            var c = ParseHex("#" + txtHex.Text);
            colorPreview.Background = new SolidColorBrush(c);
            ApplyColorToChecked(c);
        }
        catch { /* ungueltige Hex-Eingabe waehrend des Tippens ignorieren */ }
    }

    /// <summary>Weist die übergebene Farbe allen aktuell angehakten Erweiterungen zu.</summary>
    private void ApplyColorToChecked(Color c)
    {
        var hex = HexOf(c);
        _profileColorHex = hex;

        if (_checkedExtensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileNoneCheckedStatus);
            return;
        }

        foreach (var ext in _checkedExtensions)
            _editingExtensions[ext] = hex;

        HideStatus();
        RefreshExtensionList();
    }

    /// <summary>Setzt für alle angehakten Erweiterungen ihre Farbe aus der Grundliste zurück.</summary>
    private void BtnUseListColor_Click(object sender, RoutedEventArgs e)
    {
        if (_checkedExtensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileNoneCheckedStatus);
            return;
        }

        foreach (var ext in _checkedExtensions)
            _editingExtensions[ext] = HexOf(ColorScheme.GetBaseColor(ext));

        HideStatus();
        RefreshExtensionList();
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

        if (_checkedExtensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileExtensionsRequired);
            return;
        }

        var extensionColors = _checkedExtensions.ToDictionary(
            ext => ext, ext => _editingExtensions[ext], StringComparer.OrdinalIgnoreCase);

        if (_selected != null)
        {
            _selected.Name = name;
            _selected.ColorHex = _profileColorHex;
            _selected.ExtensionColors = extensionColors;
        }
        else
        {
            _selected = new ColorProfile { Name = name, ColorHex = _profileColorHex, ExtensionColors = extensionColors };
            _profiles.Add(_selected);
        }

        ColorProfileStore.Save(_profiles);
        HideStatus();
        // RefreshList selektiert das gespeicherte Profil -- LstProfiles_SelectionChanged wendet es
        // dann exklusiv an, feuert ProfileApplied und zeigt den Status (kein doppeltes Invoke hier).
        RefreshList(_selected);
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_selected == null) return;

        var result = MessageBox.Show(
            string.Format(App.Loc.ProfileDeleteConfirm, _selected.Name),
            App.Loc.ProfileWindowTitle,
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        bool wasActive = ColorScheme.ActiveProfileName != null
            && ColorScheme.ActiveProfileName.Equals(_selected.Name, StringComparison.OrdinalIgnoreCase);

        _profiles.Remove(_selected);
        ColorProfileStore.Save(_profiles);

        if (wasActive)
        {
            // Das gerade aktive Profil wurde geloescht -> zurueck zum Startprofil; die Selektion
            // des Startprofil-Eintrags erledigt Anwenden/Invoke/Status ueber SelectionChanged.
            RefreshList(null, selectDefault: true);
        }
        else
        {
            RefreshList(null);
            ClearEditor();
        }
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_checkedExtensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileExtensionsRequired);
            return;
        }

        var name = string.IsNullOrWhiteSpace(txtName.Text) ? "?" : txtName.Text.Trim();
        var extensionColors = _checkedExtensions.ToDictionary(
            ext => ext, ext => _editingExtensions[ext], StringComparer.OrdinalIgnoreCase);

        var profile = new ColorProfile { Name = name, ColorHex = _profileColorHex, ExtensionColors = extensionColors };
        ColorScheme.ApplyExclusiveProfile(profile.Name, profile.ExtensionColors);
        ProfileApplied?.Invoke(profile);
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
