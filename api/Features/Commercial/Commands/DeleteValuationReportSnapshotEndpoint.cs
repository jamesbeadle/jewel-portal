using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>DELETE /api/valuation-report-snapshots/{snapshotId} — remove a snapshot taken in error.</summary>
public sealed class DeleteValuationReportSnapshotEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ValuationReportAuthorisation authorisation;
    private readonly ICommandHandler<DeleteValuationReportSnapshot, Acknowledgement> handler;
    public DeleteValuationReportSnapshotEndpoint(SignedInUserResolver users, ValuationReportAuthorisation authorisation, ICommandHandler<DeleteValuationReportSnapshot, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(DeleteValuationReportSnapshot))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "valuation-report-snapshots/{snapshotId}")] HttpRequest request, string snapshotId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new DeleteValuationReportSnapshot(snapshotId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
