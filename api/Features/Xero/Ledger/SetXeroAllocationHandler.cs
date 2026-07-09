using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Applies one allocation action to a batch of ledger lines. Returns how many
/// lines were updated. AllocatedBy arrives stamped by the endpoint from the
/// signed-in user.
///
/// Allocate carries either one project + cost centre for the whole batch or —
/// for a single line — a split across several shares, each with its own
/// project and cost centre, whose nets must sum exactly to the line's net.
/// Any change of allocation replaces the line's previous split rows.
///
/// After the allocation saves, any draft/submitted invoice whose stored lines
/// are now all allocated is confirmed back to Xero (tracking + approval) —
/// best-effort: the allocation stands even when Xero says no, and the outcome
/// is stamped on the lines.
/// </summary>
public sealed class SetXeroAllocationHandler : ICommandHandler<SetXeroAllocation, int>
{
    private readonly JpmsContext context;
    private readonly IXeroWriteBackService writeBack;

    public SetXeroAllocationHandler(JpmsContext context, IXeroWriteBackService writeBack)
    {
        this.context = context;
        this.writeBack = writeBack;
    }

    public async Task<int> HandleAsync(SetXeroAllocation command, CancellationToken cancellationToken)
    {
        var ids = command.XeroLedgerLineIds.Distinct().ToList();
        var lines = await context.XeroLedgerLines
            .Where(line => ids.Contains(line.XeroLedgerLineId))
            .ToListAsync(cancellationToken);

        // Resolve each share's project up front (a share without one falls back to the
        // command's project) and collapse a one-entry "split" to a whole-line allocation.
        var resolvedSplits = ResolveSplits(command);
        var splits = resolvedSplits is { Count: > 1 } ? resolvedSplits : null;
        var singleProject = resolvedSplits is { Count: 1 } ? resolvedSplits[0].ProjectId : command.ProjectId;
        var singleCode = resolvedSplits is { Count: 1 } ? resolvedSplits[0].CostCenterCode : command.CostCenterCode;

        if (command.Action == XeroAllocationAction.Allocate)
            await ValidateAllocateAsync(command, splits, singleProject, singleCode, lines, cancellationToken);

        if (command.Action == XeroAllocationAction.AllocateToBucket
            && !XeroBuckets.All.Contains(command.Bucket ?? "", StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a bucket (Parking, Fuel, Software subscriptions or Other).");

        // Whatever the action, the previous split rows no longer describe these lines.
        // Reconciled in place rather than delete-and-re-add: EF's identity map refuses a
        // new instance whose key matches a deleted-but-tracked row, and a re-cut split
        // commonly keeps some of the same project + centre combinations (same keys).
        var oldSplits = await context.XeroCostSplits
            .Where(split => ids.Contains(split.XeroLedgerLineId))
            .ToListAsync(cancellationToken);

        var desiredSplits = new Dictionary<string, XeroCostSplit>(StringComparer.OrdinalIgnoreCase);
        if (command.Action == XeroAllocationAction.Allocate && splits is not null)
            foreach (var split in splits)
                desiredSplits[$"{lines[0].XeroLedgerLineId}:{split.ProjectId}:{split.CostCenterCode}"] = split; // splits ⇒ exactly one line (validated above)

        foreach (var oldSplit in oldSplits)
        {
            if (desiredSplits.TryGetValue(oldSplit.XeroCostSplitId, out var kept))
            {
                oldSplit.Net = kept.Net;
                desiredSplits.Remove(oldSplit.XeroCostSplitId);
            }
            else
            {
                context.XeroCostSplits.Remove(oldSplit);
            }
        }
        foreach (var (key, split) in desiredSplits)
            context.XeroCostSplits.Add(new XeroCostSplitEntity
            {
                XeroCostSplitId = key,
                XeroLedgerLineId = lines[0].XeroLedgerLineId,
                ProjectId = split.ProjectId!,
                CostCenterCode = split.CostCenterCode,
                Net = split.Net
            });

        // A split spanning projects has no single line-level project; keep the common
        // one when there is one so lists and summaries can still show it directly.
        var splitProjects = splits?.Select(split => split.ProjectId!).Distinct().ToList();

        var now = DateTimeOffset.UtcNow;
        foreach (var line in lines)
        {
            switch (command.Action)
            {
                case XeroAllocationAction.Allocate:
                    line.AllocationStatus = (int)XeroAllocationStatus.Allocated;
                    // A split line carries its projects + centres in XeroCostSplits
                    // (reconciled above), never on the line itself.
                    line.ProjectId = splits is null ? singleProject
                        : splitProjects!.Count == 1 ? splitProjects[0]
                        : null;
                    line.CostCenterCode = splits is null ? singleCode : null;
                    line.Bucket = null;
                    line.AllocatedBy = command.AllocatedBy;
                    line.AllocatedAtUtc = now;
                    line.Note = command.Note;
                    break;
                case XeroAllocationAction.AllocateToBucket:
                    line.AllocationStatus = (int)XeroAllocationStatus.Bucketed;
                    line.ProjectId = null;
                    line.CostCenterCode = null;
                    line.Bucket = command.Bucket;
                    line.AllocatedBy = command.AllocatedBy;
                    line.AllocatedAtUtc = now;
                    line.Note = command.Note;
                    break;
                case XeroAllocationAction.Ignore:
                    line.AllocationStatus = (int)XeroAllocationStatus.Ignored;
                    line.ProjectId = null;
                    line.CostCenterCode = null;
                    line.Bucket = null;
                    line.AllocatedBy = command.AllocatedBy;
                    line.AllocatedAtUtc = now;
                    line.Note = command.Note;
                    break;
                case XeroAllocationAction.Reset:
                    line.AllocationStatus = (int)XeroAllocationStatus.Unallocated;
                    line.ProjectId = null;
                    line.CostCenterCode = null;
                    line.Bucket = null;
                    line.AllocatedBy = null;
                    line.AllocatedAtUtc = null;
                    line.Note = null;
                    break;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Confirm-and-approve any draft invoice these allocations completed. After the
        // save on purpose: the allocation is the record; Xero's answer is stamped onto it.
        if (command.Action == XeroAllocationAction.Allocate && lines.Count > 0)
            await writeBack.TryWriteBackAsync(
                lines.Select(line => line.XeroInvoiceId).Distinct().ToList(), cancellationToken);

        return lines.Count;
    }

    /// <summary>
    /// Fills each share's project from the command-level fallback. Returns null for
    /// "no splits supplied"; a share left without any project surfaces in validation.
    /// </summary>
    private static IReadOnlyList<XeroCostSplit>? ResolveSplits(SetXeroAllocation command)
    {
        if (command.Splits is null || command.Splits.Count == 0) return null;
        return command.Splits
            .Select(split => split with { ProjectId = split.ProjectId ?? command.ProjectId })
            .ToList();
    }

    private async Task ValidateAllocateAsync(
        SetXeroAllocation command,
        IReadOnlyList<XeroCostSplit>? splits,
        string? singleProject,
        string? singleCode,
        IReadOnlyList<XeroLedgerLineEntity> lines,
        CancellationToken cancellationToken)
    {
        if (splits is null)
        {
            var projectExists = await context.Projects
                .AnyAsync(project => project.ProjectId == singleProject, cancellationToken);
            if (!projectExists)
                throw new InvalidOperationException("Choose a project before allocating.");

            var costCenterActive = await context.CostCenters
                .AnyAsync(centre => centre.Code == singleCode && centre.IsActive, cancellationToken);
            if (!costCenterActive)
                throw new InvalidOperationException("Choose an active cost centre before allocating.");

            // A one-entry Splits list collapsed to this path — its amount must still
            // describe the whole of exactly one line, or the caller's intent is off.
            if (command.Splits is { Count: 1 })
            {
                if (lines.Count != 1)
                    throw new InvalidOperationException("A split applies to one line at a time.");
                if (command.Splits[0].Net != lines[0].Net)
                    throw new InvalidOperationException(
                        $"The split must add up to the line's net of {lines[0].Net:0.00} — it currently adds up to {command.Splits[0].Net:0.00}.");
            }
            return;
        }

        // Split allocations are line-specific (the amounts belong to one line's net).
        if (lines.Count != 1)
            throw new InvalidOperationException("A split applies to one line at a time.");

        var line = lines[0];
        if (splits.Any(split => split.Net <= 0m))
            throw new InvalidOperationException("Every split amount must be greater than zero.");
        if (splits.Any(split => string.IsNullOrWhiteSpace(split.ProjectId)))
            throw new InvalidOperationException("Every split row needs a project.");

        var pairs = splits.Select(split => $"{split.ProjectId}:{split.CostCenterCode}").ToList();
        if (pairs.Distinct(StringComparer.OrdinalIgnoreCase).Count() != pairs.Count)
            throw new InvalidOperationException("Each project + cost centre combination can appear only once in a split.");

        var projectIds = splits.Select(split => split.ProjectId!).Distinct().ToList();
        var knownProjects = await context.Projects
            .Where(project => projectIds.Contains(project.ProjectId))
            .Select(project => project.ProjectId)
            .ToListAsync(cancellationToken);
        var unknownProjects = projectIds.Except(knownProjects).ToList();
        if (unknownProjects.Count > 0)
            throw new InvalidOperationException($"Not a known project: {string.Join(", ", unknownProjects)}.");

        var codes = splits.Select(split => split.CostCenterCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var activeCodes = await context.CostCenters
            .Where(centre => centre.IsActive && codes.Contains(centre.Code))
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);
        var inactive = codes.Except(activeCodes, StringComparer.OrdinalIgnoreCase).ToList();
        if (inactive.Count > 0)
            throw new InvalidOperationException($"Not an active cost centre: {string.Join(", ", inactive)}.");

        var total = splits.Sum(split => split.Net);
        if (total != line.Net)
            throw new InvalidOperationException(
                $"The split must add up to the line's net of {line.Net:0.00} — it currently adds up to {total:0.00}.");
    }
}
