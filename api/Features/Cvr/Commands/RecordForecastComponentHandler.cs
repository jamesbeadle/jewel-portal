using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordForecastComponentHandler : ICommandHandler<RecordForecastComponent, ForecastComponent>
{
    private readonly JpmsContext context;

    public RecordForecastComponentHandler(JpmsContext context) { this.context = context; }

    public async Task<ForecastComponent> HandleAsync(RecordForecastComponent command, CancellationToken cancellationToken)
    {
        var entity = await context.ForecastComponents.FirstOrDefaultAsync(
            component => component.ProjectId == command.ProjectId && component.PackageName == command.PackageName, cancellationToken);

        entity ??= AddNewForecastComponent(command);
        entity.CostIncurred = command.CostIncurred;
        entity.CostCommitted = command.CostCommitted;
        entity.QsAccrualAmount = command.QsAccrualAmount;
        entity.PrelimForecast = command.PrelimForecast;
        entity.CostToComplete = command.CostToComplete;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private ForecastComponentEntity AddNewForecastComponent(RecordForecastComponent command)
    {
        var entity = new ForecastComponentEntity
        {
            ForecastComponentId = CvrIdentifierFactory.NextForecastComponentId(),
            ProjectId = command.ProjectId,
            PackageName = command.PackageName
        };
        context.ForecastComponents.Add(entity);
        return entity;
    }
}
