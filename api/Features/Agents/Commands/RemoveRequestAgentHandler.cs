using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class RemoveRequestAgentHandler : ICommandHandler<RemoveRequestAgent, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveRequestAgentHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveRequestAgent command, CancellationToken cancellationToken)
    {
        var entity = await context.RequestAgents
            .FirstOrDefaultAsync(a => a.RequestAgentId == command.RequestAgentId && a.RequestId == command.RequestId, cancellationToken);
        if (entity is not null)
        {
            context.RequestAgents.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.RequestAgentId);
    }
}
