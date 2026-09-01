using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Variations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Deletes a non-approved variation order. See DeleteVariationOrder for the guard rules. Bid
/// packages are separate records (separation 2026-08-12) and are never deleted with a variation —
/// legacy packages still parented to this VO are detached, and any line-item coverage pointing at
/// it is reset to Unassigned so nothing references a deleted order.
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

        // Bid packages survive the variation: detach any legacy package still parented to this VO
        // (the parent link predates the 2026-08-12 separation), and reset coverage on any package
        // line that named this VO as its commercial home — the QS re-links those lines to the
        // right home rather than the record pointing at a deleted order.
        var childPackages = await context.BidPackages
            .Where(p => p.VariationOrderId == order.VariationOrderId)
            .ToListAsync(cancellationToken);
        foreach (var package in childPackages) package.VariationOrderId = null;

        var coveredLines = await context.BidPackageLineItems
            .Where(l => l.VariationOrderId == order.VariationOrderId)
            .ToListAsync(cancellationToken);
        foreach (var line in coveredLines)
        {
            line.Coverage = (int)BidPackageLineCoverage.Unassigned;
            line.VariationOrderId = null;
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
