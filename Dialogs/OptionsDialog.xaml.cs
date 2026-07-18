using System.Windows;
using AtlayaView.Core;

namespace AtlayaView.Dialogs;

public partial class OptionsDialog : Window
{
    // Lokale Arbeitskopie der Einstellungen
    private double _cushionHeight;
    private double _cushionDecay;
    private double _ambientLight;
    private double _minPixelSize;
    private bool   _showBorders;
    private bool   _showFreeSpaceCushion;

    private static readonly string[] UpdateModes = ["manual", "auto_check", "auto_apply"];
    private static readonly string[] UpdateIntervals = ["daily", "weekly", "monthly", "yearly"];

    /// <summary>Ergebnis nach Klick auf OK.</summary>
    public bool ShowFreeSpaceCushionResult { get; private set; }

    public OptionsDialog(bool showFreeSpaceCushion = true)
    {
        InitializeComponent();

        // Aktuelle Werte laden
        var s = AppSettings.Instance;
        _cushionHeight = s.CushionHeight;
        _cushionDecay  = s.CushionDecay;
        _ambientLight  = s.AmbientLight;
        _minPixelSize  = s.MinPixelSize;
        _showBorders   = s.ShowBorders;

        // Slider initialisieren (keine Events auslösen)
        slCushionHeight.Value = _cushionHeight;
        slCushionDecay.Value  = _cushionDecay;
        slAmbientLight.Value  = _ambientLight;
        slMinPixel.Value      = _minPixelSize;
        chkBorders.IsChecked  = _showBorders;
        chkFreeSpaceCushion.IsChecked = _showFreeSpaceCushion = showFreeSpaceCushion;

        var up = UpdatePreferences.Instance;
        cmbUpdateMode.SelectedIndex = Math.Max(0, Array.IndexOf(UpdateModes, up.CheckMode));
        cmbUpdateInterval.SelectedIndex = Math.Max(0, Array.IndexOf(UpdateIntervals, up.CheckInterval));

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        lblCushionHeight.Text = $"{slCushionHeight.Value:F2}";
        lblCushionDecay.Text  = $"{slCushionDecay.Value:F2}";
        lblAmbientLight.Text  = $"{slAmbientLight.Value * 100:F0} %";
        lblMinPixel.Text      = $"{slMinPixel.Value:F0} px";
    }

    private void SlCushionHeight_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        => lblCushionHeight.Text = $"{e.NewValue:F2}";

    private void SlCushionDecay_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        => lblCushionDecay.Text = $"{e.NewValue:F2}";

    private void SlAmbientLight_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        => lblAmbientLight.Text = $"{e.NewValue * 100:F0} %";

    private void SlMinPixel_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        => lblMinPixel.Text = $"{e.NewValue:F0} px";

    private void BtnDefaults_Click(object sender, RoutedEventArgs e)
    {
        slCushionHeight.Value = AppSettings.DefaultCushionHeight;
        slCushionDecay.Value  = AppSettings.DefaultCushionDecay;
        slAmbientLight.Value  = AppSettings.DefaultAmbientLight;
        slMinPixel.Value      = AppSettings.DefaultMinPixelSize;
        chkBorders.IsChecked  = AppSettings.DefaultShowBorders;
        chkFreeSpaceCushion.IsChecked = true;
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        var s = AppSettings.Instance;
        s.CushionHeight = slCushionHeight.Value;
        s.CushionDecay  = slCushionDecay.Value;
        s.AmbientLight  = slAmbientLight.Value;
        s.MinPixelSize  = slMinPixel.Value;
        s.ShowBorders   = chkBorders.IsChecked == true;
        ShowFreeSpaceCushionResult = chkFreeSpaceCushion.IsChecked == true;

        var up = UpdatePreferences.Instance;
        up.CheckMode     = UpdateModes[Math.Max(0, cmbUpdateMode.SelectedIndex)];
        up.CheckInterval = UpdateIntervals[Math.Max(0, cmbUpdateInterval.SelectedIndex)];

        DialogResult    = true;
    }
}
