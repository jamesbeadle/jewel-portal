using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Links (or, with a null/empty PartyId, unlinks) a request to the party it is corresponded with —
/// a client account directly, or an architect practice optionally acting on a client's behalf.
/// </summary>
public sealed class LinkRequestToPartyHandler : ICommandHandler<LinkRequestToParty, Request>
{
    private readonly JpmsContext context;
    public LinkRequestToPartyHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(LinkRequestToParty command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        if (string.IsNullOrWhiteSpace(command.PartyId))
        {
            entity.PartyKind = (int)PartyKind.Client;
            entity.PartyId = null;
            entity.OnBehalfOfClientId = null;
        }
        else if (command.PartyKind == PartyKind.Architect)
        {
            var architect = await context.Architects.FindAsync(new object[] { command.PartyId }, cancellationToken);
            if (architect is null) throw new InvalidOperationException($"Architect {command.PartyId} not found.");

            string? onBehalfOfClientId = null;
            if (!string.IsNullOrWhiteSpace(command.OnBehalfOfClientId))
            {
                var client = await context.Clients.FindAsync(new object[] { command.OnBehalfOfClientId }, cancellationToken);
                if (client is null) throw new InvalidOperationException($"Client {command.OnBehalfOfClientId} not found.");
                onBehalfOfClientId = command.OnBehalfOfClientId;
            }

            entity.PartyKind = (int)PartyKind.Architect;
            entity.PartyId = command.PartyId;
            entity.OnBehalfOfClientId = onBehalfOfClientId;
        }
        else
        {
            var client = await context.Clients.FindAsync(new object[] { command.PartyId }, cancellationToken);
            if (client is null) throw new InvalidOperationException($"Client {command.PartyId} not found.");

            entity.PartyKind = (int)PartyKind.Client;
            entity.PartyId = command.PartyId;
            entity.OnBehalfOfClientId = null; // only meaningful when the party is an architect
        }

        await context.SaveChangesAsync(cancellationToken);

        // Return with the itemised queries so the detail view keeps them across a party change.
        var items = await context.RequestItems
            .Where(item => item.RequestId == entity.RequestId)
            .ToListAsync(cancellationToken);
        return entity.ToModel(items);
    }
}
