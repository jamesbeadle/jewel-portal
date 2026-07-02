using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Architects;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Architects.Queries;

public sealed class ListArchitectsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListArchitects, IReadOnlyList<Architect>> handler;

    public ListArchitectsEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListArchitects, IReadOnlyList<Architect>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListArchitects))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "architects")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var architects = await handler.HandleAsync(new ListArchitects(), request.HttpContext.RequestAborted);
        return new OkObjectResult(architects);
    }
}
