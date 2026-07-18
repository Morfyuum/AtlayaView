using System.Windows;
using System.Windows.Threading;
using AtlayaView.Core;

namespace AtlayaView;

public partial class App : Application
{
    /// <summary>Globaler Lokalisierungs-Manager – für XAML: {x:Static app:App.Loc}</summary>
    public static Core.LocalizationManager Loc => Core.LocalizationManager.Instance;

    /// <summary>Geladene Einstellungen – von MainWindow beim Start ausgelesen.</summary>
    internal static SettingsStore Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Einstellungen laden und auf alle Singletons anwenden
        Settings = SettingsStore.Load();
        Settings.Apply();

        // High-DPI aware rendering
        System.Windows.Media.RenderOptions.ProcessRenderMode =
            System.Windows.Interop.RenderMode.Default;

        // Unbehandelte Ausnahmen abfangen und anzeigen statt still abstürzen
        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show(
                $"Unerwarteter Fehler:\n\n{ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                "AtlayaView – Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };
    }
}
