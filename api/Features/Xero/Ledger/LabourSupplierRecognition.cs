using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
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
/// settlement schedule reconciles by. Normalised equality first, then containment either way when
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
            .Select(worker => new { worker.WorkerId, worker.Name, worker.SubcontractorId })
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
            AddName(names, worker.Name, worker.WorkerId, worker.Name, worker.SubcontractorId);
            if (worker.SubcontractorId is not null && subNames.TryGetValue(worker.SubcontractorId, out var company))
                AddName(names, company, worker.WorkerId, worker.Name, worker.SubcontractorId);
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

    // Containment either way, but only when the shorter (contained) name still carries at least
    // two words — "Pranas Jancauskas" claims "Pranas Jancauskas Ltd" and vice versa; a lone
    // "Pranas" claims nothing.
    private static bool ContainsEitherWay(string supplier, string worker)
    {
        if (supplier.Length == worker.Length) return false; // equality already tested
        var (longer, shorter) = supplier.Length > worker.Length ? (supplier, worker) : (worker, supplier);
        if (!shorter.Contains(' ')) return false;
        return longer.Contains(shorter, StringComparison.Ordinal);
    }

    // Lowercase, letters and digits only, single spaces — the same idea as the suggester's
    // normalisation: punctuation and casing differences between Dext, Xero and the registry
    // must not defeat a match that a human would make instantly.
    private static string Normalise(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var length = 0;
        var lastWasSpace = true;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[length++] = char.ToLowerInvariant(ch);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                buffer[length++] = ' ';
                lastWasSpace = true;
            }
        }
        return new string(buffer[..length]).TrimEnd();
    }
}
