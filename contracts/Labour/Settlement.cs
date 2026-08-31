using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>Invoiced vs approved per subcontractor for the settlement reconciliation view
/// (docs/Labour-Time-Tracking-Scope.md §6).</summary>
public sealed record ListLabourSettlementForProject(string ProjectId)
    : IQuery<IReadOnlyList<LabourSettlementRow>>;

/// <summary>
/// Marks (or unmarks) a Xero purchase line as settlement of approved timesheets. Covered lines
/// are excluded from the cost-of-sales aggregation — the approved timesheet is the actual, the
/// invoice is settlement of it.
/// </summary>
// CreatedByEmail (2026-08-31): stamped server-side when the command arrives through the
// connector's action gateway; the HTTP endpoint keeps stamping via the handler's explicit
// overload, so a portal click and a connector call record the same actor the same way.
public sealed record SetXeroLineTimesheetCover(
    string XeroLedgerLineId,
    bool IsCovered,
    string ProjectId,
    string SubcontractorId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    string CreatedByEmail = "") : ICommand<Acknowledgement>;

/// <summary>
/// Posts an accepted invoice-vs-timesheet difference as a visible settlement variance against
/// the cost code, so posted cost of sales equals cash paid and nothing is silently absorbed.
/// </summary>
// CreatedByEmail (2026-08-31): same connector stamp convention as SetXeroLineTimesheetCover.
public sealed record AddLabourSettlementVariance(
    string ProjectId,
    string CostCode,
    string SubcontractorId,
    decimal Amount,
    string Reason,
    string? XeroLedgerLineId,
    string CreatedByEmail = "") : ICommand<LabourSettlementVariance>;
