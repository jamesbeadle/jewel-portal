using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Sales;

/// <summary>Every strategy with its funnel — Active first, then Draft, Paused, Retired; newest
/// first within a status.</summary>
public sealed record ListSalesStrategies : IQuery<IReadOnlyList<SalesStrategyOverview>>;

/// <summary>One strategy with its funnel and the leads it has found.</summary>
public sealed record GetSalesStrategy(string StrategyId) : IQuery<SalesStrategyDetail?>;
