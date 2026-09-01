using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SetCostCodeBudgetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly Audit.AuditActor auditActor;
    private readonly SetCostCodeBudgetAuthorisation authorisation;
    private readonly SetCostCodeBudgetValidation validation;
    private readonly ICommandHandler<SetCostCodeBudget, CostCodeBudget> handler;

    public SetCostCodeBudgetEndpoint(
        SignedInUserResolver users,
        Audit.AuditActor auditActor,
        SetCostCodeBudgetAuthorisation authorisation,
        SetCostCodeBudgetValidation validation,
        ICommandHandler<SetCostCodeBudget, CostCodeBudget> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetCostCodeBudget))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/cost-code-budgets")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email; // the trail records who moved the budget

        var command = await request.ReadFromJsonAsync<SetCostCodeBudget>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var budget = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(budget);
    }
}
