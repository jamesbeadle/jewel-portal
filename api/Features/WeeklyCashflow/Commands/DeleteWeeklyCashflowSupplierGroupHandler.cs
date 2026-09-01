using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

/// <summary>Dissolves a group — its bills go back to one line per supplier. Hard delete: the
/// group is display arrangement only; placements and exclusions are untouched. The deleted
/// group comes back in the answer so the client can un-apply it locally.</summary>
public sealed class DeleteWeeklyCashflowSupplierGroupHandler : ICommandHandler<DeleteWeeklyCashflowSupplierGroup, WeeklyCashflowSupplierGroup>
{
    private readonly JpmsContext context;

    public DeleteWeeklyCashflowSupplierGroupHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowSupplierGroup> HandleAsync(DeleteWeeklyCashflowSupplierGroup command, CancellationToken cancellationToken)
    {
        var entity = await context.WeeklyCashflowSupplierGroups
            .FirstOrDefaultAsync(group => group.SupplierGroupId == command.SupplierGroupId, cancellationToken)
            ?? throw new InvalidOperationException($"Supplier group '{command.SupplierGroupId}' not found.");

        context.WeeklyCashflowSupplierGroups.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
