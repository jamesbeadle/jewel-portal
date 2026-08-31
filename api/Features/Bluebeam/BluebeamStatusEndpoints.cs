using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// Reads and drops the shared connection. Status is readable by any signed-in user — the drawing
/// pages disable their Extract buttons off it — and never carries a secret; disconnecting is
/// admin-only (AdminGate, like the user directory).
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
