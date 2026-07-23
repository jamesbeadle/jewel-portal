using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Rejects a variation order. From Quoting or Issued this is a plain status move (the client
/// declined; nothing commercial was ever written). From APPROVED it is a real commercial event
/// that reverses the approval's writes: the Variation line comes off the Valuation Report (with
/// its claim entries), an offsetting omit accrual lands on the CVR, and the committed value is
/// released from the cost-centre budget. The V-ref, once minted, stays on the record as an audit
/// fact — its number is not re-used.
/// </summary>
public sealed record RejectVariationOrder(string VariationOrderId) : ICommand<VariationOrder>;
