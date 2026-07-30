namespace Jewel.JPMS.Models;

/// <summary>
/// The number of the deploy this assembly was compiled from.
///
/// "dev" outside a deploy. The deploy workflow (jpms-swa.yml) rewrites <see cref="Value"/> with
/// the GitHub run number BEFORE either side is built, so the Blazor bundle and the API always
/// carry the same number for a given deploy — no table to bump, nothing to remember. The API
/// stamps its number on every HTTP response (<see cref="Header"/>, via VersionStampMiddleware)
/// and the client transport compares it to its own (<see cref="AppVersionService"/> in jpms): a
/// HIGHER number from the API means this tab was built by an earlier deploy, and the UpdateToast
/// offers a refresh.
///
/// The comparison is numeric and one-directional on purpose. "dev" never prompts — a local client
/// against the deployed API, or vice versa, is a build difference, not an update. And a client
/// momentarily NEWER than the API (the seconds mid-deploy where the app has swapped but the
/// functions have not) must not prompt either: the refresh would land on the same build it came
/// from and prompt again, forever.
/// </summary>
public static class BuildVersion
{
    /// <summary>Rewritten at deploy — the workflow's sed matches this exact text, and greps for
    /// the stamped value afterwards so a drift here fails the build instead of silently shipping
    /// "dev".</summary>
    public const string Value = "dev";

    /// <summary>The response header the API stamps and the client transport watches.</summary>
    public const string Header = "X-JPMS-Version";

    /// <summary>What the footer shows: "v473" on a deployed build, "dev" locally.</summary>
    public static string Display => Value == "dev" ? "dev" : $"v{Value}";
}
