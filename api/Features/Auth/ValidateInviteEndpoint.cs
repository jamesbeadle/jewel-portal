using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// GET /api/auth/invite/{token} — lets the set-password page check a link is still valid and
/// greet the user by name before they choose a password. Never reveals anything for a bad token.
/// </summary>
public sealed class ValidateInviteEndpoint
{
    private readonly JpmsContext context;

    public ValidateInviteEndpoint(JpmsContext context) { this.context = context; }

    [Function("AuthValidateInvite")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/invite/{token}")] HttpRequest request,
        string token)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        if (string.IsNullOrWhiteSpace(token))
            return new OkObjectResult(new InviteValidation(false, null, null));

        var tokenHash = AuthTokens.Hash(token.Trim());
        var now = DateTimeOffset.UtcNow;
        var record = await context.PasswordResetTokens
            .FirstOrDefaultAsync(row => row.TokenHash == tokenHash, cancellationToken);

        if (record is null || record.ConsumedAt is not null || record.ExpiresAt <= now)
            return new OkObjectResult(new InviteValidation(false, null, null));

        var directoryUser = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == record.Email, cancellationToken);
        var displayName = string.IsNullOrWhiteSpace(directoryUser?.DisplayName)
            ? record.Email
            : directoryUser!.DisplayName;

        return new OkObjectResult(new InviteValidation(true, record.Email, displayName));
    }
}
