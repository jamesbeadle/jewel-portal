using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Queries;

public sealed class ListDayworksForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDayworksForProject, IReadOnlyList<Daywork>> handler;

    public ListDayworksForProjectEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListDayworksForProject, IReadOnlyList<Daywork>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListDayworksForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/dayworks")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var dayworks = await handler.HandleAsync(new ListDayworksForProject(projectId), request.HttpContext.RequestAborted);
        return new OkObjectResult(dayworks);
    }
}
