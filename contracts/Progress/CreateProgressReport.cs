using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// Creates a client-facing progress report: narrative sections plus an ordered selection of
/// progress update ids whose photos illustrate the completed works. The API sets
/// <see cref="CreatedByEmail"/> from the signed-in user; any client-supplied value is ignored.
/// </summary>
public sealed record CreateProgressReport(
    string ProjectId,
    string CreatedByEmail,
    string Title,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    string Introduction,
    string WorkCompleted,
    string UpcomingWorks,
    IReadOnlyList<string> SelectedUpdateIds) : ICommand<ProgressReport>;
