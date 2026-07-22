using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordSiteVisitNotesEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordSiteVisitNotesAuthorisation authorisation;
    private readonly RecordSiteVisitNotesValidation validation;
    private readonly ICommandHandler<RecordSiteVisitNotes, SiteVisit> handler;

    public RecordSiteVisitNotesEndpoint(
        SignedInUserResolver users,
        RecordSiteVisitNotesAuthorisation authorisation,
        RecordSiteVisitNotesValidation validation,
        ICommandHandler<RecordSiteVisitNotes, SiteVisit> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordSiteVisitNotes))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "site-visits/{siteVisitId}")] HttpRequest request,
        string siteVisitId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecordSiteVisitNotes>();
        if (command is null) return new BadRequestResult();
        if (command.SiteVisitId != siteVisitId) return new BadRequestObjectResult("Route siteVisitId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var visit = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(visit);
    }
}
