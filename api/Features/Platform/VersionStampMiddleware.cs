using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>
/// Stamps the announced app version (the AppVersions row, cached — see AnnouncedVersionCache) on
/// every HTTP response, so the client learns about a published update from traffic it was already
/// sending: every route load fetches its data (the stale-while-revalidate convention), so every
/// route load checks the version without a single extra request. The client side of the
/// conversation is AppVersionService, which raises the UpdateToast when the number here is higher
/// than the one the tab baselined on. Falls back to BuildVersion.Value when no announced version
/// has ever been readable, so the header never disappears.
///
/// The header is set BEFORE the function runs, not after: by the time the endpoint's IActionResult
/// has been executed the response may already have started streaming, and a started response takes
/// no new headers. Set up front it rides out on everything — including the 401s and 500s, which
/// matter most, because a stale tab is exactly the one collecting them.
/// </summary>
public sealed class VersionStampMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is not null)
        {
            var cache = context.InstanceServices.GetRequiredService<AnnouncedVersionCache>();
            var database = context.InstanceServices.GetRequiredService<JpmsContext>();
            var announced = await cache.GetAsync(database, http.RequestAborted);
            http.Response.Headers[BuildVersion.Header] = announced?.ToString() ?? BuildVersion.Value;
        }
        await next(context);
    }
}
