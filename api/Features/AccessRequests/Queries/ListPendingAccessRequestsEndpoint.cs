using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.AccessRequests.Queries;

public sealed class ListPendingAccessRequestsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListPendingAccessRequests, IReadOnlyList<AccessRequest>> handler;

    public ListPendingAccessRequestsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListPendingAccessRequests, IReadOnlyList<AccessRequest>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListPendingAccessRequests))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "access-requests")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!JpmsAdministrators.Contains(signedInUser.Email)) return new ForbidResult();

        var requests = await handler.HandleAsync(new ListPendingAccessRequests(), request.HttpContext.RequestAborted);
        return new OkObjectResult(requests);
    }
}
