using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class UpdateHsRecordHandler : ICommandHandler<UpdateHsRecord, HsRecord>
{
    private readonly JpmsContext context;
    public UpdateHsRecordHandler(JpmsContext context) { this.context = context; }

    public async Task<HsRecord> HandleAsync(UpdateHsRecord command, CancellationToken cancellationToken)
    {
        var entity = await context.HsRecords.FindAsync(new object[] { command.HsRecordId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"HS record {command.HsRecordId} not found.");
        entity.Summary = command.Summary;
        entity.Severity = (int)command.Severity;
        entity.Status = (int)command.Status;
        entity.AssignedToEmail = command.AssignedToEmail;
        entity.DueAt = command.DueAt;
        if (command.Status == HsStatus.Closed && entity.ClosedAt is null) entity.ClosedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
