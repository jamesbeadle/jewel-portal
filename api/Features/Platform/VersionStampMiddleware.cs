using Jewel.JPMS.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>
/// Stamps the build number (<see cref="BuildVersion"/>) on every HTTP response, so the client
/// learns about a newer deploy from traffic it was already sending: every route load fetches its
/// data (the stale-while-revalidate convention), so every route load checks the version without a
/// single extra request. The client side of the conversation is AppVersionService, which raises
/// the UpdateToast when the number here is higher than the one baked into the running bundle.
///
/// The header is set BEFORE the function runs, not after: by the time the endpoint's IActionResult
/// has been executed the response may already have started streaming, and a started response takes
/// no new headers. Set up front it rides out on everything — including the 401s and 500s, which
/// matter most, because a redeploy is exactly when a stale tab starts collecting them.
/// </summary>
public sealed class VersionStampMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var http = context.GetHttpContext();
        if (http is not null) http.Response.Headers[BuildVersion.Header] = BuildVersion.Value;
        await next(context);
    }
}
