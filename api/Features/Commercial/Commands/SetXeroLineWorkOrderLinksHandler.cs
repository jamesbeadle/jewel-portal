using System.Globalization;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Replaces the work-order links on one allocated Xero purchase line. One full-amount
/// slice is the everyday whole-line link; several slices split a bill across the orders
/// it pays. Guards: slices carry the line's sign, may never total past the line's net,
/// and no slice may take its order past its value (existing links on other lines count).
/// </summary>
public sealed class SetXeroLineWorkOrderLinksHandler : ICommandHandler<SetXeroLineWorkOrderLinks, Acknowledgement>
{
    private static readonly CultureInfo Gbp = CultureInfo.GetCultureInfo("en-GB");

    private readonly JpmsContext context;

    public SetXeroLineWorkOrderLinksHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(SetXeroLineWorkOrderLinks command, CancellationToken cancellationToken)
    {
        var line = await context.XeroLedgerLines.FirstOrDefaultAsync(
            candidate => candidate.XeroLedgerLineId == command.XeroLedgerLineId, cancellationToken);
        if (line is null || !string.Equals(line.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This purchase line is not allocated to this project.");

        var slices = command.Links ?? Array.Empty<XeroWorkOrderLinkSlice>();

        if (slices.Count > 0)
        {
            if (slices.Select(slice => slice.WorkOrderId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != slices.Count)
                throw new InvalidOperationException("Each work order can only appear once in a split.");

            // Linking classifies the whole ledger line, so a line split across cost
            // centres (its shares live in XeroCostSplits) can't be linked — the other
            // centres' shares would silently follow. Mirrors the UI restriction.
            var isCentreSplit = line.CostCenterCode is null
                                || await context.XeroCostSplits.AnyAsync(
                                    split => split.XeroLedgerLineId == line.XeroLedgerLineId, cancellationToken);
            if (isCentreSplit)
                throw new InvalidOperationException(
                    "This line is split across cost centres, so it can't be linked to a work order. Re-cut it as a whole-line allocation first.");

            // Slices carry the line's sign (credit notes negative) and may never total
            // past the line's own net — the unallocated remainder stays non-WO cost.
            var signedNet = line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;
            if (signedNet == 0m)
                throw new InvalidOperationException("A zero-value line can't be linked to work orders.");
            if (slices.Any(slice => slice.Amount == 0m))
                throw new InvalidOperationException("Every split amount must be non-zero.");
            if (slices.Any(slice => Math.Sign(slice.Amount) != Math.Sign(signedNet)))
                throw new InvalidOperationException(signedNet < 0m
                    ? "This is a credit note — split amounts must be negative, subtracting from the orders they credit."
                    : "Split amounts must be positive.");
            var allocatedTotal = slices.Sum(slice => slice.Amount);
            if (Math.Abs(allocatedTotal) > Math.Abs(signedNet))
                throw new InvalidOperationException(
                    $"The split allocates {allocatedTotal.ToString("C2", Gbp)} but the line is only {signedNet.ToString("C2", Gbp)}.");

            var orderIds = slices.Select(slice => slice.WorkOrderId).ToList();
            var orders = await context.WorkOrders
                .Where(order => order.ProjectId == command.ProjectId && orderIds.Contains(order.WorkOrderId))
                .ToListAsync(cancellationToken);
            var ordersById = orders.ToDictionary(order => order.WorkOrderId, StringComparer.OrdinalIgnoreCase);

            foreach (var slice in slices)
            {
                if (!ordersById.TryGetValue(slice.WorkOrderId, out var order))
                    throw new InvalidOperationException("A work order in the split does not exist on this project.");
                if (order.Status == (int)WorkOrderStatus.Cancelled)
                    throw new InvalidOperationException($"{order.Reference} is cancelled — invoices can't be linked to it.");

                // Hard balance check: a slice may never take the order past its value.
                // Credit notes subtract, so they always fit.
                if (slice.Amount > 0m)
                {
                    var alreadyInvoiced = await context.XeroLineWorkOrderLinks
                        .Where(link => link.WorkOrderId == slice.WorkOrderId
                                       && link.XeroLedgerLineId != line.XeroLedgerLineId)
                        .SumAsync(link => (decimal?)link.Amount, cancellationToken) ?? 0m;
                    var remaining = order.Value - alreadyInvoiced;
                    if (slice.Amount > remaining)
                        throw new InvalidOperationException(
                            $"This would over-invoice {order.Reference}: the slice is {slice.Amount.ToString("C2", Gbp)} but only " +
                            $"{Math.Max(remaining, 0m).ToString("C2", Gbp)} of its {order.Value.ToString("C2", Gbp)} value is left to invoice.");
                }
            }
        }

        // Replace-all: the command carries the line's complete set of links.
        var existing = await context.XeroLineWorkOrderLinks
            .Where(link => link.XeroLedgerLineId == line.XeroLedgerLineId)
            .ToListAsync(cancellationToken);
        context.XeroLineWorkOrderLinks.RemoveRange(existing);

        foreach (var slice in slices)
        {
            context.XeroLineWorkOrderLinks.Add(new XeroLineWorkOrderLinkEntity
            {
                XeroLineWorkOrderLinkId = CommercialIdentifierFactory.NextXeroLineWorkOrderLinkId(),
                XeroLedgerLineId = line.XeroLedgerLineId,
                WorkOrderId = slice.WorkOrderId,
                ProjectId = command.ProjectId,
                Amount = slice.Amount
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(line.XeroLedgerLineId);
    }
}
