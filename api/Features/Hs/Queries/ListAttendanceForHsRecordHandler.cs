using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Hs.Queries;

public sealed class ListAttendanceForHsRecordHandler : IQueryHandler<ListAttendanceForHsRecord, IReadOnlyList<HsRecordAttendance>>
{
    private readonly JpmsContext context;
    public ListAttendanceForHsRecordHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<HsRecordAttendance>> HandleAsync(ListAttendanceForHsRecord query, CancellationToken cancellationToken)
    {
        var entities = await context.HsRecordAttendance.Where(attendance => attendance.HsRecordId == query.HsRecordId).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
