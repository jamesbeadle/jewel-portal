using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Hs.Commands;

public sealed class RecordAttendanceForHsRecordHandler : ICommandHandler<RecordAttendanceForHsRecord, HsRecordAttendance>
{
    private readonly JpmsContext context;
    public RecordAttendanceForHsRecordHandler(JpmsContext context) { this.context = context; }

    public async Task<HsRecordAttendance> HandleAsync(RecordAttendanceForHsRecord command, CancellationToken cancellationToken)
    {
        var entity = new HsRecordAttendanceEntity
        {
            HsRecordAttendanceId = HsIdentifierFactory.NextHsRecordAttendanceId(),
            HsRecordId = command.HsRecordId,
            AttendeeName = command.AttendeeName,
            SignatureBlobRef = command.SignatureBlobRef,
            SignedAt = DateTimeOffset.UtcNow
        };
        context.HsRecordAttendance.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
