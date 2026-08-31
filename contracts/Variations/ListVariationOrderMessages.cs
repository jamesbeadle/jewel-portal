using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>GET /api/variation-orders/{voId}/messages — the order's in-app conversation, oldest
/// first. Internal and shared messages together; the client portal has its own scoped read that
/// only ever returns the shared thread.</summary>
public sealed record ListVariationOrderMessages(string VariationOrderId)
    : IQuery<IReadOnlyList<VariationOrderMessage>>;
