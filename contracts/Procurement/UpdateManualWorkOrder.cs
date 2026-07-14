using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Edits a work order that was raised directly in JPMS (no bid package, no seed source,
/// not a variation instruction). The whole editable surface travels together — supplier,
/// title, scope and the priced lines — mirroring what CreateManualWorkOrder captured.
/// The order's value is recomputed as the sum of its lines. Orders that came from a
/// tender award, a variation, or a Buildertrend seed are refused: their value and lines
/// are owned by their source flow.
/// </summary>
public sealed record UpdateManualWorkOrder(
    string ProjectId,
    string WorkOrderId,
    string SubcontractorId,
    string Title,
    string Scope,
    IReadOnlyList<UpdatedManualWorkOrderLine> Lines) : ICommand<WorkOrder>;

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
    decimal Amount);
