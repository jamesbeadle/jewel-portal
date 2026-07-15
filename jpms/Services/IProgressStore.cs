using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Services;

public interface IProgressStore
{
    /// <summary>False until the project's progress updates have been fetched at least once.
    /// Lets views distinguish "still loading" from "genuinely nothing recorded".</summary>
    bool UpdatesLoadedFor(string projectId);

    IReadOnlyList<ProgressUpdate> UpdatesFor(string projectId);

    IReadOnlyList<ProgressReport> ReportsFor(string projectId);

    /// <summary>Starts a background refetch of the project's progress updates and reports even if
    /// cached. Call on page entry so navigating back to the Progress tab shows fresh data
    /// (stale-while-revalidate).</summary>
    void Refresh(string projectId);

    /// <summary>Creates a progress update — a group of photos with a description — uploading the
    /// files as multipart/form-data.</summary>
    Task CreateUpdateAsync(
        string projectId, string title, string description, DateTimeOffset? workDate,
        IReadOnlyList<IBrowserFile> photos, CancellationToken cancellationToken);

    /// <summary>Appends photos to an existing progress update.</summary>
    Task AddPhotosAsync(
        string projectId, string progressUpdateId,
        IReadOnlyList<IBrowserFile> photos, CancellationToken cancellationToken);

    /// <summary>Edits a progress update's title, description and work date.</summary>
    Task UpdateUpdateAsync(
        string projectId, string progressUpdateId, string title, string description,
        DateTimeOffset? workDate, CancellationToken cancellationToken);

    /// <summary>Permanently deletes a progress update, its photos and their stored files.
    /// Cannot be undone.</summary>
    Task DeleteUpdateAsync(string projectId, string progressUpdateId, CancellationToken cancellationToken);

    /// <summary>Permanently deletes a single photo and its stored file. Cannot be undone.</summary>
    Task DeletePhotoAsync(string projectId, string progressUpdateId, string progressPhotoId, CancellationToken cancellationToken);

    /// <summary>Creates a client-facing progress report from narrative sections and an ordered
    /// selection of progress update ids.</summary>
    Task<ProgressReport> CreateReportAsync(
        string projectId, string title, DateTimeOffset? periodStart, DateTimeOffset? periodEnd,
        string introduction, string workCompleted, string upcomingWorks,
        IReadOnlyList<string> selectedUpdateIds, CancellationToken cancellationToken);

    /// <summary>Replaces a report's narrative sections and selected updates.</summary>
    Task<ProgressReport> UpdateReportAsync(
        string projectId, string progressReportId, string title, DateTimeOffset? periodStart,
        DateTimeOffset? periodEnd, string introduction, string workCompleted, string upcomingWorks,
        IReadOnlyList<string> selectedUpdateIds, CancellationToken cancellationToken);

    /// <summary>Permanently deletes a report; the underlying updates and photos are untouched.</summary>
    Task DeleteReportAsync(string projectId, string progressReportId, CancellationToken cancellationToken);

    event Action? OnChange;
}
