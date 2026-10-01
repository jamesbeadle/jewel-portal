using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>POST /api/my/labour/weeks/submit — the worker sends a finished week to the office in one
/// step. Refused until Friday is logged and every working day is accounted for; a week sent back is
/// submitted again on the same row. Answers with the week as it now reads.</summary>
public sealed class MySubmitWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MySubmitWeekHandler handler;
    public MySubmitWeekEndpoint(SignedInUserResolver users, MySubmitWeekHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MySubmitWeek))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/weeks/submit")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<MySubmitWeek>(cancellationToken);
        if (command is null) return new BadRequestResult();
        try { return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, cancellationToken)); }
        catch (InvalidOperationException rejection) { return new BadRequestObjectResult(new[] { rejection.Message }); }
    }
}

public sealed class MySubmitWeekHandler : ICommandHandler<MySubmitWeek, MyLabourWeek>
{
    private readonly JpmsContext context;
    private readonly GetMyLabourWeekHandler week;
    public MySubmitWeekHandler(JpmsContext context, GetMyLabourWeekHandler week) { this.context = context; this.week = week; }

    public Task<MyLabourWeek> HandleAsync(MySubmitWeek command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MySubmitWeek requires the signed-in email — use the endpoint.");

    public async Task<MyLabourWeek> HandleAsync(MySubmitWeek command, string email, CancellationToken cancellationToken)
    {
        var today = SiteClock.Today();
        var monday = LabourWeeks.MondayOf(command.WeekStart);
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);
        var before = await week.ForAsync(worker, email, monday, today, cancellationToken);
        if (!before.CanBeSubmitted) throw new InvalidOperationException(before.SubmitRefusal);

        var submission = await WorkerWeekSubmissions.FindAsync(context, worker.WorkerId, monday, cancellationToken)
            ?? NewSubmission(worker, monday);
        submission.Status = (int)WorkerWeekSubmissionStatus.Submitted;
        submission.SubmittedAt = DateTimeOffset.UtcNow;
        submission.ReviewedByEmail = "";
        submission.ReviewedAt = null;
        submission.ReviewNote = "";
        await context.SaveChangesAsync(cancellationToken);
        return await week.ForAsync(worker, email, monday, today, cancellationToken);
    }

    private WorkerWeekSubmissionEntity NewSubmission(WorkerEntity worker, DateTimeOffset monday)
    {
        var submission = new WorkerWeekSubmissionEntity
        {
            WorkerWeekSubmissionId = LabourIdentifierFactory.NextWorkerWeekSubmissionId(),
            WorkerId = worker.WorkerId,
            WeekStart = monday,
        };
        context.WorkerWeekSubmissions.Add(submission);
        return submission;
    }
}
