using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ReviseValuationHandler : ICommandHandler<ReviseValuation, Valuation>
{
    private readonly JpmsContext context;
    public ReviseValuationHandler(JpmsContext context) { this.context = context; }

    public async Task<Valuation> HandleAsync(ReviseValuation command, CancellationToken cancellationToken)
    {
        var entity = await context.Valuations.FindAsync(new object[] { command.ValuationId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation {command.ValuationId} not found.");
        if (entity.IsIssued) throw new InvalidOperationException($"Cannot revise an issued valuation.");
        entity.GrossValue = command.GrossValue;
        entity.RetentionPercent = command.RetentionPercent;
        entity.NetValue = command.GrossValue * (1m - (command.RetentionPercent / 100m));
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
