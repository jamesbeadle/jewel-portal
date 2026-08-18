using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

/// <summary>
/// GET /api/directory-revoked — the users whose access has been revoked, most recent first.
/// Unlike the active list (which the wider Directory page reads), this is user administration
/// and stays behind the admin gate with the upsert/remove commands.
/// </summary>
public sealed class ListRevokedDirectoryUsersEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListRevokedDirectoryUsers, IReadOnlyList<RevokedDirectoryUser>> handler;

    public ListRevokedDirectoryUsersEndpoint(
        SignedInUserResolver users,
        IQueryHandler<ListRevokedDirectoryUsers, IReadOnlyList<RevokedDirectoryUser>> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(ListRevokedDirectoryUsers))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "directory-revoked")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        var revokedUsers = await handler.HandleAsync(new ListRevokedDirectoryUsers(), request.HttpContext.RequestAborted);
        return new OkObjectResult(revokedUsers);
    }
}
