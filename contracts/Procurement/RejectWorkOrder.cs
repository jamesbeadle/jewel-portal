using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Rejects a draft work order: terminal, like cancelling a live order. The draft keeps
/// no number (none was ever minted) and from this point counts nowhere — it drops out
/// of the committed figures it counted in while a draft, and it can never be invoiced,
/// packaged, emailed or accepted. There is no un-reject; raise a fresh order instead.
/// Open to the same roles that may raise orders directly.
/// </summary>
public sealed record RejectWorkOrder(
    string ProjectId,
    string WorkOrderId) : ICommand<WorkOrder>;
