using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Contracts.Auth;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// POST /api/auth/set-password — completes an invite (or reset). Validates the single-use token,
/// applies the password policy and hands the password to PasswordSetter — which consumes this link
/// with every other live one, stores the hash, marks the account active and ends every session and
/// connected tool the person had (a reset is the answer to "someone else has my password", so
/// nothing opened before it survives) — then signs the user in.
/// </summary>
public sealed class SetPasswordEndpoint
{
    private readonly JpmsContext context;
    private readonly SessionManager sessions;
    private readonly PasswordSetter passwords;

    public SetPasswordEndpoint(JpmsContext context, SessionManager sessions, PasswordSetter passwords)
    {
        this.context = context;
        this.sessions = sessions;
        this.passwords = passwords;
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
        await passwords.SetAsync(email, body.Password!, cancellationToken);

        var secret = await sessions.CreateAsync(email, cancellationToken);
        SessionCookie.Set(request.HttpContext.Response, secret);

        var directoryRoles = await UserRoles.DirectoryRolesAsync(context, email, cancellationToken);
        var roles = UserRoles.Expand(directoryRoles);
        var directoryUser = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        var displayName = string.IsNullOrWhiteSpace(directoryUser?.DisplayName) ? email : directoryUser!.DisplayName;
        return new OkObjectResult(new AuthenticatedUserResponse(email, displayName, roles, directoryUser?.SubcontractorId,
            HomeRoleSelection.From(directoryRoles), directoryUser?.RevertToOwnRole ?? false, directoryUser?.ClientId,
            directoryUser?.ArchitectId));
    }
}
