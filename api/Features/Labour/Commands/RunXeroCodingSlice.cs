using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// The §6a automation (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md): for each fully
/// signed-off worker-month, write the settlement schedule's coding into Xero — recode the covered
/// draft bill, or stage a draft bill where none has arrived. Everything lands DRAFT; approval in
/// Xero stays human. Mapping gaps, unsigned weeks and open variances skip-and-report — the run
/// never guesses a code and never writes from unsigned data. Every outcome is recorded.
/// </summary>
public sealed class RunXeroCodingEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RunXeroCodingHandler handler;
    public RunXeroCodingEndpoint(SignedInUserResolver users, RunXeroCodingHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(RunXeroCoding))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/xero-coding/run")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<RunXeroCoding>();
        if (command is null) return new BadRequestResult();
        if (command.Year < 2020 || command.Year > 2100 || command.Month < 1 || command.Month > 12)
            return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(command, signedInUser.Email, request.HttpContext.RequestAborted));
    }
}

public sealed class RunXeroCodingHandler : ICommandHandler<RunXeroCoding, IReadOnlyList<XeroCodingRunResult>>
{
    private readonly JpmsContext context;
    private readonly SettlementScheduleBuilder builder;
    private readonly IXeroClient xero;

    public RunXeroCodingHandler(JpmsContext context, SettlementScheduleBuilder builder, IXeroClient xero)
    { this.context = context; this.builder = builder; this.xero = xero; }

    public Task<IReadOnlyList<XeroCodingRunResult>> HandleAsync(RunXeroCoding command, CancellationToken cancellationToken) =>
        HandleAsync(command, runByEmail: "", cancellationToken);

    public async Task<IReadOnlyList<XeroCodingRunResult>> HandleAsync(RunXeroCoding command, string runByEmail, CancellationToken cancellationToken)
    {
        var monthStart = new DateTimeOffset(new DateTime(command.Year, command.Month, 1), TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);
        var snapshot = await builder.BuildAsync(command.Year, command.Month, cancellationToken);

        // The rows effective for THIS month, oldest first — FindSiteMapping/FindCodeMapping take
        // the last (latest-effective) match, so a mid-month re-map codes the month the new way.
        siteMappingsCache = await context.SiteXeroMappings
            .Where(row => row.EffectiveFrom < monthEnd && (row.EffectiveTo == null || row.EffectiveTo >= monthStart))
            .OrderBy(row => row.EffectiveFrom).ToListAsync(cancellationToken);
        codeMappingsCache = await context.CostCodeXeroMappings
            .Where(row => row.EffectiveFrom < monthEnd && (row.EffectiveTo == null || row.EffectiveTo >= monthStart))
            .OrderBy(row => row.EffectiveFrom).ToListAsync(cancellationToken);
        var covers = await context.XeroLineTimesheetCovers
            .Where(cover => cover.PeriodStart < monthEnd && cover.PeriodEnd > monthStart)
            .ToListAsync(cancellationToken);
        var coveredLineIds = covers.Select(cover => cover.XeroLedgerLineId).Distinct().ToList();
        var coveredLines = coveredLineIds.Count == 0
            ? new List<XeroLedgerLineEntity>()
            : await context.XeroLedgerLines.Where(line => coveredLineIds.Contains(line.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
        var coversBySub = covers.ToLookup(cover => cover.SubcontractorId);
        var linesById = coveredLines.ToDictionary(line => line.XeroLedgerLineId);

        var results = new List<XeroCodingRunResult>();
        var wanted = command.WorkerIds is { Count: > 0 } ? command.WorkerIds.ToHashSet() : null;

        foreach (var schedule in snapshot.Workers)
        {
            if (wanted is not null && !wanted.Contains(schedule.WorkerId)) continue;
            if (schedule.Verdict == ScheduleVerdict.Nothing) continue;

            var result = await CodeWorkerMonthAsync(schedule, monthStart, coversBySub, linesById, cancellationToken);
            results.Add(result);

            context.XeroCodingRuns.Add(new XeroCodingRunEntity
            {
                XeroCodingRunId = LabourIdentifierFactory.NextXeroCodingRunId(),
                WorkerId = schedule.WorkerId,
                Month = monthStart,
                Outcome = (int)result.Outcome,
                XeroBillId = result.XeroBillId,
                Detail = result.Detail.Length > 2000 ? result.Detail[..2000] : result.Detail,
                RunByEmail = runByEmail,
                RunAt = DateTimeOffset.UtcNow,
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        return results;
    }

    private async Task<XeroCodingRunResult> CodeWorkerMonthAsync(
        WorkerSettlementSchedule schedule, DateTimeOffset monthStart,
        ILookup<string, XeroLineTimesheetCoverEntity> coversBySub,
        IReadOnlyDictionary<string, XeroLedgerLineEntity> linesById,
        CancellationToken cancellationToken)
    {
        XeroCodingRunResult Skip(string why) =>
            new(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Skipped, why, "");

        // Gate 1: sign-off is the only trigger. Nothing reaches Xero from unsigned data.
        if (!schedule.FullySignedOff)
            return Skip("Not every week with approved time is signed off — sign the month off first.");

        // Gate 2: run-once. A worker-month the automation has already written stays written;
        // re-running after a schedule change is a deliberate human decision, taken by clearing
        // the variance first, not something the run re-does silently.
        if (schedule.LastCodingOutcome is nameof(XeroCodingOutcome.BillRecoded) or nameof(XeroCodingOutcome.DraftStaged))
            return Skip($"Already coded ({schedule.LastCodingOutcome}, {schedule.LastCodedAt:dd MMM HH:mm}).");

        // Gate 3: the mapping must answer for every line — a gap skips, it never guesses.
        var gaps = new List<string>();
        var xeroLines = new List<XeroScheduleLine>();
        foreach (var line in schedule.Lines)
        {
            var site = FindSiteMapping(line.ProjectId);
            if (site is null) { gaps.Add($"site \"{line.ProjectName}\" has no Xero tracking option mapped"); continue; }
            var code = FindCodeMapping(line.CostCode);
            if (code is null) { gaps.Add($"cost code {line.CostCode} has no Xero mapping"); continue; }
            var account = line.Nature switch
            {
                SettlementLineNature.CisLabour => code.LabourAccountCode,
                SettlementLineNature.CisMaterials => code.MaterialsAccountCode,
                _ => code.TravelAccountCode,
            };
            if (string.IsNullOrWhiteSpace(account))
            { gaps.Add($"cost code {line.CostCode} has no {line.Nature} account code"); continue; }

            xeroLines.Add(new XeroScheduleLine(
                $"{schedule.WorkerName} — {line.ProjectName} [{line.CostCode}] {NatureLabel(line.Nature)} {monthStart:MMM yyyy}",
                line.Amount, account, site.XeroTrackingOptionName,
                string.IsNullOrWhiteSpace(code.XeroTrackingOptionName) ? line.CostCode : code.XeroTrackingOptionName));
        }
        if (gaps.Count > 0)
            return Skip("Mapping gaps: " + string.Join("; ", gaps.Distinct()) + ". Fix the Xero mapping and re-run.");
        if (xeroLines.Count == 0)
            return Skip("Nothing to code — the schedule has no lines.");

        // A covered bill exists → recode it, if (and only if) the totals already agree.
        var billIds = schedule.SubcontractorId is null
            ? new List<string>()
            : coversBySub[schedule.SubcontractorId]
                .Select(cover => linesById.TryGetValue(cover.XeroLedgerLineId, out var line) ? line.XeroInvoiceId : null)
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

        if (billIds.Count > 1)
            return Skip($"{billIds.Count} different bills are marked as covering this month — resolve that on the settlement view first.");

        if (billIds.Count == 1)
        {
            if (Math.Abs(schedule.Difference) >= 0.01m)
                return Skip($"The covered bill differs from the schedule by £{schedule.Difference:N2} — resolve the variance before coding.");

            var recode = await xero.RecodeDraftBillAsync(new XeroDraftCodingRequest(billIds[0], xeroLines), cancellationToken);
            return recode.Succeeded
                ? new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.BillRecoded,
                    $"Bill recoded to {xeroLines.Count} schedule line(s); left {recode.FreshStatus} in Xero.", billIds[0])
                : new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Failed,
                    recode.Error ?? "Xero refused the recode.", billIds[0]);
        }

        // No bill yet → stage a draft matching the schedule.
        if (string.IsNullOrWhiteSpace(schedule.SubcontractorName))
            return Skip("The worker has no settlement identity — link a subcontractor company or flag "
                + "them a sole trader (Workers page, or the allocation page's inline fix) so the draft "
                + "bill has a contact.");

        var monthEndDate = monthStart.AddMonths(1).AddDays(-1).UtcDateTime.Date;
        var create = await xero.CreateDraftBillAsync(new XeroDraftBillRequest(
            schedule.SubcontractorName, monthEndDate, monthEndDate.AddDays(30),
            $"JPMS labour {monthStart:MMM yyyy} — {schedule.WorkerName}", xeroLines), cancellationToken);
        return create.Succeeded
            ? new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.DraftStaged,
                $"Draft bill staged with {xeroLines.Count} line(s), gross £{schedule.GrossTotal:N2} — reconcile when the real invoice lands.",
                create.FreshStatus ?? "")
            : new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Failed,
                create.Error ?? "Xero refused the draft bill.", "");
    }

    private List<SiteXeroMappingEntity> siteMappingsCache = new();
    private List<CostCodeXeroMappingEntity> codeMappingsCache = new();

    private SiteXeroMappingEntity? FindSiteMapping(string projectId) =>
        siteMappingsCache.LastOrDefault(row => row.ProjectId == projectId);

    private CostCodeXeroMappingEntity? FindCodeMapping(string costCode) =>
        codeMappingsCache.LastOrDefault(row => string.Equals(row.CostCode, costCode, StringComparison.OrdinalIgnoreCase));

    private static string NatureLabel(SettlementLineNature nature) => nature switch
    {
        SettlementLineNature.CisLabour => "labour",
        SettlementLineNature.CisMaterials => "materials",
        _ => "travel",
    };
}
