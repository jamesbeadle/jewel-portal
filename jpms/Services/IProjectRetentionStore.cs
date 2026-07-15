using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IProjectRetentionStore
{
    /// <summary>The project's retention terms, or null when none are recorded (or not yet
    /// loaded — the first read starts a background fetch and OnChange fires when it lands).</summary>
    ProjectRetention? RetentionFor(string projectId);

    /// <summary>Forces a background refetch even when cached. Call once on page entry
    /// (stale-while-revalidate, per the front-end data-loading convention).</summary>
    void Refresh(string projectId);

    Task<ProjectRetention> SetAsync(SetProjectRetention command);
    Task<ProjectRetention> ConfirmReleaseAsync(ConfirmRetentionRelease command);

    event Action? OnChange;
}
