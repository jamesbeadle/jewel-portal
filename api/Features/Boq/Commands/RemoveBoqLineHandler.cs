using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Api.Features.Boq.Commands;

public sealed class RemoveBoqLineHandler
    : ICommandHandler<RemoveBoqLine, Acknowledgement>
{
    private readonly JpmsContext context;

    public RemoveBoqLineHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveBoqLine command, CancellationToken cancellationToken)
    {
        var entity = await context.BoqLineItems.FindAsync(new object[] { command.BoqLineItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"BoQ line {command.BoqLineItemId} not found.");

        context.BoqLineItems.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.BoqLineItemId);
    }
}
