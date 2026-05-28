using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCodeBudgetHandler : ICommandHandler<SetCostCodeBudget, CostCodeBudget>
{
    private readonly JpmsContext context;

    public SetCostCodeBudgetHandler(JpmsContext context) { this.context = context; }

    public async Task<CostCodeBudget> HandleAsync(SetCostCodeBudget command, CancellationToken cancellationToken)
    {
        var entity = await context.CostCodeBudgets.FirstOrDefaultAsync(
            budget => budget.ProjectId == command.ProjectId && budget.CostCode == command.CostCode, cancellationToken);

        entity ??= AddNewBudget(command);
        entity.AllocatedAmount = command.AllocatedAmount;
        entity.SpentAmount = command.SpentAmount;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private CostCodeBudgetEntity AddNewBudget(SetCostCodeBudget command)
    {
        var entity = new CostCodeBudgetEntity
        {
            CostCodeBudgetId = CommercialIdentifierFactory.NextCostCodeBudgetId(),
            ProjectId = command.ProjectId,
            CostCode = command.CostCode
        };
        context.CostCodeBudgets.Add(entity);
        return entity;
    }
}
