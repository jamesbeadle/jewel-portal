using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Cancels a released work order: terminal, the voiding counterpart of RejectWorkOrder for
/// an order that has already been issued to the supplier. The order keeps its minted number
/// and stays on the page as a record — a purchase order went out under that reference, so it
/// is voided, never erased — but from this point its value counts nowhere: it leaves the
/// issued totals, the Financials tab's committed figures, the WO Allocation tab and the
/// supplier's portal. Only possible while nothing has been invoiced or paid against it
/// (the handler refuses otherwise — re-code or unlink the bills first), so money already
/// recorded is never touched. There is no un-cancel; raise a fresh order instead.
/// Directors and the Finance Director only — voiding an issued commitment is a money
/// decision, not a raise-an-order one.
/// </summary>
public sealed record CancelWorkOrder(
    string ProjectId,
    string WorkOrderId) : ICommand<WorkOrder>;
