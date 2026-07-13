using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Contracts.Portal;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Portal.Commands;

public sealed class RaiseMyVariationRequestHandler
    : ICommandHandler<RaiseMyVariationRequest, SubcontractorVariationRequest>
{
    private readonly JpmsContext context;

    public RaiseMyVariationRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<SubcontractorVariationRequest> HandleAsync(RaiseMyVariationRequest command, CancellationToken cancellationToken)
    {
        var workOrder = await context.WorkOrders
            .FirstOrDefaultAsync(order => order.WorkOrderId == command.WorkOrderId, cancellationToken);
        if (workOrder is null
            || !string.Equals(workOrder.SubcontractorId, command.SubcontractorId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Work order not found."); // Same message either way: don't reveal other ids.
        if (workOrder.Status is not ((int)WorkOrderStatus.Released or (int)WorkOrderStatus.Complete))
            throw new InvalidOperationException("Variations can only be raised against issued work orders.");

        // Clamp to storage limits — the portal form is friendly but the API is the boundary.
        var title = command.Title.Trim();
        if (title.Length > 256) title = title[..256];
        var description = command.Description.Trim();
        if (description.Length > 2048) description = description[..2048];

        var entity = new SubcontractorVariationRequestEntity
        {
            VariationRequestId = VariationsIdentifierFactory.NextVariationRequestId(),
            ProjectId = workOrder.ProjectId,
            WorkOrderId = workOrder.WorkOrderId,
            SubcontractorId = command.SubcontractorId,
            Title = title,
            Description = description,
            ProposedValue = command.ProposedValue,
            Status = (int)VariationRequestStatus.Submitted,
            SubmittedAt = DateTimeOffset.UtcNow
        };
        context.SubcontractorVariationRequests.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        var projectName = await context.Projects
            .Where(project => project.ProjectId == workOrder.ProjectId)
            .Select(project => project.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "";
        return entity.ToModel(projectName, workOrder.Number);
    }
}
