using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Creates a VOQ from a request that has an RFQ enabled. Enforces one variation order per request. The VOQ
/// inherits the request's project and (by default) its title.
/// </summary>
public sealed class CreateVoqFromRfqHandler : ICommandHandler<CreateVoqFromRfq, VariationOrder>
{
    private readonly JpmsContext context;
    public CreateVoqFromRfqHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(CreateVoqFromRfq command, CancellationToken cancellationToken)
    {
        var request = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");
        if (!request.HasRfq) throw new InvalidOperationException("A variation order can only be created once an RFQ is enabled on the request.");

        var existing = await context.VariationOrders
            .AnyAsync(vo => vo.RequestId == command.RequestId, cancellationToken);
        if (existing) throw new InvalidOperationException("A variation order already exists for this request.");

        // Per-project numbering: every project runs its own VOQ sequence (references like
        // "VOQ-0072" are only unique within a project — By France's seeded register already
        // runs to VOQ-0076, and other projects must not continue that sequence).
        var nextNumber = (await context.VariationOrders
            .Where(other => other.ProjectId == request.ProjectId)
            .MaxAsync(other => (int?)other.Number, cancellationToken) ?? 0) + 1;

        // Clamp to the entity's storage limits — the AI draft-review flow lets the user paste or
        // accept text the model was only asked (not guaranteed) to keep within bounds.
        var title = string.IsNullOrWhiteSpace(command.Title) ? request.Title : command.Title!.Trim();
        var description = command.Description?.Trim() ?? request.Description;
        if (title.Length > 256) title = title[..256];
        if (description.Length > 2048) description = description[..2048];

        var entity = new VariationOrderEntity
        {
            VariationOrderId = VariationsIdentifierFactory.NextVariationOrderId(),
            ProjectId = request.ProjectId,
            RequestId = request.RequestId,
            Number = nextNumber,
            Reference = VariationsIdentifierFactory.Reference(nextNumber),
            Title = title,
            Description = description,
            Status = (int)VariationOrderStatus.Quoting,
            EstimatedValue = command.EstimatedValue,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByEmail = command.CreatedByEmail
        };

        context.VariationOrders.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
