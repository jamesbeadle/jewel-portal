using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

public sealed record AddProgrammeTask(
    string ProjectId,
    string Title,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd,
    string? BoqLineItemId) : ICommand<ProgrammeTask>;
