using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Anonymous, token-authenticated (SiteAccessGate): the end-of-day allocation. One Submitted
// timesheet per cost code, attendance closed, all in a single SaveChanges. One sign-out per
// worker per day (spec constraint) — a second attempt gets a friendly rejection.

public sealed class SiteSignOutEndpoint
{
    private readonly SiteAccessGate gate;
    private readonly ICommandHandler<SiteSignOut, Acknowledgement> handler;
    public SiteSignOutEndpoint(SiteAccessGate gate, ICommandHandler<SiteSignOut, Acknowledgement> handler)
    { this.gate = gate; this.handler = handler; }

    [Function(nameof(SiteSignOut))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-labour/{token}/sign-out")] HttpRequest request, string token)
    {
        var access = await gate.ResolveAsync(token, request.HttpContext.RequestAborted);
        if (access is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<SiteSignOut>();
        if (body is null || string.IsNullOrWhiteSpace(body.WorkerId) || body.Entries is null) return new BadRequestResult();
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

public sealed class SiteSignOutHandler : ICommandHandler<SiteSignOut, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly SiteAccessGate gate;
    public SiteSignOutHandler(JpmsContext context, SiteAccessGate gate) { this.context = context; this.gate = gate; }

    public async Task<Acknowledgement> HandleAsync(SiteSignOut command, CancellationToken cancellationToken)
    {
        var access = await gate.ResolveAsync(command.Token, cancellationToken)
            ?? throw new InvalidOperationException("Site access token is not valid.");

        var today = SiteClock.Today();
        var attendance = await context.SiteAttendances.FirstOrDefaultAsync(
            row => row.ProjectId == access.ProjectId
                   && row.WorkerId == command.WorkerId
                   && row.WorkDate == today, cancellationToken)
            ?? throw new InvalidOperationException("You haven't signed in today — sign in first.");

        if (attendance.SignedOutAt is not null)
            throw new InvalidOperationException("You've already signed out today. Contact your Project Manager if you need to amend your hours.");

        var budgetedCodes = await context.CostCodeBudgets
            .Where(budget => budget.ProjectId == access.ProjectId)
            .Select(budget => budget.CostCode)
            .ToListAsync(cancellationToken);
        var allowedCodes = budgetedCodes.Count > 0
            ? budgetedCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : (await context.CostCenters.Where(centre => centre.IsActive)
                .Select(centre => centre.Code).ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = LabourRules.CheckSignOutEntries(command.Entries, allowedCodes);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));

        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException("Worker not found.");

        foreach (var entry in command.Entries)
        {
            context.Timesheets.Add(new TimesheetEntity
            {
                TimesheetId = CommercialIdentifierFactory.NextTimesheetId(),
                ProjectId = access.ProjectId,
                PersonEmail = worker.ContactEmail,
                WorkerId = worker.WorkerId,
                SiteAttendanceId = attendance.SiteAttendanceId,
                WorkedOn = today,
                Hours = entry.Hours,
                CostCode = entry.CostCode,
                Status = (int)TimesheetStatus.Submitted,
                IsApproved = false,
            });
        }

        attendance.SignedOutAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(attendance.SiteAttendanceId);
    }
}
