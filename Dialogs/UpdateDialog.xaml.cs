using System.ComponentModel;
using System.Windows;
using AtlayaView.Core;

namespace AtlayaView.Dialogs;

public partial class UpdateDialog : Window
{
    private readonly string _currentVersion = LocalizationManager.CurrentVersion;
    private UpdateInfo? _info;

    public UpdateDialog()
    {
        InitializeComponent();
        txtCurrent.Text = $"{App.Loc.UpdateDlgCurrent} {_currentVersion}";
        txtStatus.Text = App.Loc.UpdateDlgChecking;
        Loaded += async (_, _) => await CheckAsync();
    }

    private async Task CheckAsync()
    {
        try
        {
            _info = await UpdateChecker.FetchLatestAsync();
        }
        catch
        {
            _info = null;
        }

        UpdateScheduler.WriteLastCheck(DateTime.UtcNow);

        if (_info is null || string.IsNullOrEmpty(_info.Version))
        {
            txtStatus.Text = App.Loc.UpdateDlgUnreachable;
            return;
        }

        if (!UpdateChecker.IsNewer(_info.Version, _currentVersion))
        {
            txtStatus.Text = App.Loc.UpdateDlgUpToDate;
            return;
        }

        txtStatus.Text = $"{App.Loc.UpdateDlgAvailable} {_info.Version}";

        if (SelfUpdater.ResolveMatchingUrl(_info) is not null)
        {
            txtNotes.Text = _info.Notes;
            btnApply.Visibility = Visibility.Visible;
        }
        else
        {
            txtNotes.Text = App.Loc.UpdateDlgNoMatchingVariant;
        }
    }

    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_info is null) return;
        var url = SelfUpdater.ResolveMatchingUrl(_info);
        if (url is null) return;

        btnApply.IsEnabled = false;
        var progress = new Progress<string>(msg => txtStatus.Text = msg);
        txtStatus.Text = App.Loc.UpdateDlgApplying;

        try
        {
            string extractedDir = await SelfUpdater.DownloadAndExtractAsync(url, progress);
            SelfUpdater.LaunchSwapAndExit(extractedDir);
        }
        catch (Win32Exception winEx) when (winEx.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED -- Nutzer hat die UAC-Sicherheitsabfrage abgelehnt.
            txtStatus.Text = App.Loc.UpdateDlgElevationCancelled;
            btnApply.IsEnabled = true;
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"{App.Loc.UpdateDlgDownloadFailed} {ex.Message}";
            btnApply.IsEnabled = true;
        }
    }
}
