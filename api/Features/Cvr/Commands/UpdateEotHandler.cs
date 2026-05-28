using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class UpdateEotHandler : ICommandHandler<UpdateEot, Eot>
{
    private readonly JpmsContext context;
    public UpdateEotHandler(JpmsContext context) { this.context = context; }

    public async Task<Eot> HandleAsync(UpdateEot command, CancellationToken cancellationToken)
    {
        var entity = await context.Eots.FindAsync(new object[] { command.EotId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"EOT {command.EotId} not found.");
        entity.Reason = command.Reason;
        entity.DaysGranted = command.DaysGranted;
        entity.CommercialRecovery = command.CommercialRecovery;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
