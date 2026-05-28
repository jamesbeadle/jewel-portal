using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AssembleSiteReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AssembleSiteReportAuthorisation authorisation;
    private readonly AssembleSiteReportValidation validation;
    private readonly ICommandHandler<AssembleSiteReport, SiteReport> handler;
    public AssembleSiteReportEndpoint(SignedInUserResolver users, AssembleSiteReportAuthorisation authorisation, AssembleSiteReportValidation validation, ICommandHandler<AssembleSiteReport, SiteReport> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AssembleSiteReport))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/site-reports")] HttpRequest request, string projectId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<AssembleSiteReport>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
