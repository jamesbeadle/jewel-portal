using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Records the winning bid package + subcontractor (and the agreed value) on a quoting variation
/// order. This is the tender the order will carry into approval. Purely quoting-stage data — the
/// order's status does not change (it stays Quoting until it is issued to the client).
/// </summary>
public sealed record SelectVoqTender(
    string VariationOrderId,
    string BidPackageId,
    string SubcontractorId,
    decimal? EstimatedValue = null) : ICommand<VariationOrder>;
