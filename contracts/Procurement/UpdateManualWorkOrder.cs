using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Edits a work order. The whole editable surface travels together — supplier, title,
/// scope and the priced lines — mirroring what CreateManualWorkOrder captured. The
/// order's value is recomputed as the sum of its lines.
///
/// <para>Who may edit WHAT is two gates (2026-08-21, the accountant's add-a-line flow):
/// orders raised directly in JPMS (no bid package, no seed source, not a variation
/// instruction) stay editable by everyone the authorisation admits, as before; orders
/// owned by a source flow — a tender award, a variation instruction, a Buildertrend
/// seed — and any RELEASED order's correction are a directors' decision, admitted only
/// when the endpoint has stamped <see cref="EditorMayEditAnyOrder"/> from the signed-in
/// user's roles (MD / FD / Admin). Rejected and Cancelled orders are terminal records
/// and never editable. Saving an edit NEVER re-emails the supplier — the updated
/// purchase order is downloaded and sent from the PO page by hand.</para>
/// </summary>
public sealed record UpdateManualWorkOrder(
    string ProjectId,
    string WorkOrderId,
    string SubcontractorId,
    string Title,
    string Scope,
    IReadOnlyList<UpdatedManualWorkOrderLine> Lines,
    // Programme information for the printed purchase order — all optional, edited wholesale
    // with the rest. TargetCompletion lands on WorkOrder.ScheduledCompletion.
    DateTimeOffset? ProgrammeStart = null,
    DateTimeOffset? TargetCompletion = null,
    string ProgrammeNotes = "",
    // Deposit the supplier requires — a percentage of the order value only, printed at the
    // foot of the purchase order. DepositPercent travels null unless DepositRequired.
    bool DepositRequired = false,
    decimal? DepositPercent = null,
    // SERVER-SET. The endpoint overwrites this from the signed-in user's roles (MD / FD /
    // Admin) before the handler runs — whatever the client sent is discarded, so it can
    // never be a way to smuggle authority. True admits editing non-manual orders (awarded,
    // variation-instructed, seeded).
    bool EditorMayEditAnyOrder = false) : ICommand<WorkOrder>;

/// <summary>
/// One priced line as edited. WorkOrderLineId ties it to an existing line — preserving
/// its id, so paid-to-date and invoice history stay attached — while null means a brand
/// new line. Existing lines missing from the list are removed, which is only allowed
/// while nothing has been paid against them.
/// </summary>
public sealed record UpdatedManualWorkOrderLine(
    string? WorkOrderLineId,
    string CostCode,
    string Title,
    decimal Amount,
    // The longer detail printed in the purchase order's Description column — optional,
    // so the title can stay a short label instead of carrying the whole scope.
    string Description = "");
