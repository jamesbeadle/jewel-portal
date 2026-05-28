using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Changes.Queries;

public sealed class GetChangeByIdEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetChangeById, ChangeRecord?> handler;

    public GetChangeByIdEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetChangeById, ChangeRecord?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetChangeById))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "changes/{changeRecordId}")] HttpRequest request,
        string changeRecordId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var change = await handler.HandleAsync(new GetChangeById(changeRecordId), request.HttpContext.RequestAborted);
        return new OkObjectResult(change);
    }
}
