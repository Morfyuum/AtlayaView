using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AtlayaView.Core;

namespace AtlayaView.Dialogs;

public partial class FilterDialog : Window
{
    // Lokale Arbeitskopie
    private readonly HashSet<string> _excluded = new(StringComparer.OrdinalIgnoreCase);

    // Zuordnung CheckBox → Erweiterung
    private Dictionary<CheckBox, string> _quickChecks = [];

    public FilterDialog()
    {
        InitializeComponent();
        WindowFrameFix.Apply(this);

        // Schnellauswahl-Checkboxen registrieren
        _quickChecks = new Dictionary<CheckBox, string>
        {
            { chkTmp,     ".tmp"     },
            { chkLog,     ".log"     },
            { chkPdb,     ".pdb"     },
            { chkCache,   ".cache"   },
            { chkBak,     ".bak"     },
            { chkLnk,     ".lnk"     },
            { chkIni,     ".ini"     },
            { chkSys,     ".sys"     },
            { chkThumb,   ".db"      },
            { chkPyc,     ".pyc"     },
            { chkDsStore, ".ds_store"},
        };

        LoadCurrentFilter();
    }

    // ── Aktuellen Filter laden ────────────────────────────────────────────────
    private void LoadCurrentFilter()
    {
        var f = AppFilter.Instance;

        // Größe
        long sz = f.MinFileSizeBytes;
        if (sz == 0)
        {
            txtMinSize.Text = "0";
            cmbSizeUnit.SelectedIndex = 1; // KB
        }
        else if (sz >= 1L << 30) { txtMinSize.Text = $"{sz >> 30}"; cmbSizeUnit.SelectedIndex = 3; }
        else if (sz >= 1L << 20) { txtMinSize.Text = $"{sz >> 20}"; cmbSizeUnit.SelectedIndex = 2; }
        else if (sz >= 1L << 10) { txtMinSize.Text = $"{sz >> 10}"; cmbSizeUnit.SelectedIndex = 1; }
        else { txtMinSize.Text = $"{sz}"; cmbSizeUnit.SelectedIndex = 0; }

        // Ausgeschlossene Erweiterungen
        _excluded.Clear();
        foreach (var ext in f.ExcludedExtensions)
            _excluded.Add(ext);

        UpdateQuickChecks();
        UpdateExcludedList();

        chkHidden.IsChecked = f.ExcludeHiddenFiles;
        chkSystem.IsChecked = f.ExcludeSystemFiles;
    }

    private void UpdateQuickChecks()
    {
        foreach (var (cb, ext) in _quickChecks)
            cb.IsChecked = _excluded.Contains(ext);
    }

    private void UpdateExcludedList()
    {
        lstExcluded.ItemsSource = _excluded.OrderBy(e => e).ToList();
    }

    // ── Eingabe-Validierung ──────────────────────────────────────────────────
    private void NumericOnly(object sender, TextCompositionEventArgs e)
        => e.Handled = !e.Text.All(char.IsDigit);

    // ── Eigene Erweiterung hinzufügen ────────────────────────────────────────
    private void BtnAddCustom_Click(object sender, RoutedEventArgs e)
    {
        var ext = txtCustomExt.Text.Trim();
        if (!ext.StartsWith('.')) ext = '.' + ext;
        if (ext.Length < 2) return;

        _excluded.Add(ext.ToLowerInvariant());
        txtCustomExt.Clear();
        UpdateQuickChecks();
        UpdateExcludedList();
    }

    // ── Erweiterung aus Liste entfernen ──────────────────────────────────────
    private void RemoveExcluded_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string ext)
        {
            _excluded.Remove(ext);
            UpdateQuickChecks();
            UpdateExcludedList();
        }
    }

    // ── Schnellauswahl-CheckBox geändert ─────────────────────────────────────
    private void QuickCheck_Changed(object sender, RoutedEventArgs e)
    {
        // Wir reagieren hier nicht automatisch – QuickChecks werden beim OK ausgelesen
    }

    // ── OK ───────────────────────────────────────────────────────────────────
    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        // Schnellauswahl-Checkboxen auslesen
        foreach (var (cb, ext) in _quickChecks)
        {
            if (cb.IsChecked == true)
                _excluded.Add(ext);
            else
                _excluded.Remove(ext);
        }

        // Minimale Größe berechnen
        long size = 0;
        if (long.TryParse(txtMinSize.Text, out long val) && val > 0)
        {
            size = (cmbSizeUnit.SelectedIndex) switch
            {
                0 => val,
                1 => val * 1024,
                2 => val * 1024 * 1024,
                3 => val * 1024 * 1024 * 1024,
                _ => val
            };
        }

        // Filter setzen
        var f = AppFilter.Instance;
        f.MinFileSizeBytes = size;
        f.ExcludedExtensions.Clear();
        foreach (var ext in _excluded)
            f.ExcludedExtensions.Add(ext);
        f.ExcludeHiddenFiles = chkHidden.IsChecked == true;
        f.ExcludeSystemFiles = chkSystem.IsChecked == true;

        DialogResult = true;
    }

    // ── Zurücksetzen ─────────────────────────────────────────────────────────
    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _excluded.Clear();
        txtMinSize.Text = "0";
        cmbSizeUnit.SelectedIndex = 1;
        UpdateQuickChecks();
        UpdateExcludedList();
        chkHidden.IsChecked = false;
        chkSystem.IsChecked = false;
    }
}
