using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>
/// Opens an estimate on a lead, Received. The EST-#### reference is minted server-side and the
/// lead's timeline records it. CreatedByEmail is stamped by the server.
/// </summary>
public sealed record CreateEstimate(
    string LeadId,
    string Scope,
    string ArchitectName,
    DateOnly? PriceDueOn,
    decimal? BudgetMentioned,
    decimal? Total,
    string Notes,
    string CreatedByEmail = "") : ICommand<LeadEstimate>;

/// <summary>
/// Rewrites an estimate's details — scope, architect, due date, budget, total, notes. The whole
/// record is applied as supplied; the status is NOT here (MoveEstimateStatus). A Won or Lost
/// estimate is history and is refused.
/// </summary>
public sealed record UpdateEstimateDetails(
    string EstimateId,
    string Scope,
    string ArchitectName,
    DateOnly? PriceDueOn,
    decimal? BudgetMentioned,
    decimal? Total,
    string Notes,
    // The client-facing narrative (2026-09-15) — see LeadEstimate.
    string ExecutiveSummary = "",
    string BuildTime = "",
    string Exclusions = "") : ICommand<LeadEstimate>;

/// <summary>
/// Replaces an estimate's priced breakdown with the sections given, in order (2026-09-15, the
/// tender's shape). A full-record write: every section and line is applied as supplied and
/// anything not listed is gone. Line totals are quantity × unit price, computed here; when the
/// breakdown has lines the estimate's Total becomes their sum, and when it is emptied the Total
/// is left as it was. Cost codes, where given, must be in the cost-centre master. A Won or Lost
/// estimate is history and is refused. Writes an Estimate activity on the lead's timeline.
/// ChangedByEmail is stamped by the server.
/// </summary>
public sealed record SetEstimateBreakdown(
    string EstimateId,
    IReadOnlyList<EstimateBreakdownSection> Sections,
    string ChangedByEmail = "") : ICommand<LeadEstimate>;

/// <summary>
/// Moves an estimate along its ladder. Submitted needs a Total and stamps SubmittedAt; Won and
/// Lost close it. Writes an activity on the lead's timeline, with the note if one is given.
/// ChangedByEmail is stamped by the server.
/// </summary>
public sealed record MoveEstimateStatus(
    string EstimateId,
    EstimateStatus Status,
    string? Note,
    string ChangedByEmail = "") : ICommand<LeadEstimate>;

/// <summary>One estimate by id or by its EST-#### reference.</summary>
public sealed record GetEstimate(string EstimateId) : IQuery<LeadEstimate?>;
