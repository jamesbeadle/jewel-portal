using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// The weekly sign-off marker (scope §4). Sign-off is a view over approval, never a second state
// machine: the server re-checks ForecastRules.WeekPartIsSignable at the moment of signing, and
// removing the marker touches no timesheet. A marker belongs to one MONTH's part of a week
// (2026-09-02): a week inside one month has one marker; a week that straddles a month end has
// one per month, so "to 31 Aug" signs off on the 1st with only its August days confirmed and the
// September days never hold August's settlement up.

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
        var (weekStart, monthStart) = LabourWeekParts.Resolve(command.WeekStart, command.MonthStart);
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

        if (!ForecastRules.WeekPartIsSignable(weekStart.UtcDateTime.Date, monthStart.UtcDateTime.Date, today, settledDays, absenceDays))
            throw new WeekNotSignableException(
                $"{LabourWeekParts.Describe(weekStart, monthStart)} has elapsed days with nothing approved, rejected or recorded as absence — deal with those first.");

        var existing = await context.LabourWeekSignOffs
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId && row.WeekStart == weekStart && row.MonthStart == monthStart, cancellationToken);
        var entity = existing ?? new LabourWeekSignOffEntity
        {
            LabourWeekSignOffId = LabourIdentifierFactory.NextLabourWeekSignOffId(),
            WorkerId = command.WorkerId,
            WeekStart = weekStart,
            MonthStart = monthStart,
        };
        entity.SignedOffByEmail = signedOffByEmail;
        entity.SignedOffAt = DateTimeOffset.UtcNow;
        if (existing is null) context.LabourWeekSignOffs.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new LabourWeekSignOff(entity.WorkerId, entity.WeekStart, entity.SignedOffByEmail, entity.SignedOffAt, entity.MonthStart);
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
        var (weekStart, monthStart) = LabourWeekParts.Resolve(command.WeekStart, command.MonthStart);
        var existing = await context.LabourWeekSignOffs
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId && row.WeekStart == weekStart && row.MonthStart == monthStart, cancellationToken);
        if (existing is not null)
        {
            context.LabourWeekSignOffs.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.WorkerId);
    }
}

/// <summary>
/// How a sign-off command names its week part: the week is any date in it, normalised to the
/// Monday; the month is the given MonthStart normalised to the 1st, or — left out — the month
/// of the WeekStart date AS GIVEN (so 31 Aug means August's part of that week and 1 Sep means
/// September's). Both normalise to midnight UTC, the way every labour date is stored.
/// </summary>
public static class LabourWeekParts
{
    public static (DateTimeOffset WeekStart, DateTimeOffset MonthStart) Resolve(DateTimeOffset weekStart, DateTimeOffset? monthStart)
    {
        var week = ForecastRules.WeekStartOf(weekStart.UtcDateTime);
        var month = ForecastRules.MonthStartOf((monthStart ?? weekStart).UtcDateTime.Date);
        if (!ForecastRules.WeekTouchesMonth(week, month))
            throw new InvalidOperationException(
                $"The week of {week:dd MMM yyyy} has no days in {month:MMMM yyyy} — name the month whose part of the week you mean.");
        return (new DateTimeOffset(week, TimeSpan.Zero), new DateTimeOffset(month, TimeSpan.Zero));
    }

    /// <summary>"The week of 31 Aug" for a week inside one month; "August's part of the week of
    /// 31 Aug (31 Aug)" / "September's part of the week of 31 Aug (1–6 Sep)" when it straddles.</summary>
    public static string Describe(DateTimeOffset weekStart, DateTimeOffset monthStart)
    {
        var week = weekStart.UtcDateTime.Date;
        var month = monthStart.UtcDateTime.Date;
        if (!ForecastRules.WeekStraddlesMonthEnd(week))
            return $"The week of {week:dd MMM}";
        var (first, last) = ForecastRules.WeekPart(week, month);
        // "%d": a lone "d" would be the short-date pattern, not the day number.
        var days = first == last ? $"{first:%d} {first:MMM}" : $"{first:%d}–{last:%d} {last:MMM}";
        return $"{month:MMMM}'s part of the week of {week:dd MMM} ({days})";
    }
}
