using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordContraChargeHandler : ICommandHandler<RecordContraCharge, ContraCharge>
{
    private readonly JpmsContext context;

    public RecordContraChargeHandler(JpmsContext context) { this.context = context; }

    public async Task<ContraCharge> HandleAsync(RecordContraCharge command, CancellationToken cancellationToken)
    {
        var entity = new ContraChargeEntity
        {
            ContraChargeId = CommercialInputsIdentifierFactory.NextContraChargeId(),
            ProjectId = command.ProjectId,
            SubcontractorReference = command.SubcontractorReference,
            RaisedOn = command.RaisedOn,
            Description = command.Description,
            Category = command.Category,
            Amount = command.Amount,
            Status = command.Status,
            RecoveredAmount = command.RecoveredAmount
        };
        context.ContraCharges.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
