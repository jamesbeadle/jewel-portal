using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Attaches a variation order to the request (RFI) it belongs to. Every order created through the
/// normal flow (CreateVoqFromRfq) is born linked; this exists to repair records that predate the
/// link — e.g. seeded variation data whose RequestId never pointed at a real request — so the
/// register can navigate Request → RFI → VO across all of them. The request must belong to the
/// same project, and a request can carry at most one variation order.
/// </summary>
public sealed record LinkVoqToRequest(
    string VariationOrderId,
    string RequestId) : ICommand<VariationOrder>;
