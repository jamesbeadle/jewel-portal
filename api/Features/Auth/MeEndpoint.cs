using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>GET /api/auth/me — returns the current signed-in user (from the session cookie), or 401.</summary>
public sealed class MeEndpoint
{
    private readonly SignedInUserResolver users;

    public MeEndpoint(SignedInUserResolver users) { this.users = users; }

    [Function("AuthMe")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        return new OkObjectResult(new AuthenticatedUserResponse(
            signedInUser.Email, signedInUser.DisplayName, signedInUser.Roles));
    }
}
