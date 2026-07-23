using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Issues the NEW work order that instructs an approved variation order — existing orders are
/// never uplifted (subcontractor-crm-scope §6). Released immediately (the instruction IS the
/// issue), next sequential per-project number, VO's value/title/scope, linked via
/// WorkOrderEntity.VariationOrderId. A VO still in Approved state moves to Issued. A variation can
/// need several orders, so issuing again for the same VO is allowed — the UI shows what has
/// already been issued so it is a deliberate act, not an accident.
/// </summary>
public sealed class IssueWorkOrderForVariationOrderHandler
    : ICommandHandler<IssueWorkOrderForVariationOrder, WorkOrder>
{
    private readonly JpmsContext context;

    public IssueWorkOrderForVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(IssueWorkOrderForVariationOrder command, CancellationToken cancellationToken)
    {
        var variationOrder = await context.VariationOrders
            .FirstOrDefaultAsync(vo => vo.VariationOrderId == command.VariationOrderId, cancellationToken);
        if (variationOrder is null)
            throw new InvalidOperationException($"Variation order {command.VariationOrderId} not found.");
        if (variationOrder.Status == (int)VariationOrderStatus.Rejected)
            throw new InvalidOperationException("A rejected variation order cannot be instructed.");
        if (string.IsNullOrWhiteSpace(variationOrder.SelectedSubcontractorId))
            throw new InvalidOperationException("The variation order has no subcontractor to issue a work order to.");

        var nextNumber = (await context.WorkOrders
            .Where(order => order.ProjectId == variationOrder.ProjectId)
            .MaxAsync(order => (int?)order.Number, cancellationToken) ?? 0) + 1;

        var now = DateTimeOffset.UtcNow;
        var scope = string.IsNullOrWhiteSpace(variationOrder.Description) ? variationOrder.Title : variationOrder.Description;
        if (scope.Length > 4000) scope = scope[..4000];

        var entity = new WorkOrderEntity
        {
            WorkOrderId = ProcurementIdentifierFactory.NextWorkOrderId(),
            ProjectId = variationOrder.ProjectId,
            BidPackageId = null,
            SubcontractorId = variationOrder.SelectedSubcontractorId,
            Value = variationOrder.Value,
            Scope = scope,
            AwardedAt = now,
            AwardedByEmail = command.IssuedByEmail,
            Number = nextNumber,
            Title = $"{variationOrder.VariationRef} — {variationOrder.Title}",
            Status = (int)WorkOrderStatus.Released,
            CreatedAt = now,
            VariationOrderId = variationOrder.VariationOrderId
        };
        if (entity.Title.Length > 256) entity.Title = entity.Title[..256];
        context.WorkOrders.Add(entity);

        // One priced line carrying the VO's cost code, so line-based cost-centre groupings (the
        // allocation views) see this order's value — order totals aggregate lines, not headers.
        context.WorkOrderLines.Add(new WorkOrderLineEntity
        {
            WorkOrderLineId = ProcurementIdentifierFactory.NextWorkOrderLineId(),
            WorkOrderId = entity.WorkOrderId,
            Title = entity.Title,
            Description = scope.Length > 1024 ? scope[..1024] : scope,
            CostType = "Subcontractor",
            CostCode = variationOrder.CostCode,
            Quantity = 1m,
            Unit = "item",
            UnitCost = variationOrder.Value,
            LineTotal = variationOrder.Value,
            PaidToDate = 0m,
            SortOrder = 0
        });

        // Instructing a subcontractor happens after the client has approved — issuing a work order
        // does not move the variation's own status (Issued now means "sent to the client", which is
        // an earlier stage). The instruction is recorded by the work order itself.
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
