
namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// Reads and drops the shared connection. Status is read by the internal team — the drawing pages
/// disable their Extract buttons off it — and disconnecting is admin-only (AdminGate, like the user
/// directory).
///
/// "Any signed-in user" until 2026-09-19, which predated the external portal logins: the status
/// carries the connected Jewel account's email address and the last refresh error, and a client or
/// subcontractor has no business reading either. Nobody external has an Extract button to disable.
/// </summary>
public sealed class BluebeamStatusEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IBluebeamClient client;

    public BluebeamStatusEndpoints(SignedInUserResolver users, JpmsContext context, IBluebeamClient client)
    {
        this.users = users; this.context = context; this.client = client;
    }

    [Function("GetBluebeamStatus")]
    public async Task<IActionResult> Status(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bluebeam/status")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!JpmsRoleSets.AllInternal.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var connection = await context.BluebeamConnections
            .FirstOrDefaultAsync(row => row.BluebeamConnectionId == BluebeamTokenService.ConnectionRowId, cancellationToken);
        return new OkObjectResult(new BluebeamStatus(
            client.IsConfigured,
            connection is not null,
            connection?.ConnectedEmail ?? "",
            connection?.ConnectedAt,
            connection?.LastRefreshSucceededAt,
            connection?.LastRefreshError));
    }

    [Function("DisconnectBluebeam")]
    public async Task<IActionResult> Disconnect(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bluebeam/disconnect")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AdminGate.Allows(signedInUser)) return new StatusCodeResult(403);

        var connection = await context.BluebeamConnections
            .FirstOrDefaultAsync(row => row.BluebeamConnectionId == BluebeamTokenService.ConnectionRowId, cancellationToken);
        if (connection is not null)
        {
            context.BluebeamConnections.Remove(connection);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new OkObjectResult(new BluebeamStatus(client.IsConfigured, false, "", null, null, null));
    }
}
