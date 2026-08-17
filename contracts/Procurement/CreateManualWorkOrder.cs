using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Raises a work order directly — no bid package, no tender — for commitments made
/// outside the tendering flow (a sub engaged on a call, legacy paperwork, a direct
/// instruction). Released immediately with the next sequential per-project number,
/// like awarded and variation orders — unless SaveAsDraft is set, in which case the
/// order is stored unnumbered with WorkOrderStatus.Draft: editable, counted in the
/// Financials tab's committed figures (the commitment is intended), but invisible to
/// the supplier and unlinkable until ApproveWorkOrder mints its number — or
/// RejectWorkOrder ends it. Each line carries its own cost centre and £ amount; the
/// order's value is their sum. The Financials tab, WO allocation and reconciliation
/// packages treat an approved order exactly like any other order.
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
    string ProgrammeNotes = "",
    // Save unissued: status Draft, no number minted until ApproveWorkOrder.
    bool SaveAsDraft = false,
    // Deposit the supplier requires — a percentage of the order value only, printed at the
    // foot of the purchase order. DepositPercent travels null unless DepositRequired.
    bool DepositRequired = false,
    decimal? DepositPercent = null,
    // The raise guardrail: a line's cost centre with no priced valuation report line means
    // committing cost against a centre with no matching sale (contract or variation). The
    // HTTP endpoint refuses such an order unless this is true — the raise dialog sets it after
    // the user has confirmed the warning, and the override lands in the audit trail. Internal
    // delegations (the triage raise-from-email) call the handler directly and bypass the gate
    // by design (scope decision 2026-08-17).
    bool UncoveredCostCentresAcknowledged = false) : ICommand<WorkOrder>;

/// <summary>One priced line: its cost centre, what it covers, and its £ amount. Description
/// is the longer detail printed in the purchase order's Description column — optional, so the
/// title can stay a short label instead of carrying the whole scope (titles cap at 256).</summary>
public sealed record ManualWorkOrderLine(string CostCode, string Title, decimal Amount, string Description = "");
