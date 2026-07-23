using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Un-approves a variation order back to Quoting — for approvals that should never have happened
/// (chiefly seeded history marked Approved when the client never actually approved it). NOT a
/// substitute for RejectVariationOrder: a rejected order is a real decision that stays on the
/// register, while a returned order says "this approval was a data error — it is still being
/// quoted". Reverses whatever the approval actually wrote (valuation line back to a TBC
/// placeholder, approval accrual deleted, budget released) and clears the V-ref so it can be
/// re-minted. Refused when work orders instruct the variation, when its value has been revised,
/// when it is priced as split detail lines, or when value has been claimed against it.
/// </summary>
public sealed record ReturnVariationOrderToQuoting(string VariationOrderId) : ICommand<VariationOrder>;
