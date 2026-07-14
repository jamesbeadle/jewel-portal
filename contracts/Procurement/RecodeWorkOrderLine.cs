using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Re-codes one priced work-order line across cost centres. A single part simply moves
/// the line to another centre (quantity and rate untouched); several parts split the
/// line by £ amount — the original line becomes the first part and new lines are added
/// for the rest, so an order priced under one code (e.g. a groundworker's whole package
/// under SUB-GWK) can be spread over the centres the work actually belongs to. Parts
/// must total the line exactly: this reshapes where committed value sits, it never
/// changes the order's value. PaidToDate follows the split pro-rata. Everything reading
/// work-order lines — committed by centre, invoice re-attribution, reconciliation
/// packages — follows automatically.
/// </summary>
public sealed record RecodeWorkOrderLine(
    string ProjectId,
    string WorkOrderLineId,
    IReadOnlyList<WorkOrderLinePart> Parts) : ICommand<IReadOnlyList<WorkOrderLine>>;

/// <summary>One cost centre's share of the line, signed like the line's total.</summary>
public sealed record WorkOrderLinePart(string CostCode, decimal Amount);
