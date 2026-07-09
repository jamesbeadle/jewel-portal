using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Sets the cost-side completion percentage for one cost centre on one project —
/// the commercial team's view of how far through the *cost* of the work we are,
/// edited inline on the Financials tab. Distinct from the sales-side completion,
/// which comes from the latest claim on the valuation report. Upserts.
/// </summary>
public sealed record SetCostCentreCostCompletion(
    string ProjectId,
    string CostCode,
    decimal CostCompletionPercent) : ICommand<CostCentreCostProgress>;
