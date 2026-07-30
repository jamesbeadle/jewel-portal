using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Watches the version the API reports and raises the flag the UpdateToast renders.
///
/// The transport (HttpQueryClient / HttpCommandSender) calls <see cref="ObserveResponse"/> on
/// every response it receives, so "each route load checks the version" falls out of the existing
/// data-loading convention — every page refreshes its stores on navigation, and every one of those
/// fetches carries the API's build number in a header. No extra requests, no polling. The
/// UpdateToast adds one more trigger: a backgrounded tab regaining focus asks /api/version
/// directly, because a tab nobody is navigating sends no traffic to observe.
///
/// The comparison is numeric and one-directional — see <see cref="BuildVersion"/> for why "dev"
/// never prompts and a client that is momentarily NEWER than the API (mid-deploy) must not either.
/// Once raised the flag latches: the answer to "is there a newer build?" only changes at the next
/// refresh, so re-announcing it on every subsequent response would be noise.
/// </summary>
public sealed class AppVersionService
{
    public bool UpdateAvailable { get; private set; }

    /// <summary>The newest version the API has reported — what the toast shows next to the one
    /// this bundle was built from.</summary>
    public string? LatestVersion { get; private set; }

    public event Action? OnChange;

    /// <summary>Notice the version stamped on an API response, if any. Responses without the
    /// header (local dev, SWA platform errors that never reached the functions host) say nothing
    /// either way.</summary>
    public void ObserveResponse(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(BuildVersion.Header, out var values))
            Observe(values.FirstOrDefault());
    }

    /// <summary>Notice a version reported out-of-band — the /api/version body on tab focus.</summary>
    public void Observe(string? reportedVersion)
    {
        if (UpdateAvailable) return;
        if (string.IsNullOrWhiteSpace(reportedVersion)) return;
        if (!long.TryParse(BuildVersion.Value, out var running)) return;
        if (!long.TryParse(reportedVersion.Trim(), out var reported)) return;
        if (reported <= running) return;

        LatestVersion = reportedVersion.Trim();
        UpdateAvailable = true;
        OnChange?.Invoke();
    }
}
