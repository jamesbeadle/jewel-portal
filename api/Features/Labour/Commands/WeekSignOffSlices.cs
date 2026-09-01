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

// The weekly sign-off marker (scope §4). Sign-off is a view over approval, never a second state
// machine: the server re-checks ForecastRules.WeekIsSignable at the moment of signing, and
// removing the marker touches no timesheet.

public sealed class SignOffLabourWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SignOffLabourWeekHandler handler;
    public SignOffLabourWeekEndpoint(SignedInUserResolver users, SignOffLabourWeekHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SignOffLabourWeek))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/weeks/sign-off")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<SignOffLabourWeek>();
        if (command is null) return new BadRequestResult();
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
        }
        catch (WeekNotSignableException exception)
        {
            return new ConflictObjectResult(new[] { exception.Message });
        }
    }
}

public sealed class WeekNotSignableException : Exception
{
    public WeekNotSignableException(string message) : base(message) { }
}

public sealed class SignOffLabourWeekHandler : ICommandHandler<SignOffLabourWeek, LabourWeekSignOff>
{
    private readonly JpmsContext context;
    public SignOffLabourWeekHandler(JpmsContext context) { this.context = context; }

    public Task<LabourWeekSignOff> HandleAsync(SignOffLabourWeek command, CancellationToken cancellationToken) =>
        HandleAsync(command, signedOffByEmail: "", cancellationToken);

    public async Task<LabourWeekSignOff> HandleAsync(SignOffLabourWeek command, string signedOffByEmail, CancellationToken cancellationToken)
    {
        var weekStart = new DateTimeOffset(ForecastRules.WeekStartOf(command.WeekStart.UtcDateTime), TimeSpan.Zero);
        var weekEnd = weekStart.AddDays(7);
        var today = SiteClock.Today().UtcDateTime.Date;

        var sheets = await context.Timesheets
            .Where(sheet => sheet.WorkerId == command.WorkerId && sheet.WorkedOn >= weekStart && sheet.WorkedOn < weekEnd)
            .ToListAsync(cancellationToken);
        var absences = await context.WorkerAbsences
            .Where(row => row.WorkerId == command.WorkerId && row.Date >= weekStart && row.Date < weekEnd)
            .ToListAsync(cancellationToken);

        var settledDays = sheets
            .Where(sheet => sheet.Status is (int)TimesheetStatus.Approved or (int)TimesheetStatus.Rejected)
            .Select(sheet => sheet.WorkedOn.UtcDateTime.Date).ToHashSet();
        var absenceDays = absences.Select(row => row.Date.UtcDateTime.Date).ToHashSet();

        if (!ForecastRules.WeekIsSignable(weekStart.UtcDateTime.Date, today, settledDays, absenceDays))
            throw new WeekNotSignableException(
                "This week has elapsed days with nothing approved, rejected or recorded as absence — deal with those first.");

        var existing = await context.LabourWeekSignOffs
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId && row.WeekStart == weekStart, cancellationToken);
        var entity = existing ?? new LabourWeekSignOffEntity
        {
            LabourWeekSignOffId = LabourIdentifierFactory.NextLabourWeekSignOffId(),
            WorkerId = command.WorkerId,
            WeekStart = weekStart,
        };
        entity.SignedOffByEmail = signedOffByEmail;
        entity.SignedOffAt = DateTimeOffset.UtcNow;
        if (existing is null) context.LabourWeekSignOffs.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new LabourWeekSignOff(entity.WorkerId, entity.WeekStart, entity.SignedOffByEmail, entity.SignedOffAt);
    }
}

public sealed class RemoveLabourWeekSignOffEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement> handler;
    public RemoveLabourWeekSignOffEndpoint(SignedInUserResolver users, ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RemoveLabourWeekSignOff))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/weeks/remove-sign-off")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ApproveTimesheets.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<RemoveLabourWeekSignOff>();
        if (command is null) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class RemoveLabourWeekSignOffHandler : ICommandHandler<RemoveLabourWeekSignOff, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveLabourWeekSignOffHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveLabourWeekSignOff command, CancellationToken cancellationToken)
    {
        var weekStart = new DateTimeOffset(ForecastRules.WeekStartOf(command.WeekStart.UtcDateTime), TimeSpan.Zero);
        var existing = await context.LabourWeekSignOffs
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId && row.WeekStart == weekStart, cancellationToken);
        if (existing is not null)
        {
            context.LabourWeekSignOffs.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.WorkerId);
    }
}
