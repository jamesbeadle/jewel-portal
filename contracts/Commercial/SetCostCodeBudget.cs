using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// SpentAmount is optional (2026-08-29): omitted / null means "leave the recorded spend exactly as
// it is" — so a caller raising an allocation cannot clobber the spent figure it never read. A new
// row with no spent given starts at 0.
public sealed record SetCostCodeBudget(
    string ProjectId,
    string CostCode,
    decimal AllocatedAmount,
    decimal? SpentAmount = null) : ICommand<CostCodeBudget>;
