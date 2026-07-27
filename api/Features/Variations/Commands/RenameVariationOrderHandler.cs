using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Retitles a variation order, at any stage. Title only — and deliberately nothing else: the
/// valuation lines, CVR accruals and budget commitments an approval wrote keep the wording they
/// were written with (see RenameVariationOrder), because they are snapshots of paperwork already
/// issued. Later writes (a revision, a rejection) pick the new title up on their own.
///
/// Trims and clamps to the entity's 256-character limit, the same guard CreateManualVariationOrder
/// and CreateVoqFromRfq apply, so a title cannot arrive here by one route and be refused by another.
/// </summary>
public sealed class RenameVariationOrderHandler : ICommandHandler<RenameVariationOrder, VariationOrder>
{
    private const int MaxTitleChars = 256;

    private readonly JpmsContext context;
    public RenameVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(RenameVariationOrder command, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.FindAsync(new object[] { command.VariationOrderId }, cancellationToken);
        if (order is null) throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");

        var title = command.Title.Trim();
        if (title.Length == 0) throw new InvalidOperationException("A title is required.");
        if (title.Length > MaxTitleChars) title = title[..MaxTitleChars];

        order.Title = title;

        await context.SaveChangesAsync(cancellationToken);
        return order.ToModel();
    }
}
