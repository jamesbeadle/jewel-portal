using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

public sealed record RecordAttendanceForHsRecord(
    string HsRecordId,
    string AttendeeName,
    string SignatureBlobRef) : ICommand<HsRecordAttendance>;
