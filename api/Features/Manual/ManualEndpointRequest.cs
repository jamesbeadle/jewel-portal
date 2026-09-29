namespace Jewel.JPMS.Api.Features.Manual;

/// <summary>The one place a manual endpoint reaches into the request for the token that cancels it.</summary>
internal static class ManualEndpointRequest
{
    public static CancellationToken CancellationOf(HttpRequest request) => request.HttpContext.RequestAborted;
}
