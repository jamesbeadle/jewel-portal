using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Records the agreed subcontractor (and value) on a quoting variation order — who the works will
/// be instructed to if the variation is approved, and what they have agreed to do it for. Purely
/// quoting-stage data — the order's status does not change (it stays Quoting until issued).
///
/// Bid packages were separated from the VO quoting process on 2026-08-12: a variation order sets
/// the sales side for a cost code, a bid package groups works across cost codes by trade — they
/// are different entities, and the tender itself now runs (and is awarded) entirely on the bid
/// package. The command keeps its historic name (persisted API surface, like the /voqs/ routes);
/// it no longer carries a BidPackageId.
/// </summary>
public sealed record SelectVoqTender(
    string VariationOrderId,
    string SubcontractorId,
    decimal? EstimatedValue = null) : ICommand<VariationOrder>;
