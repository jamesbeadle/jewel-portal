using Jewel.JPMS.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>POST /api/auth/logout — revokes the current session and clears the cookie.</summary>
public sealed class LogoutEndpoint
{
    private readonly SessionManager sessions;

    public LogoutEndpoint(SessionManager sessions) { this.sessions = sessions; }

    [Function("AuthLogout")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequest request)
    {
        var secret = SessionCookie.Read(request);
        if (secret is not null)
            await sessions.RevokeAsync(secret, request.HttpContext.RequestAborted);

        SessionCookie.Clear(request.HttpContext.Response);
        return new OkResult();
    }
}
