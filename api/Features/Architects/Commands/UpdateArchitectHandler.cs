using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Parties;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

public sealed class UpdateArchitectHandler : ICommandHandler<UpdateArchitect, Architect>
{
    private readonly JpmsContext context;
    public UpdateArchitectHandler(JpmsContext context) { this.context = context; }

    public async Task<Architect> HandleAsync(UpdateArchitect command, CancellationToken cancellationToken)
    {
        var entity = await context.Architects.FindAsync(new object[] { command.ArchitectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Architect {command.ArchitectId} not found.");

        entity.Name = command.Name.Trim();
        entity.ContactName = command.ContactName;
        entity.ContactEmail = command.ContactEmail;

        // The practice's contact book mirrors the legacy fields: editing here updates (or creates)
        // the primary party contact so correspondence resolution stays consistent.
        await PartyContactLegacySync.SyncPrimaryAsync(
            context, PartyKind.Architect, entity.ArchitectId,
            command.ContactName, command.ContactEmail, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
