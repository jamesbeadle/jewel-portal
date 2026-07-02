using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class DeleteRequestHandler : ICommandHandler<DeleteRequest, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteRequest command, CancellationToken cancellationToken)
    {
        // Remove the conversation first — these are flat entities with no FK cascade.
        var messages = await context.RequestMessages
            .Where(message => message.RequestId == command.RequestId)
            .ToListAsync(cancellationToken);
        context.RequestMessages.RemoveRange(messages);

        // And the official document's itemised queries, for the same reason.
        var items = await context.RequestItems
            .Where(item => item.RequestId == command.RequestId)
            .ToListAsync(cancellationToken);
        context.RequestItems.RemoveRange(items);

        var entity = await context.Requests
            .FirstOrDefaultAsync(request => request.RequestId == command.RequestId, cancellationToken);
        if (entity is not null) context.Requests.Remove(entity);

        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.RequestId);
    }
}
