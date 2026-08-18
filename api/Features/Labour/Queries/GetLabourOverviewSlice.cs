using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

public sealed class GetLabourOverviewEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetLabourOverview, LabourOverviewSnapshot> handler;
    public GetLabourOverviewEndpoint(SignedInUserResolver users, IQueryHandler<GetLabourOverview, LabourOverviewSnapshot> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetLabourOverview))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "labour/overview/{year:int}/{month:int}")] HttpRequest request,
        int year, int month)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        // Rates and £ ride on every row, so the whole overview is gated to the managing roles.
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (year < 2020 || year > 2100 || month < 1 || month > 12) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(new GetLabourOverview(year, month), request.HttpContext.RequestAborted));
    }
}

/// <summary>
/// Builds the whole month in one pass: per-worker forecast + placement grid, per-site and
/// per-cost-code recorded totals, the chase list and the confidence segments. Recorded cost for
/// unapproved rows is valued at the worker's current rate (pending view); approved rows use their
/// snapshotted CostAmount — same split the Financials tab draws.
/// </summary>
public sealed class GetLabourOverviewHandler : IQueryHandler<GetLabourOverview, LabourOverviewSnapshot>
{
    private readonly JpmsContext context;
    public GetLabourOverviewHandler(JpmsContext context) { this.context = context; }

    public async Task<LabourOverviewSnapshot> HandleAsync(GetLabourOverview query, CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(new DateTime(query.Year, query.Month, 1), TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);
        var today = SiteClock.Today().UtcDateTime.Date;

        var workers = await context.Workers.Where(worker => worker.IsActive)
            .OrderBy(worker => worker.Name).ToListAsync(cancellationToken);
        var workerIds = workers.Select(worker => worker.WorkerId).ToList();

        var timesheets = await context.Timesheets
            .Where(sheet => sheet.WorkedOn >= monthStart && sheet.WorkedOn < monthEnd && sheet.WorkerId != "")
            .ToListAsync(cancellationToken);
        var absences = await context.WorkerAbsences
            .Where(absence => absence.Date >= monthStart && absence.Date < monthEnd)
            .ToListAsync(cancellationToken);
        var openAttendance = await context.SiteAttendances
            .Where(row => row.WorkDate >= monthStart && row.WorkDate < monthEnd && row.SignedOutAt == null)
            .ToListAsync(cancellationToken);
        var signOffs = await context.LabourWeekSignOffs
            .Where(row => row.WeekStart >= monthStart.AddDays(-6) && row.WeekStart < monthEnd)
            .ToListAsync(cancellationToken);
        var contracts = await context.WorkerContracts
            .Where(row => row.EffectiveFrom < monthEnd).OrderBy(row => row.EffectiveFrom)
            .ToListAsync(cancellationToken);
        var cisStatuses = await context.WorkerCisStatuses
            .Where(row => row.EffectiveFrom < monthEnd).OrderBy(row => row.EffectiveFrom)
            .ToListAsync(cancellationToken);
        var projectNames = await context.Projects
            .ToDictionaryAsync(project => project.ProjectId, project => project.Name, cancellationToken);
        var costCentreNames = await context.CostCenters
            .ToDictionaryAsync(centre => centre.Code, centre => centre.Name, cancellationToken);

        var timesheetsByWorker = timesheets.ToLookup(sheet => sheet.WorkerId);
        var absencesByWorker = absences.ToLookup(absence => absence.WorkerId);
        var signOffsByWorker = signOffs.ToLookup(row => row.WorkerId);
        var openByWorker = openAttendance.ToLookup(row => row.WorkerId);

        var workerRows = new List<LabourOverviewWorker>();
        var chase = new List<LabourChaseItem>();
        var weekBuckets = new Dictionary<DateTime, (int Elapsed, int Confirmed, decimal Unconfirmed)>();
        decimal projectedSpend = 0m, timeOffCost = 0m, amountDueTotal = 0m, unconfirmedCost = 0m;
        int elapsedWorkerDays = 0, confirmedWorkerDays = 0;

        foreach (var worker in workers)
        {
            var dayRate = worker.HourlyRate * ForecastRules.StandardHoursPerDay;
            var contractedDays = contracts.Where(row => row.WorkerId == worker.WorkerId && row.EffectiveFrom < monthEnd)
                .OrderBy(row => row.EffectiveFrom).LastOrDefault()?.ContractedDaysPerMonth ?? 0m;
            var cisRate = cisStatuses.Where(row => row.WorkerId == worker.WorkerId && row.EffectiveFrom < monthEnd)
                .OrderBy(row => row.EffectiveFrom).LastOrDefault()?.CisRatePercent ?? 20m;

            var workerSheets = timesheetsByWorker[worker.WorkerId].ToList();
            var workerAbsences = absencesByWorker[worker.WorkerId].ToList();

            var days = new List<LabourOverviewDay>();
            foreach (var sheet in workerSheets)
            {
                projectNames.TryGetValue(sheet.ProjectId, out var projectName);
                days.Add(new LabourOverviewDay(sheet.WorkedOn, sheet.ProjectId, projectName ?? sheet.ProjectId,
                    sheet.Hours, (TimesheetStatus)sheet.Status, null));
            }
            foreach (var absence in workerAbsences)
                days.Add(new LabourOverviewDay(absence.Date, "", "", 0m, null, (AbsenceKind)absence.Kind));

            var absenceKinds = workerAbsences.Select(absence => (AbsenceKind)absence.Kind).ToList();
            var projected = ForecastRules.ProjectedCost(contractedDays, dayRate, absenceKinds);
            var timeOff = ForecastRules.TimeOffCost(dayRate, absenceKinds);
            var due = ForecastRules.AmountDue(projected, cisRate);

            var sheetDates = workerSheets.Select(sheet => sheet.WorkedOn.UtcDateTime.Date).ToHashSet();
            var absenceDates = workerAbsences.Select(absence => absence.Date.UtcDateTime.Date).ToHashSet();
            var openDates = openByWorker[worker.WorkerId].Select(row => row.WorkDate.UtcDateTime.Date).ToHashSet();

            // Elapsed weekdays this month: confirmed = timesheet or absence recorded. Unconfirmed
            // days stay in the projection at the full day rate — the confidence bar carries that.
            var first = monthStart.UtcDateTime.Date;
            var lastElapsed = today < monthEnd.UtcDateTime.Date.AddDays(-1) ? today : monthEnd.UtcDateTime.Date.AddDays(-1);
            for (var date = first; date <= lastElapsed; date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                var weekStart = ForecastRules.WeekStartOf(date);
                var bucket = weekBuckets.TryGetValue(weekStart, out var existing) ? existing : (0, 0, 0m);
                bucket.Elapsed++;
                elapsedWorkerDays++;
                var confirmed = sheetDates.Contains(date) || absenceDates.Contains(date);
                if (confirmed) { bucket.Confirmed++; confirmedWorkerDays++; }
                else
                {
                    bucket.Unconfirmed += dayRate;
                    unconfirmedCost += dayRate;
                    if (openDates.Contains(date))
                    {
                        var open = openByWorker[worker.WorkerId].First(row => row.WorkDate.UtcDateTime.Date == date);
                        projectNames.TryGetValue(open.ProjectId, out var openProjectName);
                        chase.Add(new LabourChaseItem(worker.WorkerId, worker.Name, new DateTimeOffset(date, TimeSpan.Zero),
                            LabourChaseReason.OpenAttendance, open.ProjectId, openProjectName ?? open.ProjectId));
                    }
                    else
                    {
                        chase.Add(new LabourChaseItem(worker.WorkerId, worker.Name, new DateTimeOffset(date, TimeSpan.Zero),
                            LabourChaseReason.NoTimesheet, "", ""));
                    }
                }
                weekBuckets[weekStart] = bucket;
            }

            var workerSignOffs = signOffsByWorker[worker.WorkerId]
                .Select(row => new LabourWeekSignOff(row.WorkerId, row.WeekStart, row.SignedOffByEmail, row.SignedOffAt))
                .OrderBy(row => row.WeekStart).ToList();

            projectedSpend += projected;
            timeOffCost += timeOff;
            amountDueTotal += due;

            workerRows.Add(new LabourOverviewWorker(worker.WorkerId, worker.Name, dayRate, contractedDays, cisRate,
                ForecastRules.HoursToDays(workerSheets.Sum(sheet => sheet.Hours)),
                workerAbsences.Sum(absence => ForecastRules.AbsenceDeductionDays((AbsenceKind)absence.Kind)),
                projected, due, days.OrderBy(day => day.Date).ToList(), workerSignOffs));
        }

        // Recorded £ per site / cost code: approved rows carry their snapshot; submitted rows are
        // valued at the worker's current rate so the view is live without touching actuals.
        var ratesByWorker = workers.ToDictionary(worker => worker.WorkerId, worker => worker.HourlyRate);
        decimal RecordedCost(Data.Entities.TimesheetEntity sheet) =>
            sheet.Status == (int)TimesheetStatus.Approved
                ? sheet.CostAmount
                : decimal.Round(sheet.Hours * (ratesByWorker.TryGetValue(sheet.WorkerId, out var rate) ? rate : 0m), 2);

        var sites = timesheets.GroupBy(sheet => sheet.ProjectId)
            .Select(group => new LabourOverviewSite(group.Key,
                projectNames.TryGetValue(group.Key, out var name) ? name : group.Key,
                ForecastRules.HoursToDays(group.Sum(sheet => sheet.Hours)),
                group.Sum(RecordedCost)))
            .OrderByDescending(site => site.CostRecorded).ToList();

        var costCodes = timesheets.GroupBy(sheet => sheet.CostCode)
            .Select(group => new LabourOverviewCostCode(group.Key,
                costCentreNames.TryGetValue(group.Key, out var trade) ? trade : "",
                ForecastRules.HoursToDays(group.Sum(sheet => sheet.Hours)),
                group.Sum(RecordedCost)))
            .OrderByDescending(code => code.CostRecorded).ToList();

        var weeks = weekBuckets.OrderBy(pair => pair.Key)
            .Select(pair => new LabourWeekConfidence(new DateTimeOffset(pair.Key, TimeSpan.Zero),
                pair.Value.Elapsed, pair.Value.Confirmed, pair.Value.Unconfirmed))
            .ToList();

        var totals = new LabourOverviewTotals(projectedSpend, timeOffCost, amountDueTotal,
            elapsedWorkerDays, confirmedWorkerDays, unconfirmedCost, weeks);

        return new LabourOverviewSnapshot(query.Year, query.Month, totals, workerRows, sites, costCodes,
            chase.OrderBy(item => item.Date).ThenBy(item => item.WorkerName).ToList());
    }
}
