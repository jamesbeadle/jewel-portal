using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Anonymous, token-authenticated (SiteAccessGate): a worker signs in on arrival, creating the
// day's site-register row. Idempotent — signing in twice on the same day is a no-op.

public sealed class SiteSignInEndpoint
{
    private readonly SiteAccessGate gate;
    private readonly ICommandHandler<SiteSignIn, Acknowledgement> handler;
    public SiteSignInEndpoint(SiteAccessGate gate, ICommandHandler<SiteSignIn, Acknowledgement> handler)
    { this.gate = gate; this.handler = handler; }

    [Function(nameof(SiteSignIn))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-labour/{token}/sign-in")] HttpRequest request, string token)
    {
        var access = await gate.ResolveAsync(token, request.HttpContext.RequestAborted);
        if (access is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<SiteSignIn>();
        if (body is null || string.IsNullOrWhiteSpace(body.WorkerId)) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(body with { Token = token }, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException rejection)
        {
            return new BadRequestObjectResult(new[] { rejection.Message });
        }
    }
}

public sealed class SiteSignInHandler : ICommandHandler<SiteSignIn, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly SiteAccessGate gate;
    public SiteSignInHandler(JpmsContext context, SiteAccessGate gate) { this.context = context; this.gate = gate; }

    public async Task<Acknowledgement> HandleAsync(SiteSignIn command, CancellationToken cancellationToken)
    {
        var access = await gate.ResolveAsync(command.Token, cancellationToken)
            ?? throw new InvalidOperationException("Site access token is not valid.");

        var isAssigned = await context.ProjectWorkerAssignments.AnyAsync(
            assignment => assignment.ProjectId == access.ProjectId
                          && assignment.WorkerId == command.WorkerId
                          && assignment.IsActive, cancellationToken);
        if (!isAssigned) throw new InvalidOperationException("You are not on this project's sign-in list. Ask your Project Manager to add you.");

        var today = SiteClock.Today();
        var existing = await context.SiteAttendances.FirstOrDefaultAsync(
            attendance => attendance.ProjectId == access.ProjectId
                          && attendance.WorkerId == command.WorkerId
                          && attendance.WorkDate == today, cancellationToken);
        if (existing is not null) return new Acknowledgement(existing.SiteAttendanceId);

        var attendance = new SiteAttendanceEntity
        {
            SiteAttendanceId = LabourIdentifierFactory.NextSiteAttendanceId(),
            ProjectId = access.ProjectId,
            WorkerId = command.WorkerId,
            WorkDate = today,
            SignedInAt = DateTimeOffset.UtcNow,
        };
        context.SiteAttendances.Add(attendance);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(attendance.SiteAttendanceId);
    }
}
