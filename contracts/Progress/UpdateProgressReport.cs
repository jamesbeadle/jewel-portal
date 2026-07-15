using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Replaces a progress report's narrative sections and selected updates.</summary>
public sealed record UpdateProgressReport(
    string ProgressReportId,
    string Title,
    DateTimeOffset? PeriodStart,
    DateTimeOffset? PeriodEnd,
    string Introduction,
    string WorkCompleted,
    string UpcomingWorks,
    IReadOnlyList<string> SelectedUpdateIds) : ICommand<ProgressReport>;
