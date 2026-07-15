using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Retention;

// Adds retention terms to a project, or updates them (upsert — one record per project).
// Percentages are whole numbers (5 means 5%), matching ValuationClaim.RetentionPercent.
public sealed record SetProjectRetention(
    string ProjectId,
    decimal RetentionPercent,
    decimal CompletionReleasePercent,
    int DefectsPeriodMonths,
    DateTimeOffset? PracticalCompletionAt) : ICommand<ProjectRetention>;
