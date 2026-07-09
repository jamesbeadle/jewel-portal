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
/// Allocate carries either one cost centre for the whole batch or — for a
/// single line — a split across several centres whose nets must sum exactly
/// to the line's net. Any change of allocation replaces the line's previous
/// split rows.
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

        // A one-entry "split" is just a whole-line allocation to that centre.
        var singleCode = command.Splits is { Count: 1 }
            ? command.Splits[0].CostCenterCode
            : command.CostCenterCode;
        var splits = NormalisedSplits(command);
        if (command.Action == XeroAllocationAction.Allocate)
            await ValidateAllocateAsync(command, splits, singleCode, lines, cancellationToken);

        if (command.Action == XeroAllocationAction.AllocateToBucket
            && !XeroBuckets.All.Contains(command.Bucket ?? "", StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose a bucket (Parking, Fuel, Software subscriptions or Other).");

        // Whatever the action, the previous split rows no longer describe these lines.
        // Reconciled in place rather than delete-and-re-add: EF's identity map refuses a
        // new instance whose key matches a deleted-but-tracked row, and a re-cut split
        // commonly keeps some of the same cost centres (same "{lineId}:{code}" keys).
        var oldSplits = await context.XeroCostSplits
            .Where(split => ids.Contains(split.XeroLedgerLineId))
            .ToListAsync(cancellationToken);

        var desiredSplits = new Dictionary<string, XeroCostSplit>(StringComparer.OrdinalIgnoreCase);
        if (command.Action == XeroAllocationAction.Allocate && splits is not null)
            foreach (var split in splits)
                desiredSplits[$"{lines[0].XeroLedgerLineId}:{split.CostCenterCode}"] = split; // splits ⇒ exactly one line (validated above)

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
                CostCenterCode = split.CostCenterCode,
                Net = split.Net
            });

        var now = DateTimeOffset.UtcNow;
        foreach (var line in lines)
        {
            switch (command.Action)
            {
                case XeroAllocationAction.Allocate:
                    line.AllocationStatus = (int)XeroAllocationStatus.Allocated;
                    line.ProjectId = command.ProjectId;
                    // A split line carries its centres in XeroCostSplits (reconciled above),
                    // never on the line itself.
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
    /// Splits with a single entry are collapsed to a plain whole-line allocation;
    /// two or more entries stay a split. Returns null for "no split".
    /// </summary>
    private static IReadOnlyList<XeroCostSplit>? NormalisedSplits(SetXeroAllocation command)
    {
        if (command.Splits is null || command.Splits.Count == 0) return null;
        if (command.Splits.Count == 1) return null;
        return command.Splits;
    }

    private async Task ValidateAllocateAsync(
        SetXeroAllocation command,
        IReadOnlyList<XeroCostSplit>? splits,
        string? singleCode,
        IReadOnlyList<XeroLedgerLineEntity> lines,
        CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects
            .AnyAsync(project => project.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists)
            throw new InvalidOperationException("Choose a project before allocating.");

        if (splits is null)
        {
            var costCenterActive = await context.CostCenters
                .AnyAsync(centre => centre.Code == singleCode && centre.IsActive, cancellationToken);
            if (!costCenterActive)
                throw new InvalidOperationException("Choose an active cost centre before allocating.");

            // A one-entry Splits list collapsed to this path — its amount must still
            // describe the whole of exactly one line, or the caller's intent is off.
            if (command.Splits is { Count: 1 })
            {
                if (lines.Count != 1)
                    throw new InvalidOperationException("A cost-centre split applies to one line at a time.");
                if (command.Splits[0].Net != lines[0].Net)
                    throw new InvalidOperationException(
                        $"The split must add up to the line's net of {lines[0].Net:0.00} — it currently adds up to {command.Splits[0].Net:0.00}.");
            }
            return;
        }

        // Split allocations are line-specific (the amounts belong to one line's net).
        if (lines.Count != 1)
            throw new InvalidOperationException("A cost-centre split applies to one line at a time.");

        var line = lines[0];
        if (splits.Any(split => split.Net <= 0m))
            throw new InvalidOperationException("Every split amount must be greater than zero.");

        var codes = splits.Select(split => split.CostCenterCode).ToList();
        if (codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Count)
            throw new InvalidOperationException("Each cost centre can appear only once in a split.");

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
