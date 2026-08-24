using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Deletes a draft work order outright — undecided, or already rejected. Both are drafts
/// that never became orders: no number was ever minted (approval is what mints one),
/// nothing went to the supplier, so removing the record leaves no gap anywhere. The
/// order's priced lines and attachment rows (blobs included) go with it; an undecided
/// draft's value stops counting in the Financials tab's committed figures, a rejected
/// one counted nowhere already. Rejecting remains the way to record a considered "no" —
/// deletion is for drafts that should never have existed (raised in error, duplicated),
/// and the audit trail keeps the surviving note. A live order is never deletable: it
/// carries a minted number the supplier has seen, so it is cancelled instead. Open to
/// the same roles that may approve or reject drafts.
/// </summary>
public sealed record DeleteDraftWorkOrder(
    string ProjectId,
    string WorkOrderId) : ICommand<Acknowledgement>;
