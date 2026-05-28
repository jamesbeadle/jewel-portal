using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListContraChargesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListContraChargesForProject, IReadOnlyList<ContraCharge>> handler;

    public ListContraChargesForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListContraChargesForProject, IReadOnlyList<ContraCharge>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListContraChargesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/contra-charges")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var contraCharges = await handler.HandleAsync(new ListContraChargesForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(contraCharges);
    }
}
