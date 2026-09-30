using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// POST /api/my/labour/day-off — a day off recorded as a day: the note on the project's progress
/// feed says why, in the worker's own name, and nothing else is written — today, or a day already
/// gone that the worker missed. Refused once the worker has signed in on that day (sign out to
/// log it instead) or has already logged it.
/// </summary>
public sealed class MySiteDayOffEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MySiteDayOffHandler handler;
    public MySiteDayOffEndpoint(SignedInUserResolver users, MySiteDayOffHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MySiteDayOff))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/day-off")] HttpRequest request)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<MySiteDayOff>(cancellationToken);
        if (command is null || string.IsNullOrWhiteSpace(command.ProjectId)) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, cancellationToken));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class MySiteDayOffHandler : ICommandHandler<MySiteDayOff, MySiteDayLogged>
{
    private readonly JpmsContext context;
    public MySiteDayOffHandler(JpmsContext context) { this.context = context; }

    public Task<MySiteDayLogged> HandleAsync(MySiteDayOff command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MySiteDayOff requires the signed-in email — use the endpoint.");

    public async Task<MySiteDayLogged> HandleAsync(MySiteDayOff command, string email, CancellationToken cancellationToken)
    {
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);
        await MyDayAssignment.EnsureAssignedAsync(context, command.ProjectId, worker, cancellationToken);
        if (!MyDayNotes.IsGiven(command.Description))
            throw new InvalidOperationException("Say why — rain, holiday, no works on site — so the day is recorded, not missing.");
        var workDate = MyDayFiling.ResolveWorkDate(command.Date, SiteClock.Today());
        await MyDayVacancy.EnsureAsync(context, command.ProjectId, worker, email, workDate, cancellationToken);

        var note = ProgressUpdateRows.New(
            ProgressIdentifierFactory.NextProgressUpdateId(), command.ProjectId, MyDayNotes.OffTitle(worker),
            command.Description, workDate, null, email, DateTimeOffset.UtcNow);
        context.ProgressUpdates.Add(note);
        await context.SaveChangesAsync(cancellationToken);
        return new MySiteDayLogged("", note.ProgressUpdateId);
    }
}
