using Jewel.JPMS.Contracts.Retention;

namespace Jewel.JPMS.Services;

public interface IProjectRetentionStore
{
    /// <summary>The project's retention terms, or null when none are recorded (or not yet
    /// loaded — the first read starts a background fetch and OnChange fires when it lands).</summary>
    ProjectRetention? RetentionFor(string projectId);

    /// <summary>True once the fetch has landed. Needed because null is a legitimate answer here
    /// ("no retention terms recorded"), so null alone cannot tell "none" from "not yet".</summary>
    bool RetentionLoadedFor(string projectId);

    /// <summary>Forces a background refetch even when cached. Call once on page entry
    /// (stale-while-revalidate, per the front-end data-loading convention).</summary>
    void Refresh(string projectId);

    Task<ProjectRetention> SetAsync(SetProjectRetention command);
    Task<ProjectRetention> ConfirmReleaseAsync(ConfirmRetentionRelease command);

    event Action? OnChange;
}
