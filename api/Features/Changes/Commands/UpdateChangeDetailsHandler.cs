using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class UpdateChangeDetailsHandler : ICommandHandler<UpdateChangeDetails, ChangeRecord>
{
    private readonly JpmsContext context;
    public UpdateChangeDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<ChangeRecord> HandleAsync(UpdateChangeDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.ChangeRecords.FindAsync(new object[] { command.ChangeRecordId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Change record {command.ChangeRecordId} not found.");

        entity.Reference = command.Reference;
        entity.Title = command.Title;
        entity.Description = command.Description;
        entity.Status = (int)command.Status;
        entity.Value = command.Value;
        entity.ResponseText = command.ResponseText;
        entity.RespondedByEmail = command.RespondedByEmail;
        entity.ImpliesVariation = command.ImpliesVariation;
        if (entity.RespondedAt is null && !string.IsNullOrWhiteSpace(command.ResponseText)) entity.RespondedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
