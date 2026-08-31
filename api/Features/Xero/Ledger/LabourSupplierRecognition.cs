using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// The Labour scope §6 recognition, computed next to the tracking suggester: an unallocated
/// purchase line whose SUPPLIER is one of the labour-only workers is settlement of approved
/// timesheets, not an ordinary cost — it belongs to the allocation page's Labour section and the
/// settlement machinery (covers → schedules → the §6a coding run), never in the ordinary queue
/// for a project/cost-centre decision that would double-count what the timesheets already carry.
///
/// Matching is by name only — the worker's own name (sole traders bill under it: the Dext
/// supplier IS "Pranas Jancauskas") or the linked subcontractor company's name, the same link the
/// settlement schedule reconciles by. MatchedSubcontractorId carries the settlement COUNTERPARTY
/// (2026-08-31): the linked company, or the worker themself when flagged a sole trader — so a
/// sole trader's bill can be marked as settlement without inventing a directory company. Normalised equality first, then containment either way when
/// the shorter name still has at least two words ("Pranas Jancauskas Ltd" ⊃ "Pranas Jancauskas";
/// a single word is never enough to claim a supplier). Account codes are deliberately not part of
/// the rule: 321 says CIS labour, not WHOSE labour, and a mis-coded bill must not hide from the
/// queue on the strength of its own mis-coding.
/// </summary>
public sealed class LabourSupplierRecognition
{
    /// <summary>What recognition knows about one line: the matched worker (null when the
    /// supplier is nobody in the registry) and any cover already claiming the line.</summary>
    public readonly record struct LineRecognition(
        string? MatchedWorkerId,
        string? MatchedWorkerName,
        string? MatchedSubcontractorId,
        bool CoveredByTimesheets,
        DateTimeOffset? CoveredPeriodStart);

    private readonly record struct WorkerName(
        string Normalised, string WorkerId, string DisplayName, string? SubcontractorId);

    private readonly List<WorkerName> names;
    private readonly Dictionary<string, DateTimeOffset> coverPeriodByLineId;
    private readonly Dictionary<string, LineRecognition?> matchCache = new(StringComparer.OrdinalIgnoreCase);

    private LabourSupplierRecognition(
        List<WorkerName> names, Dictionary<string, DateTimeOffset> coverPeriodByLineId)
    {
        this.names = names;
        this.coverPeriodByLineId = coverPeriodByLineId;
    }

    /// <summary>
    /// Builds the recogniser for a read, or null when no line in the read is unallocated —
    /// recognition, like the suggester, only matters while a line still needs a decision, and the
    /// worker/cover queries are skipped entirely on the other tabs.
    /// </summary>
    public static async Task<LabourSupplierRecognition?> ForAsync(
        JpmsContext context, IReadOnlyList<XeroLedgerLineEntity> entities, CancellationToken cancellationToken)
    {
        if (!entities.Any(entity => entity.AllocationStatus == (int)XeroAllocationStatus.Unallocated))
            return null;

        var workers = await context.Workers.AsNoTracking()
            .Where(worker => worker.IsActive)
            .Select(worker => new { worker.WorkerId, worker.Name, worker.SubcontractorId, worker.IsSoleTrader })
            .ToListAsync(cancellationToken);

        var subcontractorIds = workers
            .Where(worker => worker.SubcontractorId != null)
            .Select(worker => worker.SubcontractorId!)
            .Distinct()
            .ToList();
        var subNames = subcontractorIds.Count == 0
            ? new Dictionary<string, string>()
            : await context.Subcontractors.AsNoTracking()
                .Where(sub => subcontractorIds.Contains(sub.SubcontractorId))
                .ToDictionaryAsync(sub => sub.SubcontractorId, sub => sub.CompanyName, cancellationToken);

        var names = new List<WorkerName>();
        foreach (var worker in workers)
        {
            // The name row carries the settlement counterparty, not the raw link — a flagged
            // sole trader is their own counterparty, so their matched line can be covered.
            var counterparty = WorkerSettlementIdentity.CounterpartyId(
                worker.SubcontractorId, worker.IsSoleTrader, worker.WorkerId);
            AddName(names, worker.Name, worker.WorkerId, worker.Name, counterparty);
            if (worker.SubcontractorId is not null && subNames.TryGetValue(worker.SubcontractorId, out var company))
                AddName(names, company, worker.WorkerId, worker.Name, counterparty);
        }

        // The covers table is one row per covered line — small — and the covered flag has to
        // answer for every unallocated line, so it is read whole rather than by IN-list.
        var covers = await context.XeroLineTimesheetCovers.AsNoTracking()
            .ToListAsync(cancellationToken);
        var coverPeriodByLineId = new Dictionary<string, DateTimeOffset>();
        foreach (var cover in covers) coverPeriodByLineId[cover.XeroLedgerLineId] = cover.PeriodStart;

        return new LabourSupplierRecognition(names, coverPeriodByLineId);
    }

    private static void AddName(
        List<WorkerName> names, string raw, string workerId, string displayName, string? subcontractorId)
    {
        var normalised = Normalise(raw);
        if (normalised.Length == 0) return;
        if (names.Any(name => name.Normalised == normalised && name.WorkerId == workerId)) return;
        names.Add(new WorkerName(normalised, workerId, displayName, subcontractorId));
    }

    /// <summary>Recognition for one line — only unallocated lines are asked about.</summary>
    public LineRecognition? For(XeroLedgerLineEntity entity)
    {
        if (entity.AllocationStatus != (int)XeroAllocationStatus.Unallocated) return null;

        var covered = coverPeriodByLineId.TryGetValue(entity.XeroLedgerLineId, out var periodStart);
        var match = MatchContact(entity.ContactName);
        if (match is null && !covered) return null;
        return new LineRecognition(
            match?.MatchedWorkerId, match?.MatchedWorkerName, match?.MatchedSubcontractorId,
            covered, covered ? periodStart : null);
    }

    private LineRecognition? MatchContact(string? contactName)
    {
        if (string.IsNullOrWhiteSpace(contactName)) return null;
        if (matchCache.TryGetValue(contactName, out var cached)) return cached;

        var supplier = Normalise(contactName);
        LineRecognition? result = null;
        if (supplier.Length > 0)
        {
            // Equality beats containment; the first match wins within each rule, and the
            // registry is small enough that "first" is stable (insertion order by worker).
            var hit = names.FirstOrDefault(name => name.Normalised == supplier);
            if (hit.Normalised is null or "")
                hit = names.FirstOrDefault(name => ContainsEitherWay(supplier, name.Normalised));
            if (hit.Normalised is not (null or ""))
                result = new LineRecognition(hit.WorkerId, hit.DisplayName, hit.SubcontractorId, false, null);
        }
        matchCache[contactName] = result;
        return result;
    }

    // The matching rule itself lives in WorkerDirectoryMatcher (2026-08-31), shared with the
    // Xero import's auto-link and the reconcile sweep so the three cannot drift.
    private static bool ContainsEitherWay(string supplier, string worker) =>
        WorkerDirectoryMatcher.ContainsEitherWay(supplier, worker);

    private static string Normalise(string value) => WorkerDirectoryMatcher.Normalise(value);
}
