using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Hs;

public sealed record LogHsRecord(
    string ProjectId,
    HsRecordKind Kind,
    string Summary,
    HsSeverity Severity,
    string AssignedToEmail,
    DateTimeOffset? DueAt) : ICommand<HsRecord>;
