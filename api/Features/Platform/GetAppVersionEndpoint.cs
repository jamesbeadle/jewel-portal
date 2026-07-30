using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>
/// The build number as a body, for the one check the response-header route cannot cover: a tab
/// sitting in the background sends no traffic, so when it regains focus the UpdateToast asks
/// outright rather than waiting for the user's next navigation. Anonymous on purpose — the number
/// already rides on every response header, so there is nothing here to protect.
/// </summary>
public sealed class GetAppVersionEndpoint
{
    [Function("GetAppVersion")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "version")] HttpRequest request)
        => new OkObjectResult(BuildVersion.Value);
}
