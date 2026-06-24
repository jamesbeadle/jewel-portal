using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// POST /api/auth/login — verifies email + password, opens a session and sets the session cookie.
/// All failure modes return the same generic 401 to avoid revealing which emails exist.
/// </summary>
public sealed class LoginEndpoint
{
    private readonly JpmsContext context;
    private readonly SessionManager sessions;

    public LoginEndpoint(JpmsContext context, SessionManager sessions)
    {
        this.context = context;
        this.sessions = sessions;
    }

    [Function("AuthLogin")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        LoginRequest? body;
        try { body = await request.ReadFromJsonAsync<LoginRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }
        if (body is null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrEmpty(body.Password))
            return Unauthorized();

        var email = body.Email.Trim();
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);

        if (credential is null
            || credential.Status != (int)CredentialStatus.Active
            || string.IsNullOrEmpty(credential.PasswordHash))
            return Unauthorized();

        var now = DateTimeOffset.UtcNow;
        if (credential.LockedUntil is { } lockedUntil && lockedUntil > now)
            return Unauthorized();

        if (!PasswordHasher.Verify(body.Password, credential.PasswordHash))
        {
            credential.FailedAttempts += 1;
            if (credential.FailedAttempts >= AuthLockout.MaxFailedAttempts)
            {
                credential.LockedUntil = now.Add(AuthLockout.LockoutDuration);
                credential.FailedAttempts = 0;
            }
            await context.SaveChangesAsync(cancellationToken);
            return Unauthorized();
        }

        credential.FailedAttempts = 0;
        credential.LockedUntil = null;
        await context.SaveChangesAsync(cancellationToken);

        var secret = await sessions.CreateAsync(email, cancellationToken);
        SessionCookie.Set(request.HttpContext.Response, secret);

        var roles = await UserRoles.ForAsync(context, email, cancellationToken);
        var displayName = await ResolveDisplayNameAsync(email, cancellationToken);
        return new OkObjectResult(new AuthenticatedUserResponse(email, displayName, roles));
    }

    private async Task<string> ResolveDisplayNameAsync(string email, CancellationToken cancellationToken)
    {
        var directoryUser = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        return string.IsNullOrWhiteSpace(directoryUser?.DisplayName) ? email : directoryUser!.DisplayName;
    }

    private static UnauthorizedObjectResult Unauthorized() =>
        new(new { error = "Incorrect email or password." });
}
