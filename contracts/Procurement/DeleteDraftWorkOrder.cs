using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Deletes a draft work order outright — for drafts raised in error or duplicated by the
/// assistant, where even a Rejected row would just be noise. Only drafts can be deleted:
/// no number was ever minted (approval is what mints one), nothing went to the supplier,
/// so removing the record leaves no gap anywhere. The order's priced lines and attachment
/// rows (blobs included) go with it; its value stops counting in the Financials tab's
/// committed figures. Rejecting remains the way to record a considered "no" — deletion is
/// for drafts that should never have existed, and the audit trail keeps the surviving note.
/// Open to the same roles that may approve or reject drafts.
/// </summary>
public sealed record DeleteDraftWorkOrder(
    string ProjectId,
    string WorkOrderId) : ICommand<Acknowledgement>;
