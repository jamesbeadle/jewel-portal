using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Records a touch on a lead — a call, an email, a brochure posted, a meeting, a note. OccurredAt
/// defaults to now. RecordedByEmail is stamped by the server. Does not move the stage: that is
/// a decision (MoveLeadStage), not a record of contact.
/// </summary>
public sealed record LogLeadActivity(
    string LeadId,
    LeadActivityKind Kind,
    string Summary,
    DateTimeOffset? OccurredAt,
    string RecordedByEmail = "") : ICommand<LeadActivity>;
