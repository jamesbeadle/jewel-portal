using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class PromoteSubcontractorToDirectoryHandler
    : ICommandHandler<PromoteSubcontractorToDirectory, Subcontractor>
{
    private readonly JpmsContext context;

    public PromoteSubcontractorToDirectoryHandler(JpmsContext context) { this.context = context; }

    public async Task<Subcontractor> HandleAsync(PromoteSubcontractorToDirectory command, CancellationToken cancellationToken)
    {
        var entity = await context.Subcontractors.FindAsync(new object[] { command.SubcontractorId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Subcontractor {command.SubcontractorId} not found.");

        // Idempotent: promoting a record already in the directory is a no-op, not an error — two
        // people pressing "Add to directory" on the same tender both get the same answer.
        if (entity.IsProspect)
        {
            entity.IsProspect = false;
            await context.SaveChangesAsync(cancellationToken);
        }

        return entity.ToModel(await context.TradesForAsync(entity.SubcontractorId, cancellationToken));
    }
}
