using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Retention;

// Adds deposit & retention terms to a project, or updates them (upsert — one record per
// project). Percentages are whole numbers (5 means 5%), matching
// ValuationClaim.RetentionPercent. DepositPercent is the cash-up-front deposit (e.g. 20);
// 0 means no deposit — trailing default keeps existing positional callers compiling.
public sealed record SetProjectRetention(
    string ProjectId,
    decimal RetentionPercent,
    decimal CompletionReleasePercent,
    int DefectsPeriodMonths,
    DateTimeOffset? PracticalCompletionAt,
    decimal DepositPercent = 0m,
    // Deposit releases settled before the portal began deducting them (excluded from
    // future claim deductions). See ProjectRetention.DepositReleasedOpening.
    decimal DepositReleasedOpening = 0m) : ICommand<ProjectRetention>;
