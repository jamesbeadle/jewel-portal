using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Health;

/// <summary>
/// GET /api/ping — the cheapest possible proof that the managed-function host is up and serving.
///
/// This exists to be called on a schedule, not by the app. Static Web Apps managed functions run on
/// a consumption host that scales to zero and cannot be configured with Always On, so after an idle
/// spell the next real user pays a cold start of 15-30s (often worse) while every screen sits on a
/// spinner. An Application Insights Standard availability test pointed here every five minutes keeps
/// the host resident, and doubles as the outage alert we otherwise have no way to raise — managed
/// functions cannot be wired to Application Insights directly.
///
/// Deliberately touches nothing: no database, no auth, no DI beyond the host itself. If this answers
/// and a real endpoint does not, the fault is below the API rather than in the platform, which is
/// exactly the discrimination that was missing while we were diagnosing the twice-daily stalls.
/// </summary>
public sealed class PingEndpoint
{
    [Function("Ping")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest request) =>
        new OkObjectResult(new { status = "ok", utc = DateTimeOffset.UtcNow });
}
