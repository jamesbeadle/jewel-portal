using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Locks a cost centre down on the Financials tab (or unlocks it). A finalised centre
/// expects no further spend: its remaining drawdown reads as realised profit / loss
/// instead of funds still available. Upserts the centre's progress row.
/// </summary>
public sealed record SetCostCentreFinalisation(
    string ProjectId,
    string CostCode,
    bool IsFinalised) : ICommand<CostCentreCostProgress>;
