using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Clients;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class UpdateClientArchitectHandler : ICommandHandler<UpdateClientArchitect, Client>
{
    private readonly JpmsContext context;
    public UpdateClientArchitectHandler(JpmsContext context) { this.context = context; }

    public async Task<Client> HandleAsync(UpdateClientArchitect command, CancellationToken cancellationToken)
    {
        var entity = await context.Clients.FindAsync(new object[] { command.ClientId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Client {command.ClientId} not found.");

        entity.ArchitectName = command.ArchitectName;
        entity.ArchitectEmail = command.ArchitectEmail;
        if (command.PrimaryContactName is not null) entity.PrimaryContactName = command.PrimaryContactName;
        if (command.PrimaryContactEmail is not null) entity.PrimaryContactEmail = command.PrimaryContactEmail;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
