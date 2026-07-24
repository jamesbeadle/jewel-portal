using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Creates a standalone variation order (in Quoting) with no request behind it — the manual-entry
/// route for historic / client-instructed variations that never tendered through the app. Mirrors
/// CreateVoqFromRfq's numbering and clamping, but the project comes straight off the command (there
/// is no request to inherit it from) and RequestId is left empty. A caller-supplied Number is used
/// as-is once it is confirmed free on the project (it fixes the VOQ number and the V-ref minted at
/// approval, so a manual add can match a reference already issued to the client); otherwise the
/// project's next number is taken. Approval — and the Valuation Report / CVR / budget write-through —
/// still runs through ApproveVariationOrder.
/// </summary>
public sealed class CreateManualVariationOrderHandler : ICommandHandler<CreateManualVariationOrder, VariationOrder>
{
    private readonly JpmsContext context;
    public CreateManualVariationOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrder> HandleAsync(CreateManualVariationOrder command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var title = command.Title.Trim();
        if (title.Length == 0) throw new InvalidOperationException("A title is required.");
        var description = (command.Description ?? "").Trim();
        // Clamp to the entity's storage limits — the same guard CreateVoqFromRfq applies.
        if (title.Length > 256) title = title[..256];
        if (description.Length > 2048) description = description[..2048];

        // Numbering is per-project (references like "VOQ-0050" are only unique within a project).
        // A caller-set number is honoured once it is free; otherwise take one past the project's max.
        int number;
        if (command.Number is { } requested)
        {
            if (requested <= 0)
                throw new InvalidOperationException("The variation number must be a positive whole number.");
            var taken = await context.VariationOrders
                .AnyAsync(vo => vo.ProjectId == command.ProjectId && vo.Number == requested, cancellationToken);
            if (taken)
                throw new InvalidOperationException($"{VariationsIdentifierFactory.Reference(requested)} already exists on this project — choose a different number.");
            number = requested;
        }
        else
        {
            number = (await context.VariationOrders
                .Where(other => other.ProjectId == command.ProjectId)
                .MaxAsync(other => (int?)other.Number, cancellationToken) ?? 0) + 1;
        }

        var entity = new VariationOrderEntity
        {
            VariationOrderId = VariationsIdentifierFactory.NextVariationOrderId(),
            ProjectId = command.ProjectId,
            RequestId = "",                 // standalone: no request behind it (link later on the variation)
            Number = number,
            Reference = VariationsIdentifierFactory.Reference(number),
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
