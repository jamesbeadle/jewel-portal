using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Re-states a PRE-approval variation order's estimate — the quoting-stage figure the register,
/// the VO document and the valuation export's Pending tab read. Null (or zero) says the order is
/// currently unpriced, which is a real statement: the export's Pending tab lists only orders
/// carrying a figure, so this is how an order put into abeyance is taken off it without touching
/// its status or history.
///
/// Refused once a build-up is staged — the staged lines' total IS the estimate (clear the staging
/// first, via StageVariationOrderBuildUp with an empty list, if the figure must go). Refused on
/// Approved orders (ReviseVariationOrderValue owns the approved figure and its downstream writes)
/// and on Rejected ones (a decided order's last quoted figure is part of its record).
/// </summary>
public sealed record SetVariationOrderEstimate(string VariationOrderId, decimal? EstimatedValue) : ICommand<VariationOrder>;
