using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Platform;

/// <summary>
/// The announced version as a body, for the one check the response-header route cannot cover: a
/// tab sitting in the background sends no traffic, so when it regains focus the UpdateToast asks
/// outright rather than waiting for the user's next navigation. Reads the database directly
/// instead of going through the cache TTL — a tab regaining focus is exactly the moment freshness
/// matters — and teaches the cache what it found. Anonymous on purpose: the number already rides
/// on every response header, so there is nothing here to protect.
/// </summary>
public sealed class GetAppVersionEndpoint
{
    private readonly JpmsContext context;
    private readonly AnnouncedVersionCache cache;

    public GetAppVersionEndpoint(JpmsContext context, AnnouncedVersionCache cache)
    {
        this.context = context;
        this.cache = cache;
    }

    [Function("GetAppVersion")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "version")] HttpRequest request)
    {
        try
        {
            var row = await context.AppVersions.AsNoTracking()
                .FirstOrDefaultAsync(
                    current => current.AppVersionId == AnnouncedVersionCache.CurrentRowId,
                    request.HttpContext.RequestAborted);
            if (row is not null)
            {
                cache.Update(row.Version);
                return new OkObjectResult(row.Version.ToString());
            }
        }
        catch
        {
            // Schema behind the code or database unreachable — fall through to the compile-time
            // number, the same answer the header gives in that state.
        }
        return new OkObjectResult(BuildVersion.Value);
    }
}
