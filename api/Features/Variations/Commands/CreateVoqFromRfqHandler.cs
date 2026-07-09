using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

/// <summary>
/// Creates a VOQ from a request that has an RFQ enabled. Enforces one VOQ per request. The VOQ
/// inherits the request's project and (by default) its title.
/// </summary>
public sealed class CreateVoqFromRfqHandler : ICommandHandler<CreateVoqFromRfq, VariationOrderQuote>
{
    private readonly JpmsContext context;
    public CreateVoqFromRfqHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderQuote> HandleAsync(CreateVoqFromRfq command, CancellationToken cancellationToken)
    {
        var request = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");
        if (!request.HasRfq) throw new InvalidOperationException("A VOQ can only be created once an RFQ is enabled on the request.");

        var existing = await context.VariationOrderQuotes
            .AnyAsync(voq => voq.RequestId == command.RequestId, cancellationToken);
        if (existing) throw new InvalidOperationException("A VOQ already exists for this request.");

        var nextNumber = (await context.VariationOrderQuotes.MaxAsync(voq => (int?)voq.Number, cancellationToken) ?? 0) + 1;

        // Clamp to the entity's storage limits — the AI draft-review flow lets the user paste or
        // accept text the model was only asked (not guaranteed) to keep within bounds.
        var title = string.IsNullOrWhiteSpace(command.Title) ? request.Title : command.Title!.Trim();
        var description = command.Description?.Trim() ?? request.Description;
        if (title.Length > 256) title = title[..256];
        if (description.Length > 2048) description = description[..2048];

        var entity = new VariationOrderQuoteEntity
        {
            VariationOrderQuoteId = VariationsIdentifierFactory.NextVoqId(),
            ProjectId = request.ProjectId,
            RequestId = request.RequestId,
            Number = nextNumber,
            Reference = VariationsIdentifierFactory.Reference(nextNumber),
            Title = title,
            Description = description,
            Status = (int)VariationOrderQuoteStatus.Draft,
            EstimatedValue = command.EstimatedValue,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByEmail = command.CreatedByEmail
        };

        context.VariationOrderQuotes.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
