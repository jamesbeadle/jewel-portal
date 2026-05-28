using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Queries;

public sealed class ListBidPackagesForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListBidPackagesForProject, IReadOnlyList<BidPackage>> handler;

    public ListBidPackagesForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListBidPackagesForProject, IReadOnlyList<BidPackage>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListBidPackagesForProject))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/bid-packages")] HttpRequest request,
        string projectId)
    {
        if (users.Resolve(request) is null) return new UnauthorizedResult();
        return new OkObjectResult(await handler.HandleAsync(new ListBidPackagesForProject(projectId), request.HttpContext.RequestAborted));
    }
}
