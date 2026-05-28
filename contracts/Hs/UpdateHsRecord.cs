using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

public sealed record UpdateHsRecord(
    string HsRecordId,
    string Summary,
    HsSeverity Severity,
    HsStatus Status,
    string AssignedToEmail,
    DateTimeOffset? DueAt) : ICommand<HsRecord>;
