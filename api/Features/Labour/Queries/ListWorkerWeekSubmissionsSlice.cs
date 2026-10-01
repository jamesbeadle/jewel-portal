using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>GET /api/labour/weeks/submissions — the weeks operatives have sent for review: the ones
/// waiting first, oldest week first, then the ones answered in the last few weeks, newest first.
/// Hours only; the approving roles read it, the directors act on it.</summary>
public sealed class ListWorkerWeekSubmissionsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ListWorkerWeekSubmissionsHandler handler;
    public ListWorkerWeekSubmissionsEndpoint(SignedInUserResolver users, ListWorkerWeekSubmissionsHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListWorkerWeekSubmissions))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/weeks/submissions")] HttpRequest request)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListWorkerWeekSubmissions(), cancellationToken));
    }
}

public sealed class ListWorkerWeekSubmissionsHandler : IQueryHandler<ListWorkerWeekSubmissions, IReadOnlyList<WorkerWeekSubmission>>
{
    private static readonly TimeSpan AnsweredShownFor = TimeSpan.FromDays(42);
    private readonly JpmsContext context;
    private readonly WorkerWeekSubmissionReader reader;
    public ListWorkerWeekSubmissionsHandler(JpmsContext context, WorkerWeekSubmissionReader reader) { this.context = context; this.reader = reader; }

    public async Task<IReadOnlyList<WorkerWeekSubmission>> HandleAsync(ListWorkerWeekSubmissions query, CancellationToken cancellationToken)
    {
        var answeredSince = DateTimeOffset.UtcNow - AnsweredShownFor;
        var waiting = await context.WorkerWeekSubmissions.AsNoTracking()
            .Where(row => row.Status == (int)WorkerWeekSubmissionStatus.Submitted)
            .OrderBy(row => row.WeekStart).ThenBy(row => row.SubmittedAt)
            .ToListAsync(cancellationToken);
        var answered = await context.WorkerWeekSubmissions.AsNoTracking()
            .Where(row => row.Status != (int)WorkerWeekSubmissionStatus.Submitted && row.ReviewedAt >= answeredSince)
            .OrderByDescending(row => row.ReviewedAt)
            .ToListAsync(cancellationToken);
        var submissions = new List<WorkerWeekSubmission>();
        foreach (var submission in waiting.Concat(answered))
            submissions.Add(await reader.ReadAsync(submission, cancellationToken));
        return submissions;
    }
}
