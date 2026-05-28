using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record SetCostCodeBudget(
    string ProjectId,
    string CostCode,
    decimal AllocatedAmount,
    decimal SpentAmount) : ICommand<CostCodeBudget>;
