using Jewel.JPMS.Contracts.Commercial;

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

            // Drop the Draft claim's entry for this line so it stays reconcilable. A locked
            // claim's row stays: it is a line of the frozen statement the client was sent, with
            // the bill line copied onto it, and its money is the claim's (2026-09-18).
            var orphanedClaimLines = await DraftClaimRows.ForLinesAsync(
                context, new[] { command.ValuationLineItemId }, cancellationToken);
            context.ClaimLines.RemoveRange(orphanedClaimLines);
            context.ValuationLineItems.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ValuationLineItemId);
    }
}
