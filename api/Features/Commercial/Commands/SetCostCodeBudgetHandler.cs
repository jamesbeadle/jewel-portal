using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCodeBudgetHandler : ICommandHandler<SetCostCodeBudget, CostCodeBudget>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public SetCostCodeBudgetHandler(JpmsContext context, AuditTrail audit)
    { this.context = context; this.audit = audit; }

    public async Task<CostCodeBudget> HandleAsync(SetCostCodeBudget command, CancellationToken cancellationToken)
    {
        var entity = await context.CostCodeBudgets.FirstOrDefaultAsync(
            budget => budget.ProjectId == command.ProjectId && budget.CostCode == command.CostCode, cancellationToken);

        var created = entity is null;
        var previousAllocated = entity?.AllocatedAmount;
        var previousSpent = entity?.SpentAmount;

        entity ??= AddNewBudget(command);
        entity.AllocatedAmount = command.AllocatedAmount;
        // Null means "leave the recorded spend alone" (2026-08-29) — a caller raising an
        // allocation must not be able to clobber a spent figure it never read.
        entity.SpentAmount = command.SpentAmount ?? entity.SpentAmount;

        await context.SaveChangesAsync(cancellationToken);

        // Budget moves are money statements the Financials tab reads as truth, so each one is a
        // matter of record — before → after, after the save, best-effort per the AuditTrail
        // convention. The actor comes from AuditActor, set by the HTTP endpoint and the MCP
        // endpoint alike.
        await audit.WriteAsync(
            AuditEventType.CostCodeBudgetSet,
            created
                ? $"{entity.CostCode} budget created: allocated £{entity.AllocatedAmount:N2}, spent £{entity.SpentAmount:N2}"
                : $"{entity.CostCode} budget changed: allocated £{previousAllocated:N2} → £{entity.AllocatedAmount:N2}, "
                  + $"spent £{previousSpent:N2} → £{entity.SpentAmount:N2}",
            projectId: command.ProjectId,
            cancellationToken: cancellationToken);

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
