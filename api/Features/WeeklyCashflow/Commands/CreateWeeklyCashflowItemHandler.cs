using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class CreateWeeklyCashflowItemHandler : ICommandHandler<CreateWeeklyCashflowItem, WeeklyCashflowItem>
{
    private readonly JpmsContext context;

    public CreateWeeklyCashflowItemHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowItem> HandleAsync(CreateWeeklyCashflowItem command, CancellationToken cancellationToken)
    {
        var entity = new WeeklyCashflowItemEntity
        {
            WeeklyCashflowItemId = Guid.NewGuid().ToString("N"),
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };
        WeeklyCashflowItemDetailsRules.Apply(entity, command.Details);

        context.WeeklyCashflowItems.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
