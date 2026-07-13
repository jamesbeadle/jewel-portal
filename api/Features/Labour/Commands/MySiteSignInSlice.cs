using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The signed-in worker signs in on arrival — creates today's site-register row for the chosen
// project. Idempotent per day. Worker resolved from the session email; no impersonation.

public sealed class MySiteSignInEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MySiteSignInHandler handler;
    public MySiteSignInEndpoint(SignedInUserResolver users, MySiteSignInHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MySiteSignIn))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/sign-in")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var command = await request.ReadFromJsonAsync<MySiteSignIn>();
        if (command is null || string.IsNullOrWhiteSpace(command.ProjectId)) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class MySiteSignInHandler : ICommandHandler<MySiteSignIn, Acknowledgement>
{
    private readonly JpmsContext context;
    public MySiteSignInHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(MySiteSignIn command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MySiteSignIn requires the signed-in email — use the endpoint.");

    public async Task<Acknowledgement> HandleAsync(MySiteSignIn command, string email, CancellationToken cancellationToken)
    {
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);

        var isAssigned = await context.ProjectWorkerAssignments.AnyAsync(
            assignment => assignment.ProjectId == command.ProjectId
                          && assignment.WorkerId == worker.WorkerId
                          && assignment.IsActive, cancellationToken);
        if (!isAssigned) throw new InvalidOperationException("You're not on this project's worker list — ask your Project Manager to add you.");

        var today = SiteClock.Today();
        var existing = await context.SiteAttendances.FirstOrDefaultAsync(
            attendance => attendance.ProjectId == command.ProjectId
                          && attendance.WorkerId == worker.WorkerId
                          && attendance.WorkDate == today, cancellationToken);
        if (existing is not null) return new Acknowledgement(existing.SiteAttendanceId);

        var attendance = new SiteAttendanceEntity
        {
            SiteAttendanceId = LabourIdentifierFactory.NextSiteAttendanceId(),
            ProjectId = command.ProjectId,
            WorkerId = worker.WorkerId,
            WorkDate = today,
            SignedInAt = DateTimeOffset.UtcNow,
        };
        context.SiteAttendances.Add(attendance);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(attendance.SiteAttendanceId);
    }
}
