using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class LogHsRecordHandler : ICommandHandler<LogHsRecord, HsRecord>
{
    private readonly JpmsContext context;
    public LogHsRecordHandler(JpmsContext context) { this.context = context; }

    public async Task<HsRecord> HandleAsync(LogHsRecord command, CancellationToken cancellationToken)
    {
        var entity = new HsRecordEntity
        {
            HsRecordId = HsIdentifierFactory.NextHsRecordId(),
            ProjectId = command.ProjectId,
            Kind = (int)command.Kind,
            Summary = command.Summary,
            Severity = (int)command.Severity,
            Status = (int)HsStatus.Open,
            AssignedToEmail = command.AssignedToEmail,
            RaisedAt = DateTimeOffset.UtcNow,
            DueAt = command.DueAt,
            ClosedAt = null
        };
        context.HsRecords.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
