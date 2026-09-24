using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Reinstates a rejected variation order — the rejection was made in error, or the client has
/// changed their mind. The order returns to Issued when it had been issued, else to Quoting, and
/// its rejection date is cleared. A variation rejected from Approved comes back UNAPPROVED: the
/// rejection already reversed the approval's valuation lines and budget, so the approval and its
/// rejection offset leave the CVR together and the V-ref is freed, and a person re-approves it
/// through ApproveVariationOrder with the lines as they now stand.
/// </summary>
public sealed record ReinstateVariationOrder(string VariationOrderId) : ICommand<VariationOrder>;
