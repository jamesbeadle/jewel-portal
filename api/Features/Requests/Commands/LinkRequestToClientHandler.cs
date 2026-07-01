using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>Links (or, with a null/empty ClientId, unlinks) a request to a client account.</summary>
public sealed class LinkRequestToClientHandler : ICommandHandler<LinkRequestToClient, Request>
{
    private readonly JpmsContext context;
    public LinkRequestToClientHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(LinkRequestToClient command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        if (string.IsNullOrWhiteSpace(command.ClientId))
        {
            entity.ClientId = null;
        }
        else
        {
            var client = await context.Clients.FindAsync(new object[] { command.ClientId }, cancellationToken);
            if (client is null) throw new InvalidOperationException($"Client {command.ClientId} not found.");
            entity.ClientId = command.ClientId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
