using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Retention;

// Records that a release milestone actually happened (the schedule only ever forecasts —
// money never moves by itself). The amount is frozen on the record; the confirmation
// timestamp is set server-side.
public sealed record ConfirmRetentionRelease(
    string ProjectId,
    RetentionMilestone Milestone,
    decimal Amount) : ICommand<ProjectRetention>;
