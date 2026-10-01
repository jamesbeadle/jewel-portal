using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// A director's answer to a submitted week (2026-10-01, Jeremy's ask): sign it off in one step, or
// send it back with a note. Both are gated to the MD, FD and Admin (LabourRoleSets.ReviewWorkerWeeks).

public sealed class SignOffWorkerWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SignOffWorkerWeekHandler handler;
    public SignOffWorkerWeekEndpoint(SignedInUserResolver users, SignOffWorkerWeekHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SignOffWorkerWeek))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/weeks/submissions/{submissionId}/sign-off")] HttpRequest request, string submissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ReviewWorkerWeeks.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        try { return new OkObjectResult(await handler.HandleAsync(new SignOffWorkerWeek(submissionId), signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
        catch (WeekNotSignableException rejection) { return new ConflictObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class SendBackWorkerWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SendBackWorkerWeekHandler handler;
    public SendBackWorkerWeekEndpoint(SignedInUserResolver users, SendBackWorkerWeekHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SendBackWorkerWeek))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/weeks/submissions/{submissionId}/send-back")] HttpRequest request, string submissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ReviewWorkerWeeks.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var body = await request.ReadFromJsonAsync<SendBackWorkerWeek>(cancellationToken);
        if (body is null) return new BadRequestResult();
        var command = body with { WorkerWeekSubmissionId = submissionId };
        try { return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class SendBackWorkerWeekHandler : ICommandHandler<SendBackWorkerWeek, WorkerWeekSubmission>
{
    private readonly JpmsContext context;
    private readonly WorkerWeekSubmissionReader reader;
    public SendBackWorkerWeekHandler(JpmsContext context, WorkerWeekSubmissionReader reader) { this.context = context; this.reader = reader; }

    public Task<WorkerWeekSubmission> HandleAsync(SendBackWorkerWeek command, CancellationToken cancellationToken) =>
        HandleAsync(command, reviewedByEmail: "", cancellationToken);

    public async Task<WorkerWeekSubmission> HandleAsync(SendBackWorkerWeek command, string reviewedByEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Note))
            throw new InvalidOperationException("Say what needs changing — the note is what the operative reads.");
        var submission = await SubmittedWeeks.AwaitingReviewAsync(context, command.WorkerWeekSubmissionId, cancellationToken);
        submission.Status = (int)WorkerWeekSubmissionStatus.SentBack;
        submission.ReviewedByEmail = reviewedByEmail;
        submission.ReviewedAt = DateTimeOffset.UtcNow;
        submission.ReviewNote = command.Note.Trim();
        await context.SaveChangesAsync(cancellationToken);
        return await reader.ReadAsync(submission, cancellationToken);
    }
}

/// <summary>The submission a review command names, which must still be waiting.</summary>
public static class SubmittedWeeks
{
    public static async Task<WorkerWeekSubmissionEntity> AwaitingReviewAsync(JpmsContext context, string submissionId, CancellationToken cancellationToken)
    {
        var submission = await context.WorkerWeekSubmissions
            .FirstOrDefaultAsync(row => row.WorkerWeekSubmissionId == submissionId, cancellationToken)
            ?? throw new InvalidOperationException("That submitted week is no longer here.");
        var isWaiting = (WorkerWeekSubmissionStatus)submission.Status == WorkerWeekSubmissionStatus.Submitted;
        if (!isWaiting) throw new InvalidOperationException("That week has already been answered.");
        return submission;
    }
}
