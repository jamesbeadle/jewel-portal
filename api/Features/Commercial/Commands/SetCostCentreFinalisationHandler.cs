using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCentreFinalisationHandler : ICommandHandler<SetCostCentreFinalisation, CostCentreCostProgress>
{
    private readonly JpmsContext context;

    public SetCostCentreFinalisationHandler(JpmsContext context) { this.context = context; }

    public async Task<CostCentreCostProgress> HandleAsync(SetCostCentreFinalisation command, CancellationToken cancellationToken)
    {
        var entity = await context.CostCentreCostProgress.FirstOrDefaultAsync(
            progress => progress.ProjectId == command.ProjectId && progress.CostCode == command.CostCode, cancellationToken);

        entity ??= AddNewProgress(command);
        entity.IsFinalised = command.IsFinalised;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private CostCentreCostProgressEntity AddNewProgress(SetCostCentreFinalisation command)
    {
        var entity = new CostCentreCostProgressEntity
        {
            CostCentreCostProgressId = CommercialIdentifierFactory.NextCostCentreCostProgressId(),
            ProjectId = command.ProjectId,
            CostCode = command.CostCode
        };
        context.CostCentreCostProgress.Add(entity);
        return entity;
    }
}
