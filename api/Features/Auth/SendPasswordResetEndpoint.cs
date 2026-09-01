using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// POST /api/auth/send-reset (admin only) — sends a user a password-reset link and hands the link
/// back so the admin can relay it by hand if the email does not arrive. Unlike the public endpoint
/// this one answers honestly: the caller is already an administrator, so there is nothing to leak,
/// and a plain "they've never set a password — re-invite them instead" saves a support round trip.
/// </summary>
public sealed class SendPasswordResetEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PasswordResetSender resets;
    private readonly IConfiguration configuration;

    public SendPasswordResetEndpoint(SignedInUserResolver users, PasswordResetSender resets, IConfiguration configuration)
    {
        this.users = users;
        this.resets = resets;
        this.configuration = configuration;
    }

    [Function("AuthSendPasswordReset")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/send-reset")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        SendPasswordResetRequest? body;
        try { body = await request.ReadFromJsonAsync<SendPasswordResetRequest>(cancellationToken); }
        catch { return new BadRequestResult(); }

        var email = body?.Email?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(email))
            return new BadRequestObjectResult(new { error = "Enter the user's email address." });

        // An admin deliberately clicking "send reset" twice is not abuse, so the anti-flood window
        // that guards the public endpoint does not apply here.
        var result = await resets.SendAsync(
            email, SiteBaseUrl.Resolve(configuration, request), bypassThrottle: true, cancellationToken);

        return result.Outcome switch
        {
            PasswordResetSender.Outcome.Sent => new OkObjectResult(result.Reset),
            PasswordResetSender.Outcome.NoSuchAccount => new BadRequestObjectResult(new
            {
                error = "There's no account for that address yet. Invite them instead."
            }),
            PasswordResetSender.Outcome.NotYetActive => new BadRequestObjectResult(new
            {
                error = "They haven't set a password yet, so there's nothing to reset. Re-invite them to send a fresh set-password link."
            }),
            PasswordResetSender.Outcome.Disabled => new BadRequestObjectResult(new
            {
                error = "That account is disabled. Re-enable it before sending a reset link."
            }),
            _ => new BadRequestObjectResult(new { error = "Couldn't send a reset link just now. Please try again." })
        };
    }
}
