using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RemoveValuationLineItemHandler : ICommandHandler<RemoveValuationLineItem, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveValuationLineItemHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveValuationLineItem command, CancellationToken cancellationToken)
    {
        var entity = await context.ValuationLineItems.FindAsync(new object?[] { command.ValuationLineItemId }, cancellationToken);
        if (entity is not null)
        {
            // Variation lines mirror approved VOs — remove them by cancelling the VO instead.
            if (entity.ElementType == (int)Jewel.JPMS.Models.ValuationElementType.Variation)
                throw new InvalidOperationException(
                    "Variation lines mirror approved variation orders and cannot be removed directly. Cancel the variation order instead.");

            // Drop any claim entries that referenced this line so claims stay reconcilable.
            var orphanedClaimLines = await context.ClaimLines
                .Where(line => line.ValuationLineItemId == command.ValuationLineItemId)
                .ToListAsync(cancellationToken);
            context.ClaimLines.RemoveRange(orphanedClaimLines);
            context.ValuationLineItems.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ValuationLineItemId);
    }
}
