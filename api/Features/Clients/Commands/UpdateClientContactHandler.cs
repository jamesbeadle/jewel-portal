using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class UpdateClientContactHandler : ICommandHandler<UpdateClientContact, Client>
{
    private readonly JpmsContext context;
    public UpdateClientContactHandler(JpmsContext context) { this.context = context; }

    public async Task<Client> HandleAsync(UpdateClientContact command, CancellationToken cancellationToken)
    {
        var entity = await context.Clients.FindAsync(new object[] { command.ClientId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Client {command.ClientId} not found.");

        entity.Name = command.Name.Trim();
        entity.PrimaryContactName = command.PrimaryContactName;
        entity.PrimaryContactEmail = command.PrimaryContactEmail;

        // The client's contact book mirrors the legacy fields: editing here updates (or creates)
        // the primary party contact so correspondence resolution stays consistent.
        await PartyContactLegacySync.SyncPrimaryAsync(
            context, PartyKind.Client, entity.ClientId,
            command.PrimaryContactName, command.PrimaryContactEmail, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
