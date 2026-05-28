using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class DraftValuationHandler : ICommandHandler<DraftValuation, Valuation>
{
    private readonly JpmsContext context;
    public DraftValuationHandler(JpmsContext context) { this.context = context; }

    public async Task<Valuation> HandleAsync(DraftValuation command, CancellationToken cancellationToken)
    {
        var entity = new ValuationEntity
        {
            ValuationId = CommercialIdentifierFactory.NextValuationId(),
            ClaimPeriodId = command.ClaimPeriodId,
            ProjectId = command.ProjectId,
            GrossValue = command.GrossValue,
            RetentionPercent = command.RetentionPercent,
            NetValue = command.GrossValue * (1m - (command.RetentionPercent / 100m)),
            IsIssued = false,
            IssuedAt = null
        };
        context.Valuations.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
