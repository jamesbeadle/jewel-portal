using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Site;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeTaskLinkEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RemoveProgrammeTaskLinkAuthorisation authorisation;
    private readonly RemoveProgrammeTaskLinkValidation validation;
    private readonly ICommandHandler<RemoveProgrammeTaskLink, Acknowledgement> handler;
    public RemoveProgrammeTaskLinkEndpoint(SignedInUserResolver users, RemoveProgrammeTaskLinkAuthorisation authorisation, RemoveProgrammeTaskLinkValidation validation, ICommandHandler<RemoveProgrammeTaskLink, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RemoveProgrammeTaskLink))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "programme-links/{programmeTaskLinkId}")] HttpRequest request, string programmeTaskLinkId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RemoveProgrammeTaskLink(programmeTaskLinkId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
