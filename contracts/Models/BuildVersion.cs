namespace Jewel.JPMS.Models;

/// <summary>
/// The number compiled into this assembly — and, since 2026-08-03, only the FALLBACK for the
/// version conversation. The version the API actually announces is the AppVersions database row
/// that an administrator bumps from Admin → System (see api Features/Platform): the API stamps it
/// on every HTTP response (<see cref="Header"/>, via VersionStampMiddleware) and the client
/// (<see cref="AppVersionService"/> in jpms) raises the UpdateToast when the announced number
/// rises above the one the tab baselined on. Nothing stamps this constant at deploy any more —
/// that is exactly why the announced row exists — but the client still prefers it when it parses,
/// so the original stamped-build design keeps working if a workflow ever rewrites it again.
///
/// The comparison is numeric and one-directional on purpose: only a higher announcement prompts,
/// so a publish can never loop a tab that has already refreshed onto it.
/// </summary>
public static class BuildVersion
{
    /// <summary>"dev" unless a deploy workflow ever stamps a build number over this exact text.</summary>
    public const string Value = "dev";

    /// <summary>The response header the API stamps and the client transport watches.</summary>
    public const string Header = "X-JPMS-Version";

    /// <summary>What the footer shows: "v473" on a deployed build, "dev" locally.</summary>
    public static string Display => Value == "dev" ? "dev" : $"v{Value}";
}
