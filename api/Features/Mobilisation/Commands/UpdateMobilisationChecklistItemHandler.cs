using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Mobilisation.Commands;

public sealed class UpdateMobilisationChecklistItemHandler : ICommandHandler<UpdateMobilisationChecklistItem, MobilisationItem>
{
    private readonly JpmsContext context;
    public UpdateMobilisationChecklistItemHandler(JpmsContext context) { this.context = context; }

    public async Task<MobilisationItem> HandleAsync(UpdateMobilisationChecklistItem command, CancellationToken cancellationToken)
    {
        var entity = await context.MobilisationItems.FindAsync(new object[] { command.MobilisationItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Mobilisation item {command.MobilisationItemId} not found.");
        entity.Description = command.Description;
        entity.OwnerEmail = command.OwnerEmail;
        var wasCompleted = entity.IsComplete;
        entity.IsComplete = command.IsComplete;
        if (!wasCompleted && command.IsComplete) entity.CompletedAt = DateTimeOffset.UtcNow;
        if (wasCompleted && !command.IsComplete) entity.CompletedAt = null;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
