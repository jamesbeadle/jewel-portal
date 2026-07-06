using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

public sealed class CreateArchitectHandler : ICommandHandler<CreateArchitect, Architect>
{
    private readonly JpmsContext context;
    public CreateArchitectHandler(JpmsContext context) { this.context = context; }

    public async Task<Architect> HandleAsync(CreateArchitect command, CancellationToken cancellationToken)
    {
        var entity = new ArchitectEntity
        {
            ArchitectId = ArchitectIdentifierFactory.NextArchitectId(),
            Name = command.Name.Trim(),
            ContactName = command.ContactName,
            ContactEmail = command.ContactEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Architects.Add(entity);

        // Seed the contact book: the contact given at creation becomes the practice's primary To
        // correspondent on the new PartyContacts table.
        await PartyContactLegacySync.SyncPrimaryAsync(
            context, PartyKind.Architect, entity.ArchitectId,
            command.ContactName, command.ContactEmail, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
