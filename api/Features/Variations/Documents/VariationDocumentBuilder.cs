using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// Collates a <see cref="VariationDocumentModel"/> from the SQL source of truth. Pure read — calling
/// it on download, on attach or on every resend always reflects the variation exactly as it stands
/// (idempotent regeneration; nothing is persisted). Same arrangement as RequestDocumentBuilder.
/// </summary>
public static class VariationDocumentBuilder
{
    public static async Task<VariationDocumentModel?> BuildAsync(
        JpmsContext context, string variationOrderId, CancellationToken cancellationToken)
    {
        var order = await context.VariationOrders.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VariationOrderId == variationOrderId, cancellationToken);
        if (order is null)
            return null;

        var project = await context.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProjectId == order.ProjectId, cancellationToken);

        // The priced build-up lives on the valuation report under the minted V-ref, so it exists
        // only once the order is approved. Before then the document carries the estimate — honest
        // to the record: nothing has been written to the report yet.
        var lines = new List<VariationDocumentLine>();
        if (order.VariationRef is { Length: > 0 } variationRef)
            lines = await context.ValuationLineItems.AsNoTracking()
                .Where(line => line.ProjectId == order.ProjectId
                    && line.ElementType == (int)ValuationElementType.Variation
                    && line.VariationRef == variationRef)
                .OrderBy(line => line.DisplayOrder)
                .Select(line => new VariationDocumentLine(
                    line.CostCode, line.Description, line.Unit, line.Quantity, line.Rate, line.LineAmount))
                .ToListAsync(cancellationToken);

        var status = (VariationOrderStatus)order.Status;

        return new VariationDocumentModel(
            VariationOrderId: order.VariationOrderId,
            DisplayNumber: order.Number > 0 ? $"V{order.Number}" : "",
            Reference: order.Reference,
            Title: order.Title,
            Description: order.Description,
            StatusLabel: status.DisplayName(),
            ProjectName: project?.Name ?? "(unknown project)",
            ProjectReference: project?.Reference ?? order.ProjectId,
            ClientName: project?.ClientName ?? "",
            CreatedByEmail: order.CreatedByEmail,
            CreatedAt: order.CreatedAt,
            IssuedAt: order.IssuedAt,
            ApprovedAt: order.ApprovedAt,
            VariationRef: order.VariationRef,
            EstimatedValue: order.EstimatedValue,
            ApprovedValue: order.Value,
            IsApproved: status == VariationOrderStatus.Approved,
            CommercialBasis: order.CommercialBasis,
            ProgrammeImpact: order.ProgrammeImpact,
            Exclusions: order.Exclusions,
            Lines: lines,
            GeneratedAt: DateTimeOffset.UtcNow);
    }
}
