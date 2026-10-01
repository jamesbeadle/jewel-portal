using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Services;

public sealed class HttpWeekReviewStore : IWeekReviewStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private IReadOnlyList<WorkerWeekSubmission>? submissions;

    public HttpWeekReviewStore(IQueryClient queries, ICommandSender commands) { this.queries = queries; this.commands = commands; }

    public event Action? OnChange;

    public IReadOnlyList<WorkerWeekSubmission> Submissions => submissions ?? Array.Empty<WorkerWeekSubmission>();

    public bool IsLoaded => submissions is not null;

    public bool HasSubmissions => Submissions.Count > 0;

    public async Task RefreshAsync()
    {
        submissions = await queries.AskAsync(new ListWorkerWeekSubmissions(), CancellationToken.None);
        OnChange?.Invoke();
    }

    public async Task<WorkerWeekSubmission> SignOffAsync(string submissionId)
    {
        var answered = await commands.SendAsync(new SignOffWorkerWeek(submissionId), CancellationToken.None);
        await RefreshAsync();
        return answered;
    }

    public async Task<WorkerWeekSubmission> SendBackAsync(string submissionId, string note)
    {
        var answered = await commands.SendAsync(new SendBackWorkerWeek(submissionId, note), CancellationToken.None);
        await RefreshAsync();
        return answered;
    }
}
