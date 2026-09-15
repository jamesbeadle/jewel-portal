using Jewel.JPMS.Contracts.Hs;

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
        entity.AssignedToName = command.AssignedToName;
        entity.DueAt = command.DueAt;
        if (command.Status == HsStatus.Closed && entity.ClosedAt is null) entity.ClosedAt = DateTimeOffset.UtcNow;
        await StampAuditItemAsync(entity.HsRecordId, entity.ClosedAt, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // A corrective action minted from an audit item is that item's "date rectified" when it closes
    // (and not when it reopens): the audit sheet reads the register's answer.
    private async Task StampAuditItemAsync(string hsRecordId, DateTimeOffset? closedAt, CancellationToken cancellationToken)
    {
        var item = await context.HsAuditItems.FirstOrDefaultAsync(row => row.HsRecordId == hsRecordId, cancellationToken);
        if (item is null) return;
        item.DateRectified = closedAt is { } closed ? new DateTimeOffset(closed.UtcDateTime.Date, TimeSpan.Zero) : null;
    }
}
