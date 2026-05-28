using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class AddClaimPeriodHandler : ICommandHandler<AddClaimPeriod, ClaimPeriod>
{
    private readonly JpmsContext context;

    public AddClaimPeriodHandler(JpmsContext context) { this.context = context; }

    public async Task<ClaimPeriod> HandleAsync(AddClaimPeriod command, CancellationToken cancellationToken)
    {
        var entity = new ClaimPeriodEntity
        {
            ClaimPeriodId = CommercialIdentifierFactory.NextClaimPeriodId(),
            ProjectId = command.ProjectId,
            PeriodNumber = command.PeriodNumber,
            StartDate = command.StartDate,
            EndDate = command.EndDate
        };
        context.ClaimPeriods.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
