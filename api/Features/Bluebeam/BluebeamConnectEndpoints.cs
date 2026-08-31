using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// The one-time connect flow's api half: Start (admin-only) mints the consent URL the browser is
/// sent to. The CALLBACK deliberately does NOT live here — the Static Web Apps edge intercepts any
/// request carrying a ?code= query parameter as one of its own auth callbacks and 500s it before
/// a managed function ever runs (proved empirically 2026-08-31: /api/bluebeam/callback?state=x
/// redirects fine, ?code=x alone 500s, and nothing reaches App Insights). Bluebeam therefore
/// redirects to the WORKER Function App (worker/Bluebeam/BluebeamConnectCallback.cs), a plain
/// Functions host with no such edge, which stores the connection and sends the browser back to
/// /admin/integrations. The signed ten-minute state minted here is what proves the flow was begun
/// by an admin, and BluebeamConnectionWriter is the shared store both halves use.
/// </summary>
public sealed class BluebeamConnectEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly BluebeamOptions options;

    public BluebeamConnectEndpoints(SignedInUserResolver users, BluebeamOptions options)
    {
        this.users = users; this.options = options;
    }

    [Function("StartBluebeamConnect")]
    public async Task<IActionResult> Start(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bluebeam/connect/start")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);
        if (!options.IsConfigured)
            return new BadRequestObjectResult(
                "Bluebeam isn't configured — add the Bluebeam__ClientId and Bluebeam__ClientSecret app settings first.");

        var state = BluebeamConnectionState.Mint(options.ClientSecret!, signedInUser.Email);
        var authorizeUrl = options.AuthorizeUrl
            + $"?response_type=code&client_id={Uri.EscapeDataString(options.ClientId!)}"
            + $"&redirect_uri={Uri.EscapeDataString(options.RedirectUri)}"
            + $"&scope={Uri.EscapeDataString(options.Scopes)}"
            + $"&state={Uri.EscapeDataString(state)}";
        return new OkObjectResult(new BluebeamConnectStart(authorizeUrl));
    }
}
