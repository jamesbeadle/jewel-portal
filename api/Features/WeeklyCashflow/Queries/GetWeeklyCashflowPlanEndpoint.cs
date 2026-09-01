using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Queries;

public sealed class GetWeeklyCashflowPlanEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetWeeklyCashflowPlan, WeeklyCashflowPlan> handler;

    public GetWeeklyCashflowPlanEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetWeeklyCashflowPlan, WeeklyCashflowPlan> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetWeeklyCashflowPlan))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weekly-cashflow/plan")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(StatusCodes.Status403Forbidden);

        var plan = await handler.HandleAsync(new GetWeeklyCashflowPlan(), request.HttpContext.RequestAborted);
        return new OkObjectResult(plan);
    }
}
