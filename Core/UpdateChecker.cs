using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace AtlayaView.Core;

/// <summary>Ergebnis einer Update-Prüfung gegen den AtlayaView-Feed.</summary>
public sealed record UpdateInfo(string Version, string? UrlFull, string? UrlFx, string Notes);

/// <summary>
/// Fragt den öffentlichen AtlayaView-Update-Feed ab (einzige Netzwerkfunktion von
/// AtlayaView, nur bei Update-Prüfung genutzt). Feed-Quelle:
/// scripts/build_website.py -> write_update_feed_atlayaview() im Atlaya-Repo,
/// url_full/url_fx sind relativ zur Website-Wurzel (nicht zu /atlayaview/).
/// </summary>
public static class UpdateChecker
{
    private const string LegacyFeedUrl = "https://atlaya.capecter.com/atlayaview/updates/latest.json";
    private const string LegacySiteRoot = "https://atlaya.capecter.com/";
    // Leer = bisheriger Website-Feed bleibt aktiv. Sobald dein GitHub-Owner hier steht,
    // liest AtlayaView die neueste Version direkt aus GitHub Releases.
    private const string GitHubOwner = "";
    private const string GitHubRepo = "AtlayaView";
    private static readonly Regex FullZipPattern = new(@"-win-x64(?:-full)?\.zip$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FxZipPattern = new(@"-win-x64-fx\.zip$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsGitHubConfigured =>
        !string.IsNullOrWhiteSpace(GitHubOwner) &&
        !string.IsNullOrWhiteSpace(GitHubRepo);

    public static async Task<UpdateInfo?> FetchLatestAsync(CancellationToken ct = default)
    {
        using var client = CreateClient();
        return IsGitHubConfigured
            ? await FetchLatestFromGitHubAsync(client, ct).ConfigureAwait(false)
            : await FetchLatestFromLegacyFeedAsync(client, ct).ConfigureAwait(false);
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AtlayaView", LocalizationManager.CurrentVersion));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static async Task<UpdateInfo?> FetchLatestFromLegacyFeedAsync(HttpClient client, CancellationToken ct)
    {
        var json = await client.GetStringAsync(LegacyFeedUrl, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string version = root.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
        string notes = root.TryGetProperty("notes", out var n) ? n.GetString() ?? "" : "";
        string? urlFull = ResolveLegacyUrl(root, "url_full");
        string? urlFx = ResolveLegacyUrl(root, "url_fx");
        return new UpdateInfo(version, urlFull, urlFx, notes);
    }

    private static async Task<UpdateInfo?> FetchLatestFromGitHubAsync(HttpClient client, CancellationToken ct)
    {
        string apiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
        var json = await client.GetStringAsync(apiUrl, ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string version = root.TryGetProperty("tag_name", out var tag) ? NormalizeVersion(tag.GetString()) : "";
        string notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";
        string? urlFull = null;
        string? urlFx = null;

        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            foreach (var asset in assets.EnumerateArray())
            {
                if (!asset.TryGetProperty("name", out var nameEl) || !asset.TryGetProperty("browser_download_url", out var urlEl))
                {
                    continue;
                }

                string name = nameEl.GetString() ?? string.Empty;
                string? downloadUrl = urlEl.GetString();
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    continue;
                }

                if (FxZipPattern.IsMatch(name))
                {
                    urlFx = downloadUrl;
                    continue;
                }

                if (FullZipPattern.IsMatch(name))
                {
                    urlFull = downloadUrl;
                }
            }
        }

        return new UpdateInfo(version, urlFull, urlFx, notes);
    }

    private static string? ResolveLegacyUrl(JsonElement root, string field)
    {
        if (!root.TryGetProperty(field, out var el)) return null;
        var rel = el.GetString();
        if (string.IsNullOrEmpty(rel)) return null;
        return new Uri(new Uri(LegacySiteRoot), rel).ToString();
    }

    private static string NormalizeVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return value![0] is 'v' or 'V' ? value[1..] : value;
    }

    /// <summary>true, wenn candidate eine höhere Version als current ist (analog
    /// core/update_checker.py: parse_version/is_newer).</summary>
    public static bool IsNewer(string candidate, string current)
    {
        var a = ParseVersion(candidate);
        var b = ParseVersion(current);
        int len = Math.Max(a.Length, b.Length);
        for (int i = 0; i < len; i++)
        {
            int av = i < a.Length ? a[i] : 0;
            int bv = i < b.Length ? b[i] : 0;
            if (av != bv) return av > bv;
        }
        return false;
    }

    private static int[] ParseVersion(string v) =>
        v.Split(['.', '-', '+'])
         .Select(p => int.TryParse(p, out var n) ? n : 0)
         .ToArray();
}
