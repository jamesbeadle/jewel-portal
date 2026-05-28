using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class ApproveSiteReportEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ApproveSiteReportAuthorisation authorisation;
    private readonly ApproveSiteReportValidation validation;
    private readonly ICommandHandler<ApproveSiteReport, SiteReport> handler;
    public ApproveSiteReportEndpoint(SignedInUserResolver users, ApproveSiteReportAuthorisation authorisation, ApproveSiteReportValidation validation, ICommandHandler<ApproveSiteReport, SiteReport> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ApproveSiteReport))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-reports/{siteReportId}/approval")] HttpRequest request, string siteReportId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new ApproveSiteReport(siteReportId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
