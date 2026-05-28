using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class IssueValuationHandler : ICommandHandler<IssueValuation, Valuation>
{
    private readonly JpmsContext context;
    public IssueValuationHandler(JpmsContext context) { this.context = context; }

    public async Task<Valuation> HandleAsync(IssueValuation command, CancellationToken cancellationToken)
    {
        var entity = await context.Valuations.FindAsync(new object[] { command.ValuationId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Valuation {command.ValuationId} not found.");
        if (entity.IsIssued) throw new InvalidOperationException($"Valuation {command.ValuationId} already issued.");
        entity.IsIssued = true;
        entity.IssuedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
