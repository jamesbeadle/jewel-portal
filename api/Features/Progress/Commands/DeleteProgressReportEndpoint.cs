using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Progress;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class DeleteProgressReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DeleteProgressReportAuthorisation authorisation;
    private readonly ICommandHandler<DeleteProgressReport, Acknowledgement> handler;

    public DeleteProgressReportEndpoint(
        SignedInUserResolver users,
        DeleteProgressReportAuthorisation authorisation,
        ICommandHandler<DeleteProgressReport, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.handler = handler;
    }

    [Function(nameof(DeleteProgressReport))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "progress-reports/{progressReportId}")] HttpRequest request,
        string progressReportId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteProgressReport(progressReportId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var acknowledgement = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(acknowledgement);
    }
}
