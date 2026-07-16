using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Creates or replaces a package's whole definition. Guards: a work order sits in at
/// most one package; a sales line's slices across all packages may never total past
/// the line's value (partial slices carry the line's sign); locked packages are
/// read-only until unlocked. Member lists are replaced wholesale — the builder always
/// saves the complete definition.
/// </summary>
public sealed class SaveReconciliationPackageHandler : ICommandHandler<SaveReconciliationPackage, ReconciliationPackage>
{
    private static readonly CultureInfo Gbp = CultureInfo.GetCultureInfo("en-GB");

    private readonly JpmsContext context;

    public SaveReconciliationPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<ReconciliationPackage> HandleAsync(SaveReconciliationPackage command, CancellationToken cancellationToken)
    {
        ReconciliationPackageEntity package;
        if (command.ReconciliationPackageId is null)
        {
            package = new ReconciliationPackageEntity
            {
                ReconciliationPackageId = CommercialIdentifierFactory.NextReconciliationPackageId(),
                ProjectId = command.ProjectId
            };
            context.ReconciliationPackages.Add(package);
        }
        else
        {
            package = await context.ReconciliationPackages.FirstOrDefaultAsync(
                    candidate => candidate.ReconciliationPackageId == command.ReconciliationPackageId
                                 && candidate.ProjectId == command.ProjectId, cancellationToken)
                ?? throw new InvalidOperationException("This package no longer exists — refresh and try again.");
            if (package.IsLocked)
                throw new InvalidOperationException("This package is locked. Unlock it before editing.");
        }
        package.Name = command.Name.Trim();

        var orderIds = command.WorkOrderIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var slices = command.SalesLines;

        // Work orders must exist on the project and belong to no other package.
        if (orderIds.Count > 0)
        {
            var known = await context.WorkOrders
                .Where(order => order.ProjectId == command.ProjectId && orderIds.Contains(order.WorkOrderId))
                .Select(order => order.WorkOrderId)
                .ToListAsync(cancellationToken);
            if (known.Count != orderIds.Count)
                throw new InvalidOperationException("A work order in the package does not exist on this project.");

            var claimedElsewhere = await context.ReconciliationPackageOrders
                .Where(member => member.ProjectId == command.ProjectId
                                 && orderIds.Contains(member.WorkOrderId)
                                 && member.ReconciliationPackageId != package.ReconciliationPackageId)
                .Select(member => member.WorkOrderId)
                .ToListAsync(cancellationToken);
            if (claimedElsewhere.Count > 0)
                throw new InvalidOperationException(
                    "Already in another package: " + string.Join(", ", claimedElsewhere) + ". A work order has one home.");
        }

        // Sales slices: the line must exist here, a slice carries the line's sign, and
        // all packages' slices of one line may never total past the line's value.
        if (slices.Count > 0)
        {
            var lineIds = slices.Select(slice => slice.ValuationLineItemId).ToList();
            if (lineIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != lineIds.Count)
                throw new InvalidOperationException("Each sales line can only appear once in a package — combine the amounts.");

            var lines = await context.ValuationLineItems
                .Where(line => line.ProjectId == command.ProjectId && lineIds.Contains(line.ValuationLineItemId))
                .Select(line => new { line.ValuationLineItemId, line.LineAmount })
                .ToListAsync(cancellationToken);
            var amountsByLine = lines.ToDictionary(line => line.ValuationLineItemId, line => line.LineAmount, StringComparer.OrdinalIgnoreCase);

            var otherSlices = (await context.ReconciliationPackageSalesLines
                    .Where(slice => slice.ProjectId == command.ProjectId
                                    && lineIds.Contains(slice.ValuationLineItemId)
                                    && slice.ReconciliationPackageId != package.ReconciliationPackageId)
                    .ToListAsync(cancellationToken))
                .GroupBy(slice => slice.ValuationLineItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(slice => slice.Amount), StringComparer.OrdinalIgnoreCase);

            foreach (var slice in slices)
            {
                if (!amountsByLine.TryGetValue(slice.ValuationLineItemId, out var lineAmount))
                    throw new InvalidOperationException("A sales line in the package does not exist on this project.");
                if (slice.Amount == 0m || lineAmount == 0m)
                    throw new InvalidOperationException("Every sales slice needs a non-zero amount.");
                if (Math.Sign(slice.Amount) != Math.Sign(lineAmount))
                    throw new InvalidOperationException("A slice must carry the same sign as its sales line (omits are negative).");

                var elsewhere = otherSlices.TryGetValue(slice.ValuationLineItemId, out var taken) ? taken : 0m;
                if (Math.Abs(elsewhere + slice.Amount) > Math.Abs(lineAmount))
                    throw new InvalidOperationException(
                        $"Over-allocated sales line: {slice.Amount.ToString("C2", Gbp)} requested but only " +
                        $"{(lineAmount - elsewhere).ToString("C2", Gbp)} of its {lineAmount.ToString("C2", Gbp)} value is still available.");
            }
        }

        // Direct cost slices: the Xero line must be a whole-line allocation on this
        // project (split lines can't be sliced — same rule as work-order links), a slice
        // carries the line's signed net's sign, and all packages' slices of one line may
        // never total past its non-work-order remainder (net less its order-link slices).
        var costSlices = (IReadOnlyList<PackageCostSlice>?)command.CostLines ?? Array.Empty<PackageCostSlice>();
        if (costSlices.Count > 0)
        {
            var costLineIds = costSlices.Select(slice => slice.XeroLedgerLineId).ToList();
            if (costLineIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != costLineIds.Count)
                throw new InvalidOperationException("Each purchase line can only appear once in a package — combine the amounts.");

            var ledgerLines = await context.XeroLedgerLines
                .Where(line => line.ProjectId == command.ProjectId
                               && costLineIds.Contains(line.XeroLedgerLineId)
                               && line.AllocationStatus == (int)Jewel.JPMS.Contracts.Xero.XeroAllocationStatus.Allocated
                               && line.CostCenterCode != null)
                .Select(line => new { line.XeroLedgerLineId, line.Net, line.Type })
                .ToListAsync(cancellationToken);
            var netsByLine = ledgerLines.ToDictionary(
                line => line.XeroLedgerLineId,
                line => line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net,
                StringComparer.OrdinalIgnoreCase);

            var splitLineIds = (await context.XeroCostSplits
                    .Where(split => costLineIds.Contains(split.XeroLedgerLineId))
                    .Select(split => split.XeroLedgerLineId)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Timesheet-covered lines are settlement of approved labour, not fresh cost —
            // they never feed the actuals columns, so netting them out would corrupt them.
            var coveredLineIds = (await context.XeroLineTimesheetCovers
                    .Where(cover => costLineIds.Contains(cover.XeroLedgerLineId))
                    .Select(cover => cover.XeroLedgerLineId)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var linkedByLine = (await context.XeroLineWorkOrderLinks
                    .Where(link => link.ProjectId == command.ProjectId && costLineIds.Contains(link.XeroLedgerLineId))
                    .ToListAsync(cancellationToken))
                .GroupBy(link => link.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(link => link.Amount), StringComparer.OrdinalIgnoreCase);

            var otherCostSlices = (await context.ReconciliationPackageCostLines
                    .Where(slice => slice.ProjectId == command.ProjectId
                                    && costLineIds.Contains(slice.XeroLedgerLineId)
                                    && slice.ReconciliationPackageId != package.ReconciliationPackageId)
                    .ToListAsync(cancellationToken))
                .GroupBy(slice => slice.XeroLedgerLineId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(slice => slice.Amount), StringComparer.OrdinalIgnoreCase);

            foreach (var slice in costSlices)
            {
                if (!netsByLine.TryGetValue(slice.XeroLedgerLineId, out var signedNet))
                    throw new InvalidOperationException("A purchase line in the package is not an allocated line on this project.");
                if (splitLineIds.Contains(slice.XeroLedgerLineId))
                    throw new InvalidOperationException("A centre-split purchase line can't join a package — package the whole line or unsplit it first.");
                if (coveredLineIds.Contains(slice.XeroLedgerLineId))
                    throw new InvalidOperationException("A timesheet-covered line is settlement of approved labour, not fresh cost — it can't join a package.");
                if (slice.Amount == 0m || signedNet == 0m)
                    throw new InvalidOperationException("Every purchase slice needs a non-zero amount.");
                if (Math.Sign(slice.Amount) != Math.Sign(signedNet))
                    throw new InvalidOperationException("A purchase slice must carry the same sign as its line (credit notes are negative).");

                var linked = linkedByLine.TryGetValue(slice.XeroLedgerLineId, out var linkTotal) ? linkTotal : 0m;
                var elsewhere = otherCostSlices.TryGetValue(slice.XeroLedgerLineId, out var taken) ? taken : 0m;
                var available = signedNet - linked - elsewhere;
                if (Math.Abs(elsewhere + linked + slice.Amount) > Math.Abs(signedNet))
                    throw new InvalidOperationException(
                        $"Over-allocated purchase line: {slice.Amount.ToString("C2", Gbp)} requested but only " +
                        $"{available.ToString("C2", Gbp)} of its {signedNet.ToString("C2", Gbp)} net is not already " +
                        "paying a work order or sitting in another package.");
            }
        }

        // Replace both member lists wholesale.
        var existingOrders = await context.ReconciliationPackageOrders
            .Where(member => member.ReconciliationPackageId == package.ReconciliationPackageId)
            .ToListAsync(cancellationToken);
        context.ReconciliationPackageOrders.RemoveRange(existingOrders);
        foreach (var workOrderId in orderIds)
        {
            context.ReconciliationPackageOrders.Add(new ReconciliationPackageOrderEntity
            {
                ReconciliationPackageOrderId = CommercialIdentifierFactory.NextReconciliationPackageOrderId(),
                ReconciliationPackageId = package.ReconciliationPackageId,
                ProjectId = command.ProjectId,
                WorkOrderId = workOrderId
            });
        }

        var existingSlices = await context.ReconciliationPackageSalesLines
            .Where(slice => slice.ReconciliationPackageId == package.ReconciliationPackageId)
            .ToListAsync(cancellationToken);
        context.ReconciliationPackageSalesLines.RemoveRange(existingSlices);
        foreach (var slice in slices)
        {
            context.ReconciliationPackageSalesLines.Add(new ReconciliationPackageSalesLineEntity
            {
                ReconciliationPackageSalesLineId = CommercialIdentifierFactory.NextReconciliationPackageSalesLineId(),
                ReconciliationPackageId = package.ReconciliationPackageId,
                ProjectId = command.ProjectId,
                ValuationLineItemId = slice.ValuationLineItemId,
                Amount = slice.Amount
            });
        }

        var existingCostSlices = await context.ReconciliationPackageCostLines
            .Where(slice => slice.ReconciliationPackageId == package.ReconciliationPackageId)
            .ToListAsync(cancellationToken);
        context.ReconciliationPackageCostLines.RemoveRange(existingCostSlices);
        foreach (var slice in costSlices)
        {
            context.ReconciliationPackageCostLines.Add(new ReconciliationPackageCostLineEntity
            {
                ReconciliationPackageCostLineId = CommercialIdentifierFactory.NextReconciliationPackageCostLineId(),
                ReconciliationPackageId = package.ReconciliationPackageId,
                ProjectId = command.ProjectId,
                XeroLedgerLineId = slice.XeroLedgerLineId,
                Amount = slice.Amount
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return new ReconciliationPackage(
            package.ReconciliationPackageId, package.ProjectId, package.Name,
            orderIds, slices.ToList(), package.IsLocked, package.LockedAt, costSlices.ToList());
    }
}
