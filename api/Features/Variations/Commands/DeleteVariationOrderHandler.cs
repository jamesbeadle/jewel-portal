using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Deletes a non-approved variation order and its quoting-stage tender data. See
/// DeleteVariationOrder for the guard rules. The cascade is explicit (no DB-level cascade is
/// configured on these tables): bid-package children first, then the packages, then the order.
/// </summary>
public sealed class DeleteVariationOrderHandler : ICommandHandler<DeleteVariationOrder, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteVariationOrder command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) return new Acknowledgement(command.VariationOrderId); // already gone — nothing to do

        if (order.Status == (int)VariationOrderStatus.Approved)
            throw new InvalidOperationException(
                "An approved variation order can't be deleted — reject it or return it to quoting first, so its Valuation Report, CVR and cost-centre budget entries are reversed.");

        // Work orders only instruct approved variations, but guard anyway — never orphan committed work.
        var instructed = await context.WorkOrders
            .AnyAsync(wo => wo.VariationOrderId == order.VariationOrderId, cancellationToken);
        if (instructed)
            throw new InvalidOperationException("Work orders instruct this variation — cancel them before deleting it.");

        // Cascade the tender data hanging off this VO's bid packages.
        var packageIds = await context.BidPackages
            .Where(p => p.VariationOrderId == order.VariationOrderId)
            .Select(p => p.BidPackageId)
            .ToListAsync(cancellationToken);
        if (packageIds.Count > 0)
        {
            var quoteIds = await context.Quotes
                .Where(q => packageIds.Contains(q.BidPackageId))
                .Select(q => q.QuoteId)
                .ToListAsync(cancellationToken);

            var quoteLines = await context.QuoteLineItems
                .Where(l => quoteIds.Contains(l.QuoteId)).ToListAsync(cancellationToken);
            var quotes = await context.Quotes
                .Where(q => packageIds.Contains(q.BidPackageId)).ToListAsync(cancellationToken);
            var recipients = await context.BidPackageRecipients
                .Where(r => packageIds.Contains(r.BidPackageId)).ToListAsync(cancellationToken);
            var lineItems = await context.BidPackageLineItems
                .Where(l => packageIds.Contains(l.BidPackageId)).ToListAsync(cancellationToken);
            var drawings = await context.BidPackageDrawings
                .Where(d => packageIds.Contains(d.BidPackageId)).ToListAsync(cancellationToken);
            var packages = await context.BidPackages
                .Where(p => packageIds.Contains(p.BidPackageId)).ToListAsync(cancellationToken);

            context.QuoteLineItems.RemoveRange(quoteLines);
            context.Quotes.RemoveRange(quotes);
            context.BidPackageRecipients.RemoveRange(recipients);
            context.BidPackageLineItems.RemoveRange(lineItems);
            context.BidPackageDrawings.RemoveRange(drawings);
            context.BidPackages.RemoveRange(packages);
        }

        // If this VO came from accepting a subcontractor's variation request, unlink it so that
        // request returns to the review queue instead of pointing at a deleted order.
        var linkedRequests = await context.SubcontractorVariationRequests
            .Where(r => r.VariationOrderId == order.VariationOrderId)
            .ToListAsync(cancellationToken);
        foreach (var request in linkedRequests) request.VariationOrderId = null;

        context.VariationOrders.Remove(order);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.VariationOrderId);
    }
}
