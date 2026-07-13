using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Commercial;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// End-of-day allocation + sign-out for the signed-in worker: one Submitted timesheet per cost
// code, attendance closed, single SaveChanges. One sign-out per project per day.

public sealed class MySiteSignOutEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MySiteSignOutHandler handler;
    public MySiteSignOutEndpoint(SignedInUserResolver users, MySiteSignOutHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(MySiteSignOut))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/sign-out")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        var command = await request.ReadFromJsonAsync<MySiteSignOut>();
        if (command is null || string.IsNullOrWhiteSpace(command.ProjectId) || command.Entries is null) return new BadRequestResult();
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

public sealed class MySiteSignOutHandler : ICommandHandler<MySiteSignOut, Acknowledgement>
{
    private readonly JpmsContext context;
    public MySiteSignOutHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(MySiteSignOut command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("MySiteSignOut requires the signed-in email — use the endpoint.");

    public async Task<Acknowledgement> HandleAsync(MySiteSignOut command, string email, CancellationToken cancellationToken)
    {
        var worker = await WorkerByEmail.ResolveAsync(context, email, cancellationToken);

        var today = SiteClock.Today();
        var attendance = await context.SiteAttendances.FirstOrDefaultAsync(
            row => row.ProjectId == command.ProjectId
                   && row.WorkerId == worker.WorkerId
                   && row.WorkDate == today, cancellationToken)
            ?? throw new InvalidOperationException("You haven't signed in today — sign in first.");

        if (attendance.SignedOutAt is not null)
            throw new InvalidOperationException("You've already signed out today. Contact your Project Manager if you need to amend your hours.");

        var budgetedCodes = await context.CostCodeBudgets
            .Where(budget => budget.ProjectId == command.ProjectId)
            .Select(budget => budget.CostCode)
            .ToListAsync(cancellationToken);
        var allowedCodes = budgetedCodes.Count > 0
            ? budgetedCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : (await context.CostCenters.Where(centre => centre.IsActive)
                .Select(centre => centre.Code).ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = LabourRules.CheckSignOutEntries(command.Entries, allowedCodes);
        if (errors.Count > 0) throw new InvalidOperationException(string.Join(" ", errors));

        foreach (var entry in command.Entries)
        {
            context.Timesheets.Add(new TimesheetEntity
            {
                TimesheetId = CommercialIdentifierFactory.NextTimesheetId(),
                ProjectId = command.ProjectId,
                PersonEmail = email,
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
