using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

/// <summary>Rewrites the item's editable face. The creator stamp is untouched. Placements are
/// left alone deliberately: occurrence keys carry natural dates, so a schedule change simply
/// stops asking for the old keys — nothing to clean, nothing lost.</summary>
public sealed class UpdateWeeklyCashflowItemHandler : ICommandHandler<UpdateWeeklyCashflowItem, WeeklyCashflowItem>
{
    private readonly JpmsContext context;

    public UpdateWeeklyCashflowItemHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowItem> HandleAsync(UpdateWeeklyCashflowItem command, CancellationToken cancellationToken)
    {
        var entity = await context.WeeklyCashflowItems
            .FirstOrDefaultAsync(item => item.WeeklyCashflowItemId == command.WeeklyCashflowItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Weekly cashflow item '{command.WeeklyCashflowItemId}' not found.");

        WeeklyCashflowItemDetailsRules.Apply(entity, command.Details);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
