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

    /// <summary>Wird gesetzt, wenn der Nutzer "Auf Liste anwenden" geklickt hat.</summary>
    public ColorProfile? AppliedProfile { get; private set; }

    public ColorProfileDialog()
    {
        InitializeComponent();
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
            btn.Click += (_, _) => { if (btn.Tag is Color pc) SetColor(pc); };
            palettePanel.Children.Add(btn);
        }
    }

    // ── Liste ────────────────────────────────────────────────────────────────
    private void RefreshList(ColorProfile? selectAfter)
    {
        var items = _profiles.Select(p => new ProfileListItem
        {
            Profile = p,
            Brush = new SolidColorBrush(ParseHex(p.ColorHex)),
            CountText = string.Format(App.Loc.ProfileExtCountFmt, p.Extensions.Count)
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
        txtExtensions.Text = string.Join(", ", item.Profile.Extensions);
        _suppressUpdate = false;

        SetColor(ParseHex(item.Profile.ColorHex));
        HideStatus();
    }

    // ── Editor ───────────────────────────────────────────────────────────────
    private void ClearEditor()
    {
        _selected = null;
        lstProfiles.SelectedItem = null;
        _suppressUpdate = true;
        txtName.Text = string.Empty;
        txtExtensions.Text = string.Empty;
        _suppressUpdate = false;
        SetColor(Palette[4]);
        btnDelete.IsEnabled = false;
        btnApply.IsEnabled = false;
        HideStatus();
        txtName.Focus();
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e) => ClearEditor();

    private void SetColor(Color c)
    {
        colorPreview.Background = new SolidColorBrush(c);
        _suppressUpdate = true;
        txtHex.Text = $"{c.R:X2}{c.G:X2}{c.B:X2}";
        _suppressUpdate = false;
        btnApply.IsEnabled = true;
    }

    private void TxtHex_Changed(object sender, TextChangedEventArgs e)
    {
        if (_suppressUpdate || txtHex.Text.Length != 6) return;
        try
        {
            var c = ParseHex("#" + txtHex.Text);
            colorPreview.Background = new SolidColorBrush(c);
        }
        catch { /* ungueltige Hex-Eingabe waehrend des Tippens ignorieren */ }
    }

    private Color CurrentColor()
    {
        try { return ParseHex("#" + txtHex.Text); }
        catch { return Palette[4]; }
    }

    private static List<string> ParseExtensions(string raw)
        => raw.Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
              .Select(NormalizeExtension)
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList();

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

        var extensions = ParseExtensions(txtExtensions.Text);
        if (extensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileExtensionsRequired);
            return;
        }

        var color = CurrentColor();
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        if (_selected != null)
        {
            _selected.Name = name;
            _selected.Extensions = extensions;
            _selected.ColorHex = hex;
        }
        else
        {
            _selected = new ColorProfile { Name = name, Extensions = extensions, ColorHex = hex };
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
        var extensions = ParseExtensions(txtExtensions.Text);
        if (extensions.Count == 0)
        {
            ShowStatus(App.Loc.ProfileExtensionsRequired);
            return;
        }

        var color = CurrentColor();
        var name = string.IsNullOrWhiteSpace(txtName.Text) ? "?" : txtName.Text.Trim();

        AppliedProfile = new ColorProfile
        {
            Name = name,
            Extensions = extensions,
            ColorHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}"
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
