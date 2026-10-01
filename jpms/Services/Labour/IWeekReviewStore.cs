namespace Jewel.JPMS.Services;

/// <summary>The weeks operatives have submitted, and the directors' answers to them.</summary>
public interface IWeekReviewStore
{
    event Action? OnChange;

    IReadOnlyList<WorkerWeekSubmission> Submissions { get; }
    bool IsLoaded { get; }
    bool HasSubmissions { get; }
    Task RefreshAsync();
    Task<WorkerWeekSubmission> SignOffAsync(string submissionId);
    Task<WorkerWeekSubmission> SendBackAsync(string submissionId, string note);
}
