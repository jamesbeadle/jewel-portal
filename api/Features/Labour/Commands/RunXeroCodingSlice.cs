using System.Globalization;
using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>
/// The §6a automation (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md): for each fully
/// signed-off worker-month, write the settlement schedule's coding into Xero.
///
/// Rebuilt 2026-09-03 on the accountant's "the coding run must settle a worker who already has a
/// bill": the cover route (worker invoices → Dext lands it → accountant authorises → marks it as
/// settlement) is the sole trader's NORMAL path, so the run's default is to FIND the worker's
/// existing bill — covered, or recognised by contact + period, draft or AUTHORISED — and recode
/// that bill's lines to the schedule's split, keeping its total, VAT treatment, status and cover.
/// Staging a draft is the exception, for the worker whose invoice hasn't arrived. A bill that
/// cannot be recoded (paid, credited, voided) skips with its status named; a second bill is
/// never staged beside one — a silent duplicate is the worst outcome because it looks like it
/// worked. Mapping gaps and unsigned weeks skip-and-report; the run never guesses a code and
/// never writes from unsigned data. Every outcome is recorded; a dry run records nothing.
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

/// <summary>
/// Resets a worker-month's coding outcome (item D, 2026-09-03). The run-once gate reads the
/// LATEST recorded outcome, so a worker-month whose staged bill was deleted by hand sat behind
/// DraftStaged for ever. A reset APPENDS a Reset outcome — who, why, what it was — so the history
/// reads staged → reset → recoded and nothing is erased. Touches nothing in Xero.
/// </summary>
public sealed class ResetXeroCodingOutcomeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ResetXeroCodingOutcomeHandler handler;
    public ResetXeroCodingOutcomeEndpoint(SignedInUserResolver users, ResetXeroCodingOutcomeHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ResetXeroCodingOutcome))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/xero-coding/reset")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageSettlement.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var command = await request.ReadFromJsonAsync<ResetXeroCodingOutcome>();
        if (command is null || string.IsNullOrWhiteSpace(command.WorkerId)) return new BadRequestResult();
        if (command.Year < 2020 || command.Year > 2100 || command.Month < 1 || command.Month > 12)
            return new BadRequestResult();
        if (string.IsNullOrWhiteSpace(command.Reason))
            return new BadRequestObjectResult(new[] { "Say why the coding outcome is being reset — the reason is recorded against the worker-month." });
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

public sealed class ResetXeroCodingOutcomeHandler : ICommandHandler<ResetXeroCodingOutcome, Acknowledgement>
{
    private readonly JpmsContext context;
    public ResetXeroCodingOutcomeHandler(JpmsContext context) { this.context = context; }

    public Task<Acknowledgement> HandleAsync(ResetXeroCodingOutcome command, CancellationToken cancellationToken) =>
        HandleAsync(command, resetByEmail: "", cancellationToken);

    public async Task<Acknowledgement> HandleAsync(ResetXeroCodingOutcome command, string resetByEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new InvalidOperationException("Say why the coding outcome is being reset — the reason is recorded against the worker-month.");

        var monthStart = new DateTimeOffset(new DateTime(command.Year, command.Month, 1), TimeSpan.Zero);
        var worker = await context.Workers.AsNoTracking()
            .FirstOrDefaultAsync(row => row.WorkerId == command.WorkerId, cancellationToken)
            ?? throw new InvalidOperationException("No such worker.");

        var latest = await context.XeroCodingRuns.AsNoTracking()
            .Where(run => run.WorkerId == command.WorkerId && run.Month == monthStart)
            .OrderByDescending(run => run.RunAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is null)
            throw new InvalidOperationException(
                $"{worker.Name}'s {monthStart:MMM yyyy} has no coding outcome to reset — the run has never written it, so it will run as it is.");
        var previous = (XeroCodingOutcome)latest.Outcome;
        if (previous is not (XeroCodingOutcome.BillRecoded or XeroCodingOutcome.DraftStaged))
            throw new InvalidOperationException(
                $"{worker.Name}'s {monthStart:MMM yyyy} reads {previous} ({latest.RunAt:dd MMM HH:mm}) — that does not block the run, so there is nothing to reset.");

        var detail = $"Reset by {(string.IsNullOrWhiteSpace(resetByEmail) ? "the portal" : resetByEmail)}: {command.Reason.Trim()} "
            + $"(was {previous} at {latest.RunAt:dd MMM yyyy HH:mm}"
            + (string.IsNullOrEmpty(latest.XeroBillId) ? ")" : $", bill {latest.XeroBillId})")
            + " — the next run takes this month again.";
        var entity = new XeroCodingRunEntity
        {
            XeroCodingRunId = LabourIdentifierFactory.NextXeroCodingRunId(),
            WorkerId = command.WorkerId,
            Month = monthStart,
            Outcome = (int)XeroCodingOutcome.Reset,
            XeroBillId = latest.XeroBillId,
            Detail = detail.Length > 2000 ? detail[..2000] : detail,
            RunByEmail = resetByEmail,
            RunAt = DateTimeOffset.UtcNow,
        };
        context.XeroCodingRuns.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(entity.XeroCodingRunId);
    }
}

public sealed class RunXeroCodingHandler : ICommandHandler<RunXeroCoding, IReadOnlyList<XeroCodingRunResult>>
{
    private readonly JpmsContext context;
    private readonly SettlementScheduleBuilder builder;
    private readonly IXeroClient xero;
    private readonly XeroOptions xeroOptions;

    public RunXeroCodingHandler(JpmsContext context, SettlementScheduleBuilder builder, IXeroClient xero, XeroOptions xeroOptions)
    { this.context = context; this.builder = builder; this.xero = xero; this.xeroOptions = xeroOptions; }

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

        // Covers are read TRACKED: a recode re-points them (removes the old rows, adds new ones)
        // in the same SaveChanges as the run's own record.
        var covers = await context.XeroLineTimesheetCovers
            .Where(cover => cover.PeriodStart < monthEnd && cover.PeriodEnd > monthStart)
            .ToListAsync(cancellationToken);
        var coveredLineIds = covers.Select(cover => cover.XeroLedgerLineId).Distinct().ToList();
        var coveredLines = coveredLineIds.Count == 0
            ? new List<XeroLedgerLineEntity>()
            : await context.XeroLedgerLines.AsNoTracking()
                .Where(line => coveredLineIds.Contains(line.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
        var coversBySub = covers.ToLookup(cover => cover.SubcontractorId);
        var coveredLinesById = coveredLines.ToDictionary(line => line.XeroLedgerLineId);

        // The bills that could be a worker's for this month — item A's "match on contact +
        // period": every ACCPAY line dated near the month (the period is taken from the bill's
        // own stated month where Dext/the invoice number carries one, else a window either side
        // of month end), recognised per worker below by the ONE name rule the allocation page
        // already uses. Read wide once, filtered in memory per worker.
        var windowStart = monthStart.AddDays(-45).UtcDateTime;
        var windowEnd = monthEnd.AddDays(45).UtcDateTime;
        var nearbyLines = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.Type == "ACCPAY" && line.Date != null && line.Date >= windowStart && line.Date < windowEnd)
            .ToListAsync(cancellationToken);

        // The contact name Xero already holds for each supplier (latest bill wins) — a staged
        // draft goes to THAT contact rather than to a near-miss spelling that would create a
        // second contact.
        var contactsByLastBill = await context.XeroLedgerLines.AsNoTracking()
            .Where(line => line.Type == "ACCPAY" && line.ContactName != null && line.ContactName != "")
            .GroupBy(line => line.ContactName!)
            .Select(group => new { Name = group.Key, Last = group.Max(line => line.Date) })
            .ToListAsync(cancellationToken);
        knownContacts = contactsByLastBill.OrderByDescending(row => row.Last).Select(row => row.Name).ToList();

        var latestRuns = (await context.XeroCodingRuns.AsNoTracking()
                .Where(run => run.Month == monthStart).OrderBy(run => run.RunAt).ToListAsync(cancellationToken))
            .GroupBy(run => run.WorkerId).ToDictionary(group => group.Key, group => group.Last());

        var results = new List<XeroCodingRunResult>();
        var wanted = command.WorkerIds is { Count: > 0 } ? command.WorkerIds.ToHashSet() : null;

        foreach (var schedule in snapshot.Workers)
        {
            if (wanted is not null && !wanted.Contains(schedule.WorkerId)) continue;
            if (schedule.Verdict == ScheduleVerdict.Nothing) continue;

            var result = await CodeWorkerMonthAsync(
                new WorkerRun(schedule, monthStart, monthEnd, coversBySub, coveredLinesById, nearbyLines,
                    latestRuns.TryGetValue(schedule.WorkerId, out var latest) ? latest : null, runByEmail, command.DryRun),
                cancellationToken);
            results.Add(result);

            if (command.DryRun) continue;
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
        // A dry run has changed nothing tracked; saving is harmless but pointless.
        if (!command.DryRun) await context.SaveChangesAsync(cancellationToken);
        return results;
    }

    private sealed record WorkerRun(
        WorkerSettlementSchedule Schedule,
        DateTimeOffset MonthStart,
        DateTimeOffset MonthEnd,
        ILookup<string, XeroLineTimesheetCoverEntity> CoversBySub,
        IReadOnlyDictionary<string, XeroLedgerLineEntity> CoveredLinesById,
        IReadOnlyList<XeroLedgerLineEntity> NearbyLines,
        XeroCodingRunEntity? LatestRun,
        string RunByEmail,
        bool DryRun);

    private async Task<XeroCodingRunResult> CodeWorkerMonthAsync(WorkerRun run, CancellationToken cancellationToken)
    {
        var schedule = run.Schedule;
        var monthStart = run.MonthStart;
        XeroCodingRunResult Skip(string why) =>
            new(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Skipped, why, "");
        XeroCodingRunResult Failed(string why, string billId) =>
            new(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Failed, why, billId);

        // Gate 1: sign-off is the only trigger. Nothing reaches Xero from unsigned data.
        if (!schedule.FullySignedOff)
            return Skip("Not every week with approved time is signed off — sign the month off first.");

        // Gate 2: run-once. A worker-month the automation has already written stays written —
        // UNLESS the bill it wrote has since been deleted or voided in Xero (item D: a deleted
        // staged draft must not trap the month). Re-running otherwise is a deliberate human
        // decision, taken by resetting the coding outcome (with a reason) first.
        var preface = "";
        if (run.LatestRun is not null
            && (XeroCodingOutcome)run.LatestRun.Outcome is XeroCodingOutcome.BillRecoded or XeroCodingOutcome.DraftStaged)
        {
            var previous = (XeroCodingOutcome)run.LatestRun.Outcome;
            var stamp = $"{previous}, {run.LatestRun.RunAt:dd MMM HH:mm}";
            if (string.IsNullOrEmpty(run.LatestRun.XeroBillId))
                return Skip($"Already coded ({stamp}). Reset the coding outcome to run this month again.");
            XeroBillSummary? written;
            try { written = await xero.GetBillAsync(run.LatestRun.XeroBillId, cancellationToken); }
            catch (XeroCallFailedException failure)
            {
                return Skip($"Already coded ({stamp}) — and Xero couldn't confirm bill {run.LatestRun.XeroBillId} still stands: {failure.Message}");
            }
            if (written is not null && !IsGone(written.Status))
                return Skip($"Already coded ({stamp}): bill {BillLabel(written)} is {written.Status}, £{written.Total:N2}. "
                    + "Reset the coding outcome to run this month again.");
            preface = $"The bill this month was coded to on {run.LatestRun.RunAt:dd MMM HH:mm} ({previous}) is "
                + (written is null ? "no longer in Xero" : written.Status.ToLowerInvariant()) + " — coding again. ";
        }

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

        // Both routes need a settlement counterparty: covers key on it, and a recognised bill is
        // marked as settlement against it.
        if (schedule.SubcontractorId is null || string.IsNullOrWhiteSpace(schedule.SubcontractorName))
            return Skip("The worker has no settlement identity — link a subcontractor company or flag "
                + "them a sole trader (Workers page, or the allocation page's inline fix) so the run "
                + "knows whose bill to look for.");

        // Find the worker's bill for the month (item A). Covered bills first — the accountant's
        // explicit mark — then bills recognised by contact + period that nobody has marked yet.
        var coveredBillIds = run.CoversBySub[schedule.SubcontractorId]
                .Select(cover => run.CoveredLinesById.TryGetValue(cover.XeroLedgerLineId, out var line) ? line.XeroInvoiceId : null)
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();
        if (coveredBillIds.Count > 1)
            return Skip($"{coveredBillIds.Count} different bills are marked as covering this month — resolve that on the settlement view first.");

        var recognised = coveredBillIds.Count == 1
            ? new List<IGrouping<string, XeroLedgerLineEntity>>()
            : RecognisedBills(run);
        if (recognised.Count > 1)
            return Skip($"{recognised.Count} bills in Xero look like {schedule.WorkerName}'s for {monthStart:MMM yyyy}: "
                + string.Join(", ", recognised.Select(bill => $"{LedgerBillLabel(bill.First())} ({bill.First().InvoiceStatus}, £{bill.First().InvoiceTotal:N2})"))
                + ". Mark the right one as settlement on the Cost allocation page's Labour tab, then re-run.");

        var billId = coveredBillIds.Count == 1 ? coveredBillIds[0] : recognised.SingleOrDefault()?.Key;
        var wasCovered = coveredBillIds.Count == 1;

        if (billId is not null)
            return await RecodeExistingBillAsync(run, billId, wasCovered, xeroLines, preface, cancellationToken);

        // No bill anywhere → stage a draft matching the schedule (the exception, item F).
        var contactName = PreferredContactName(schedule);
        var monthEndDate = monthStart.AddMonths(1).AddDays(-1).UtcDateTime.Date;
        var reference = $"JPMS labour {monthStart:MMM yyyy} — {schedule.WorkerName}";
        if (run.DryRun)
            return new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.WouldStageDraft,
                preface + $"No bill from {contactName} for {monthStart:MMM yyyy} in Xero — would stage a DRAFT bill "
                + $"\"{reference}\" dated {monthEndDate:dd MMM yyyy}: {xeroLines.Count} line(s), net £{schedule.GrossTotal:N2} "
                + "(VAT per the contact's default in Xero, never assumed). " + LinesSummary(xeroLines), "");

        var create = await xero.CreateDraftBillAsync(new XeroDraftBillRequest(
            contactName, monthEndDate, monthEndDate.AddDays(30), reference, xeroLines), cancellationToken);
        return create.Succeeded
            ? new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.DraftStaged,
                preface + $"Draft bill \"{reference}\" staged for {contactName} with {xeroLines.Count} line(s), net £{schedule.GrossTotal:N2}. "
                + (create.Note ?? "") + " Reconcile when the real invoice lands.",
                create.FreshStatus ?? "")
            : Failed(preface + (create.Error ?? "Xero refused the draft bill."), "");
    }

    private async Task<XeroCodingRunResult> RecodeExistingBillAsync(
        WorkerRun run, string billId, bool wasCovered, List<XeroScheduleLine> xeroLines, string preface, CancellationToken cancellationToken)
    {
        var schedule = run.Schedule;
        XeroCodingRunResult Skip(string why) =>
            new(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Skipped, why, billId);

        // The bill's stored lines, TRACKED — a recode replaces them with the fresh lines (and moves
        // the cover) in the same SaveChanges as the run record, so the reconciliation the cover
        // holds is never half-done: verdict, covered total and difference read the same before
        // and after (item B).
        var storedLines = await context.XeroLedgerLines
            .Where(line => line.XeroInvoiceId == billId)
            .ToListAsync(cancellationToken);
        var allocated = storedLines.Where(line => line.AllocationStatus == (int)XeroAllocationStatus.Allocated).ToList();
        if (allocated.Count > 0)
            return Skip($"Bill {LedgerBillLabel(storedLines[0])} has {allocated.Count} line(s) already allocated by hand on the Cost "
                + "allocation page — recoding would orphan that allocation. Unallocate them (or leave the bill as coded) and re-run.");

        // Decide on what Xero holds NOW, not on last night's sync.
        XeroBillSummary? bill;
        try { bill = await xero.GetBillAsync(billId, cancellationToken); }
        catch (XeroCallFailedException failure)
        {
            return new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Failed,
                preface + $"Couldn't read bill {billId} from Xero: {failure.Message}", billId);
        }
        if (bill is null)
            return Skip(preface + $"Bill {billId} is marked as {schedule.WorkerName}'s for {run.MonthStart:MMM yyyy} but Xero no longer has it "
                + "— sync from Xero (which clears its cover) and re-run.");
        if (!bill.IsRecodable)
            return Skip(preface + $"Bill {BillLabel(bill)} for {run.MonthStart:MMM yyyy} can't be recoded — {bill.NotRecodableReason}. "
                + "Nothing was written and no second bill was staged; the site and cost-code split stays portal-side "
                + "(the approved timesheets carry it).");

        var difference = decimal.Round(bill.SubTotal - schedule.GrossTotal, 2);
        var totals = $"Bill {BillLabel(bill)}: {bill.Status}, net £{bill.SubTotal:N2}, VAT £{bill.TotalTax:N2} "
            + $"({bill.TaxType ?? "account default"}, {bill.LineAmountTypes}), total £{bill.Total:N2}. Schedule £{schedule.GrossTotal:N2}"
            + (difference == 0m ? " — matches." : $" — differs by £{difference:N2}: the bill's money is split in the schedule's proportions; post a settlement variance for the difference.");

        if (run.DryRun)
            return new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.WouldRecodeBill,
                preface + $"Would recode {(wasCovered ? "the covered bill" : "the bill recognised by contact + period")} to {xeroLines.Count} line(s), "
                + "keeping its status, total and VAT" + (wasCovered ? " and moving the cover onto the new lines. " : " and marking it as settlement of the month. ")
                + totals + " " + LinesSummary(xeroLines), billId);

        var recode = await xero.RecodeBillAsync(new XeroBillCodingRequest(billId, xeroLines), cancellationToken);
        if (!recode.Succeeded)
            return new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.Failed,
                preface + (recode.Error ?? "Xero refused the recode."), billId);

        var moved = await RepointCoverAndLedgerAsync(run, billId, storedLines, bill, recode, cancellationToken);
        var proof = recode.Total == bill.Total && recode.TotalTax == bill.TotalTax
            ? $"Total £{recode.Total:N2} and VAT £{recode.TotalTax:N2} unchanged"
            : $"WARNING — total moved from £{bill.Total:N2} to £{recode.Total:N2} (VAT £{bill.TotalTax:N2} → £{recode.TotalTax:N2}): check the bill in Xero";
        return new XeroCodingRunResult(schedule.WorkerId, schedule.WorkerName, XeroCodingOutcome.BillRecoded,
            preface + $"Recoded bill {BillLabel(bill)} to {recode.Lines.Count} line(s); left {recode.Status} in Xero. {proof}. "
            + (wasCovered ? $"Cover moved onto {moved} line(s). " : $"Marked as settlement of {run.MonthStart:MMM yyyy} ({moved} line(s)). ")
            + totals, billId);
    }

    /// <summary>
    /// Item B: Xero issued new line ids, so the stored ledger lines are replaced with the fresh
    /// ones and every cover on the old lines is re-created on the new labour lines — same
    /// counterparty, same period, same project scope — all in the tracked context, saved with
    /// the run record. Only cost-of-sales lines are stored (the sync's own rule), so a cover
    /// never points at a line the next sync would drop. Returns how many lines now carry cover.
    /// </summary>
    private async Task<int> RepointCoverAndLedgerAsync(
        WorkerRun run, string billId, List<XeroLedgerLineEntity> storedLines, XeroBillSummary before,
        XeroBillRecodeResult recode, CancellationToken cancellationToken)
    {
        var template = storedLines.FirstOrDefault();
        // EVERY cover on the bill's old lines, whatever month or project it was marked under —
        // a cover left pointing at a line Xero no longer has would silently drop the covered
        // total. (Instances already tracked from the month's read come back as the same objects.)
        var storedIds = storedLines.Select(line => line.XeroLedgerLineId).ToList();
        var oldCovers = storedIds.Count == 0
            ? new List<XeroLineTimesheetCoverEntity>()
            : await context.XeroLineTimesheetCovers
                .Where(cover => storedIds.Contains(cover.XeroLedgerLineId))
                .ToListAsync(cancellationToken);
        var coverTemplate = oldCovers
            .OrderByDescending(cover => cover.SubcontractorId == run.Schedule.SubcontractorId)
            .ThenBy(cover => cover.PeriodStart)
            .FirstOrDefault();
        var now = DateTimeOffset.UtcNow;

        foreach (var cover in oldCovers) context.XeroLineTimesheetCovers.Remove(cover);
        foreach (var line in storedLines) context.XeroLedgerLines.Remove(line);

        var covered = 0;
        foreach (var line in recode.Lines)
        {
            if (string.IsNullOrEmpty(line.LineItemId) || !IsCostOfSales(line.AccountCode)) continue;
            var accountName = storedLines.FirstOrDefault(stored => string.Equals(stored.AccountCode, line.AccountCode, StringComparison.OrdinalIgnoreCase))?.AccountName;
            var entity = new XeroLedgerLineEntity
            {
                XeroLedgerLineId = $"{billId}:{line.LineItemId}",
                XeroInvoiceId = billId,
                XeroLineItemId = line.LineItemId,
                Type = template?.Type ?? "ACCPAY",
                InvoiceNumber = before.InvoiceNumber is null ? template?.InvoiceNumber : Truncate(before.InvoiceNumber, 64),
                Reference = before.Reference is null ? template?.Reference : Truncate(before.Reference, 256),
                ContactName = before.ContactName is null ? template?.ContactName : Truncate(before.ContactName, 256),
                Date = before.Date ?? template?.Date,
                InvoiceStatus = Truncate(recode.Status, 32)!,
                Description = Truncate(line.Description, 1024),
                Net = line.LineAmount,
                Tax = line.TaxAmount,
                InvoiceTotal = recode.Total,
                AmountDue = template?.AmountDue ?? before.AmountDue,
                AccountCode = Truncate(line.AccountCode, 32),
                AccountName = accountName,
                XeroSite = Truncate(line.SiteOption, 128),
                XeroCostCode = Truncate(line.CostCodeOption, 128),
                HasAttachments = template?.HasAttachments ?? false,
                AllocationStatus = (int)XeroAllocationStatus.Unallocated,
                FirstSeenAtUtc = template?.FirstSeenAtUtc ?? now,
                LastSyncedAtUtc = now,
            };
            context.XeroLedgerLines.Add(entity);

            // Every recoded line settles the month. A bill that was covered keeps the cover's
            // own scope (project, period, who marked it); a bill found by recognition is marked
            // worker-month scoped — ProjectId "" — the same mark the Labour tab's "Mark as
            // settlement" makes.
            context.XeroLineTimesheetCovers.Add(new XeroLineTimesheetCoverEntity
            {
                XeroLineTimesheetCoverId = LabourIdentifierFactory.NextXeroLineTimesheetCoverId(),
                XeroLedgerLineId = entity.XeroLedgerLineId,
                ProjectId = coverTemplate?.ProjectId ?? "",
                SubcontractorId = coverTemplate?.SubcontractorId ?? run.Schedule.SubcontractorId ?? "",
                PeriodStart = coverTemplate?.PeriodStart ?? run.MonthStart,
                PeriodEnd = coverTemplate?.PeriodEnd ?? run.MonthEnd,
                CreatedByEmail = coverTemplate?.CreatedByEmail ?? run.RunByEmail,
                CreatedAt = now,
            });
            covered++;
        }
        return covered;
    }

    /// <summary>
    /// Bills in the ledger that read as this worker's for the month: contact matches the worker
    /// (their name or their linked company, the allocation page's rule), and the period matches —
    /// the month the invoice number / reference states where it states one ("Aug 2026",
    /// "August 2026", "08/2026", "2026-08"), else a bill dated from a week before the month to
    /// two weeks after it. Voided/deleted bills never count.
    /// </summary>
    private List<IGrouping<string, XeroLedgerLineEntity>> RecognisedBills(WorkerRun run)
    {
        var names = new[] { run.Schedule.WorkerName, run.Schedule.SubcontractorName }
            .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
        var dateFrom = run.MonthStart.AddDays(-7).UtcDateTime;
        var dateTo = run.MonthEnd.AddDays(14).UtcDateTime;
        return run.NearbyLines
            .Where(line => !string.IsNullOrWhiteSpace(line.ContactName)
                && !IsGone(line.InvoiceStatus)
                && names.Any(name => WorkerDirectoryMatcher.Matches(line.ContactName!, name)))
            .GroupBy(line => line.XeroInvoiceId)
            .Where(bill =>
            {
                var first = bill.First();
                var stated = StatedMonth(first.InvoiceNumber) ?? StatedMonth(first.Reference);
                if (stated is not null)
                    return stated.Value.Year == run.MonthStart.Year && stated.Value.Month == run.MonthStart.Month;
                return first.Date is { } date && date >= dateFrom && date < dateTo;
            })
            .OrderBy(bill => bill.First().Date)
            .ToList();
    }

    private static readonly Regex MonthNameYear = new(
        @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|sept|oct|nov|dec)[a-z]*\.?\s*[-/']?\s*(20\d\d|\d\d)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex NumericMonthYear = new(
        @"\b(0?[1-9]|1[0-2])[/\-](20\d\d)\b|\b(20\d\d)[/\-](0?[1-9]|1[0-2])\b",
        RegexOptions.CultureInvariant);

    /// <summary>The month a bill number/reference states, when it states one.</summary>
    internal static (int Year, int Month)? StatedMonth(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var named = MonthNameYear.Match(text);
        if (named.Success)
        {
            var abbreviation = named.Groups[1].Value[..3].ToLowerInvariant();
            var month = Array.IndexOf(MonthAbbreviations, abbreviation) + 1;
            var year = int.Parse(named.Groups[2].Value, CultureInfo.InvariantCulture);
            if (year < 100) year += 2000;
            if (month > 0) return (year, month);
        }
        var numeric = NumericMonthYear.Match(text);
        if (numeric.Success)
        {
            if (numeric.Groups[1].Success)
                return (int.Parse(numeric.Groups[2].Value, CultureInfo.InvariantCulture), int.Parse(numeric.Groups[1].Value, CultureInfo.InvariantCulture));
            return (int.Parse(numeric.Groups[3].Value, CultureInfo.InvariantCulture), int.Parse(numeric.Groups[4].Value, CultureInfo.InvariantCulture));
        }
        return null;
    }

    private static readonly string[] MonthAbbreviations =
        { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };

    /// <summary>The contact Xero already holds for the worker (their latest bill's contact name),
    /// else the settlement name the schedule carries.</summary>
    private string PreferredContactName(WorkerSettlementSchedule schedule)
    {
        var names = new[] { schedule.WorkerName, schedule.SubcontractorName }
            .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct().ToList();
        var known = knownContacts.FirstOrDefault(contact => names.Any(name => WorkerDirectoryMatcher.Matches(contact, name)));
        return known ?? schedule.SubcontractorName;
    }

    private bool IsCostOfSales(string? accountCode)
    {
        if (xeroOptions.CostOfSalesAccountPrefixes.Count == 0) return true;
        if (string.IsNullOrWhiteSpace(accountCode)) return false;
        return xeroOptions.CostOfSalesAccountPrefixes.Any(prefix =>
            accountCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGone(string status) =>
        status.Equals("VOIDED", StringComparison.OrdinalIgnoreCase)
        || status.Equals("DELETED", StringComparison.OrdinalIgnoreCase);

    private static string BillLabel(XeroBillSummary bill) =>
        !string.IsNullOrWhiteSpace(bill.InvoiceNumber) ? $"\"{bill.InvoiceNumber}\""
        : !string.IsNullOrWhiteSpace(bill.Reference) ? $"\"{bill.Reference}\""
        : bill.InvoiceId;

    private static string LedgerBillLabel(XeroLedgerLineEntity line) =>
        !string.IsNullOrWhiteSpace(line.InvoiceNumber) ? $"\"{line.InvoiceNumber}\""
        : !string.IsNullOrWhiteSpace(line.Reference) ? $"\"{line.Reference}\""
        : line.XeroInvoiceId;

    private static string LinesSummary(IReadOnlyList<XeroScheduleLine> lines) =>
        "Lines: " + string.Join("; ", lines.Select(line => $"{line.SiteOption} / {line.CostCodeOption} → {line.AccountCode} £{line.Net:N2}")) + ".";

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];

    private List<SiteXeroMappingEntity> siteMappingsCache = new();
    private List<CostCodeXeroMappingEntity> codeMappingsCache = new();
    private List<string> knownContacts = new();

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
