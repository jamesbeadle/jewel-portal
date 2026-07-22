using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

public sealed class ListDirectoryUsersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListDirectoryUsers, IReadOnlyList<DirectoryUser>> handler;

    public ListDirectoryUsersEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListDirectoryUsers, IReadOnlyList<DirectoryUser>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListDirectoryUsers))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "directory")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        var directoryUsers = await handler.HandleAsync(new ListDirectoryUsers(), request.HttpContext.RequestAborted);
        return new OkObjectResult(directoryUsers);
    }
}
