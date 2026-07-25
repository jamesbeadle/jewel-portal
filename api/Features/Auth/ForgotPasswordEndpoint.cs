using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// POST /api/auth/forgot-password — anonymous self-service reset. Always answers 200 with the same
/// neutral body, whatever happened behind it: an unknown address, an account that has never set a
/// password, a disabled account and a successful send are indistinguishable to the caller, so this
/// endpoint cannot be used to discover who has an account here.
/// </summary>
public sealed class ForgotPasswordEndpoint
{
    private readonly PasswordResetSender resets;
    private readonly IConfiguration configuration;

    public ForgotPasswordEndpoint(PasswordResetSender resets, IConfiguration configuration)
    {
        this.resets = resets;
        this.configuration = configuration;
    }

    [Function("AuthForgotPassword")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/forgot-password")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        ForgotPasswordRequest? body;
        try { body = await request.ReadFromJsonAsync<ForgotPasswordRequest>(cancellationToken); }
        catch { return Neutral(); }

        var email = body?.Email?.Trim() ?? "";
        if (!LooksLikeEmail(email)) return Neutral();

        await resets.SendAsync(email, SiteBaseUrl.Resolve(configuration, request), bypassThrottle: false, cancellationToken);
        return Neutral();
    }

    private static OkObjectResult Neutral() => new(new PasswordResetAcknowledgement(
        "If that email address has a JPMS account, a reset link is on its way. " +
        "Check your inbox — and your spam folder — then follow the link to choose a new password."));

    private static bool LooksLikeEmail(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@') && value.IndexOf('@') < value.LastIndexOf('.');
}
