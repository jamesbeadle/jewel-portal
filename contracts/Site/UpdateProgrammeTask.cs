using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

public sealed record UpdateProgrammeTask(
    string ProgrammeTaskId,
    string Title,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd,
    decimal ProgressPercent,
    string? BoqLineItemId) : ICommand<ProgrammeTask>;
