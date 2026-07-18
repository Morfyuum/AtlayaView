using System.IO;
using System.Text.Json;

namespace AtlayaView.Core;

internal sealed class UpdateCheckState
{
    public DateTime? LastCheckUtc { get; set; }
}

/// <summary>
/// Prüft in konfigurierbarem Abstand (siehe <see cref="UpdatePreferences"/>) auf neue
/// AtlayaView-Versionen. AtlayaView läuft nicht dauerhaft im Hintergrund (kein Dienst),
/// daher genügt ein grober Poll-Timer statt exakter Zeitplanung – beim Start (erster Tick
/// sofort) wird ohnehin geprüft, ob eine Prüfung fällig ist.
/// </summary>
public sealed class UpdateScheduler : IDisposable
{
    private static readonly Dictionary<string, int> IntervalDays = new()
    {
        ["daily"] = 1,
        ["weekly"] = 7,
        ["monthly"] = 30,
        ["yearly"] = 365,
    };

    private static string StatePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AtlayaView", "update_check_state.json");

    private readonly Func<Task> _onDue;
    private System.Threading.Timer? _timer;

    public UpdateScheduler(Func<Task> onDue) => _onDue = onDue;

    /// <summary>Startet den Poll-Timer (alle 30 Minuten geprüft, ob eine Prüfung fällig ist).</summary>
    public void Start()
    {
        _timer?.Dispose();
        _timer = new System.Threading.Timer(
            _ => _ = TickAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
    }

    private async Task TickAsync()
    {
        if (!IsDue()) return;
        try
        {
            await _onDue().ConfigureAwait(false);
        }
        catch
        {
            // Nächster Poll versucht es erneut.
        }
    }

    public static bool IsDue()
    {
        string mode = UpdatePreferences.Instance.CheckMode;
        if (mode == "manual") return false;
        var last = ReadLastCheck();
        if (last is null) return true;
        int days = IntervalDays.GetValueOrDefault(UpdatePreferences.Instance.CheckInterval, 7);
        return DateTime.UtcNow >= last.Value.AddDays(days);
    }

    public static DateTime? ReadLastCheck()
    {
        try
        {
            if (!File.Exists(StatePath)) return null;
            var state = JsonSerializer.Deserialize<UpdateCheckState>(File.ReadAllText(StatePath));
            return state?.LastCheckUtc;
        }
        catch
        {
            return null;
        }
    }

    public static void WriteLastCheck(DateTime whenUtc)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            File.WriteAllText(
                StatePath, JsonSerializer.Serialize(new UpdateCheckState { LastCheckUtc = whenUtc }));
        }
        catch
        {
            // Speicherfehler ignorieren – naechste Pruefung greift wieder auf "nie geprueft" zurueck.
        }
    }

    public void Dispose() => _timer?.Dispose();
}
