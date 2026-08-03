using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Watches the version the API reports and raises the flag the UpdateToast renders.
///
/// The transport (HttpQueryClient / HttpCommandSender) calls <see cref="ObserveResponse"/> on
/// every response it receives, so "each route load checks the version" falls out of the existing
/// data-loading convention — every page refreshes its stores on navigation, and every one of those
/// fetches carries the announced version in a header. No extra requests, no polling. The
/// UpdateToast adds one more trigger: a backgrounded tab regaining focus asks /api/version
/// directly, because a tab nobody is navigating sends no traffic to observe.
///
/// What the API announces is the AppVersions database row that Admin → System publishes — not a
/// compile-time build number. The tab's own reference point (<see cref="baseline"/>) is therefore
/// the FIRST version it observes: the announced version at load time IS the version this tab was
/// served under. A deploy-stamped bundle (BuildVersion parses) still prefers its own number, so
/// the original stamped-build design keeps working if stamping ever returns.
///
/// The comparison stays numeric and one-directional: only a HIGHER announcement prompts, so a
/// publish can never loop — after the refresh the tab re-baselines on the number that prompted
/// it. Once raised the flag latches: the answer to "is there a newer version?" only changes at
/// the next refresh, so re-announcing it on every subsequent response would be noise.
/// </summary>
public sealed class AppVersionService
{
    public bool UpdateAvailable { get; private set; }

    /// <summary>The newest version the API has reported — what the toast shows next to the one
    /// this tab is running.</summary>
    public string? LatestVersion { get; private set; }

    /// <summary>The version this tab is running, for the comparison and the toast's "from" side.
    /// Null until the first version has been observed.</summary>
    private long? baseline;

    public event Action? OnChange;

    /// <summary>What the toast shows as the running version: the baked build number when it
    /// exists, otherwise the version this tab baselined on ("dev" before anything is observed).</summary>
    public string RunningDisplay =>
        BuildVersion.Value != "dev" ? BuildVersion.Display
        : baseline is long running ? $"v{running}"
        : BuildVersion.Display;

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
        if (string.IsNullOrWhiteSpace(reportedVersion)) return;
        if (!long.TryParse(reportedVersion.Trim(), out var reported)) return;

        if (UpdateAvailable)
        {
            // The flag has latched, but a second publish can land while the toast is showing —
            // keep the advertised target current so it never offers an already-old number.
            if (long.TryParse(LatestVersion, out var advertised) && reported > advertised)
            {
                LatestVersion = reported.ToString();
                OnChange?.Invoke();
            }
            return;
        }

        // First sighting: a stamped bundle knows its own number; a "dev" bundle adopts what the
        // API is announcing right now as the version it was served under.
        baseline ??= long.TryParse(BuildVersion.Value, out var built) ? built : reported;
        if (reported <= baseline) return;

        LatestVersion = reported.ToString();
        UpdateAvailable = true;
        OnChange?.Invoke();
    }
}
