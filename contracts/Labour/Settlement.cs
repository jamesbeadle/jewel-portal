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
public sealed record SetXeroLineTimesheetCover(
    string XeroLedgerLineId,
    bool IsCovered,
    string ProjectId,
    string SubcontractorId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd) : ICommand<Acknowledgement>;

/// <summary>
/// Posts an accepted invoice-vs-timesheet difference as a visible settlement variance against
/// the cost code, so posted cost of sales equals cash paid and nothing is silently absorbed.
/// </summary>
public sealed record AddLabourSettlementVariance(
    string ProjectId,
    string CostCode,
    string SubcontractorId,
    decimal Amount,
    string Reason,
    string? XeroLedgerLineId) : ICommand<LabourSettlementVariance>;
