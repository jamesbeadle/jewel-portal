using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class CreateClientHandler : ICommandHandler<CreateClient, Client>
{
    private readonly JpmsContext context;
    public CreateClientHandler(JpmsContext context) { this.context = context; }

    public async Task<Client> HandleAsync(CreateClient command, CancellationToken cancellationToken)
    {
        var entity = new ClientEntity
        {
            ClientId = ClientIdentifierFactory.NextClientId(),
            Name = command.Name.Trim(),
            PrimaryContactName = command.PrimaryContactName,
            PrimaryContactEmail = command.PrimaryContactEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Clients.Add(entity);

        // Seed the contact book: the primary contact given at creation becomes the client's
        // primary To correspondent on the new PartyContacts table.
        await PartyContactLegacySync.SyncPrimaryAsync(
            context, PartyKind.Client, entity.ClientId,
            command.PrimaryContactName, command.PrimaryContactEmail, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
