using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Directory.Queries;

public sealed class GetDirectoryUserEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetDirectoryUser, DirectoryUser?> handler;

    public GetDirectoryUserEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetDirectoryUser, DirectoryUser?> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(GetDirectoryUser))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "directory/{email}")] HttpRequest request,
        string email)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var isOwnEntry = string.Equals(signedInUser.Email, email, StringComparison.OrdinalIgnoreCase);
        if (!isOwnEntry && !JpmsAdministrators.Contains(signedInUser.Email)) return new ForbidResult();

        var directoryUser = await handler.HandleAsync(new GetDirectoryUser(email), request.HttpContext.RequestAborted);
        return new OkObjectResult(directoryUser);
    }
}
