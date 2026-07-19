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
    private bool   _suppressFastScanEvent;

    private static readonly string[] UpdateModes = ["manual", "auto_check", "auto_apply"];
    private static readonly string[] UpdateIntervals = ["startup", "daily", "weekly", "monthly", "yearly"];

    /// <summary>Ergebnis nach Klick auf OK.</summary>
    public bool ShowFreeSpaceCushionResult { get; private set; }

    public OptionsDialog(bool showFreeSpaceCushion = true)
    {
        InitializeComponent();
        WindowFrameFix.Apply(this);

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

        _suppressFastScanEvent = true;
        chkFastScan.IsChecked = ScanPreferences.Instance.FastScanEnabled;
        _suppressFastScanEvent = false;

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

    private void ChkFastScan_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressFastScanEvent || ElevationHelper.IsElevated) return;

        var answer = MessageBox.Show(
            App.Loc.MsgFastScanElevateText, App.Loc.MsgFastScanElevateTitle,
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            chkFastScan.IsChecked = false;
            return;
        }

        // Einstellung + aktuelle Dialog-Werte vor dem Neustart sichern, sonst geht der
        // Haken beim erhöhten Neustart wieder verloren.
        ScanPreferences.Instance.FastScanEnabled = true;
        SettingsStore.Save(chkFreeSpaceCushion.IsChecked == true);

        if (!ElevationHelper.TryRelaunchElevated())
        {
            ScanPreferences.Instance.FastScanEnabled = false;
            SettingsStore.Save(chkFreeSpaceCushion.IsChecked == true);
            chkFastScan.IsChecked = false;
            MessageBox.Show(App.Loc.MsgFastScanElevateDenied, App.Loc.MsgFastScanElevateTitle,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        // Bei Erfolg beendet TryRelaunchElevated() den aktuellen (nicht-erhöhten) Prozess bereits.
    }

    private void BtnDefaults_Click(object sender, RoutedEventArgs e)
    {
        slCushionHeight.Value = AppSettings.DefaultCushionHeight;
        slCushionDecay.Value  = AppSettings.DefaultCushionDecay;
        slAmbientLight.Value  = AppSettings.DefaultAmbientLight;
        slMinPixel.Value      = AppSettings.DefaultMinPixelSize;
        chkBorders.IsChecked  = AppSettings.DefaultShowBorders;
        chkFreeSpaceCushion.IsChecked = true;

        _suppressFastScanEvent = true;
        chkFastScan.IsChecked = false;
        _suppressFastScanEvent = false;
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

        // Nur tatsächlich elevated darf FastScanEnabled aktiv bleiben – ohne erhöhte Rechte
        // greift ohnehin immer der Fallback auf den normalen Scanner, aber so bleibt die
        // Checkbox ehrlich (kein "aktiviert", das nie etwas bewirkt).
        ScanPreferences.Instance.FastScanEnabled = chkFastScan.IsChecked == true && ElevationHelper.IsElevated;

        DialogResult    = true;
    }
}
