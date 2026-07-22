using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Site;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeBaselineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RemoveProgrammeBaselineAuthorisation authorisation;
    private readonly RemoveProgrammeBaselineValidation validation;
    private readonly ICommandHandler<RemoveProgrammeBaseline, Acknowledgement> handler;
    public RemoveProgrammeBaselineEndpoint(SignedInUserResolver users, RemoveProgrammeBaselineAuthorisation authorisation, RemoveProgrammeBaselineValidation validation, ICommandHandler<RemoveProgrammeBaseline, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RemoveProgrammeBaseline))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "programme-baselines/{programmeBaselineId}")] HttpRequest request, string programmeBaselineId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RemoveProgrammeBaseline(programmeBaselineId);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
