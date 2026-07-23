using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Raises a work order directly — no bid package, no tender — for commitments made
/// outside the tendering flow (a sub engaged on a call, legacy paperwork, a direct
/// instruction). Released immediately with the next sequential per-project number,
/// like awarded and variation orders. Each line carries its own cost centre and £
/// amount; the order's value is their sum. The Financials tab, WO allocation and
/// reconciliation packages treat it exactly like any other order.
/// </summary>
public sealed record CreateManualWorkOrder(
    string ProjectId,
    string SubcontractorId,
    string Title,
    string Scope,
    string RaisedByEmail,
    IReadOnlyList<ManualWorkOrderLine> Lines,
    // Programme information for the printed purchase order — all optional. TargetCompletion
    // lands on WorkOrder.ScheduledCompletion; the PO's Programme section renders when any is set.
    DateTimeOffset? ProgrammeStart = null,
    DateTimeOffset? TargetCompletion = null,
    string ProgrammeNotes = "") : ICommand<WorkOrder>;

/// <summary>One priced line: its cost centre, what it covers, and its £ amount.</summary>
public sealed record ManualWorkOrderLine(string CostCode, string Title, decimal Amount);
