using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Attaches a VOQ to the request (RFI) it belongs to. Every VOQ created through the normal flow
/// (CreateVoqFromRfq) is born linked; this exists to repair records that predate the link — e.g.
/// seeded variation data whose RequestId never pointed at a real request — so the register can
/// navigate Request → RFI → VOQ → VO across all of them. The request must belong to the same
/// project, and a request can carry at most one VOQ.
/// </summary>
public sealed record LinkVoqToRequest(
    string VariationOrderQuoteId,
    string RequestId) : ICommand<VariationOrderQuote>;
