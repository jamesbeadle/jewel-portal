using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>Render and return the variation order's official document PDF — the same idempotent
/// regenerate-on-demand arrangement as the request (RFI) document. Null when the order is not found.</summary>
public sealed record GetVariationOrderDocument(string VariationOrderId) : IQuery<VariationDocumentFile?>;
