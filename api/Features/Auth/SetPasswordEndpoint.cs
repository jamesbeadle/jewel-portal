using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// POST /api/auth/set-password — completes an invite (or reset). Validates the single-use token,
/// applies the password policy, stores the hash, marks the account active and signs the user in.
/// </summary>
public sealed class SetPasswordEndpoint
{
    private readonly JpmsContext context;
    private readonly SessionManager sessions;

    public SetPasswordEndpoint(JpmsContext context, SessionManager sessions)
    {
        this.context = context;
        this.sessions = sessions;
    }

    [Function("AuthSetPassword")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/set-password")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        SetPasswordRequest? body;
        try { body = await request.ReadFromJsonAsync<SetPasswordRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }
        if (body is null || string.IsNullOrWhiteSpace(body.Token))
            return new BadRequestObjectResult(new { error = "Missing token." });

        var policyError = PasswordPolicy.Validate(body.Password);
        if (policyError is not null)
            return new BadRequestObjectResult(new { error = policyError });

        var tokenHash = AuthTokens.Hash(body.Token.Trim());
        var now = DateTimeOffset.UtcNow;
        var token = await context.PasswordResetTokens
            .FirstOrDefaultAsync(row => row.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.ConsumedAt is not null || token.ExpiresAt <= now)
            return new BadRequestObjectResult(new { error = "This link is invalid or has expired. Ask an administrator for a new invite." });

        var email = token.Email;
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        if (credential is null)
        {
            credential = new UserCredentialEntity { Email = email, CreatedAt = now };
            context.UserCredentials.Add(credential);
        }

        credential.PasswordHash = PasswordHasher.Hash(body.Password!);
        credential.Status = (int)CredentialStatus.Active;
        credential.PasswordSetAt = now;
        credential.FailedAttempts = 0;
        credential.LockedUntil = null;

        token.ConsumedAt = now;
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
}
