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

        await context.SaveChangesAsync(cancellationToken);
        return new ReconciliationPackage(
            package.ReconciliationPackageId, package.ProjectId, package.Name,
            orderIds, slices.ToList(), package.IsLocked, package.LockedAt);
    }
}
