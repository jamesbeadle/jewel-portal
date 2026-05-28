using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class LogDayworkHandler : ICommandHandler<LogDaywork, Daywork>
{
    private readonly JpmsContext context;

    public LogDayworkHandler(JpmsContext context) { this.context = context; }

    public async Task<Daywork> HandleAsync(LogDaywork command, CancellationToken cancellationToken)
    {
        var entity = new DayworkEntity
        {
            DayworkId = CommercialInputsIdentifierFactory.NextDayworkId(),
            ProjectId = command.ProjectId,
            WorkedOn = command.WorkedOn,
            SubcontractorReference = command.SubcontractorReference,
            Description = command.Description,
            InstructedBy = command.InstructedBy,
            Hours = command.Hours,
            HourlyRate = command.HourlyRate,
            LabourCost = command.LabourCost,
            PlantCost = command.PlantCost,
            MaterialsCost = command.MaterialsCost,
            UpliftPercent = command.UpliftPercent,
            ChargeableAmount = command.ChargeableAmount
        };
        context.Dayworks.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
