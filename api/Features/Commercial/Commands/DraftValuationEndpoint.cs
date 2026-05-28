using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class DraftValuationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DraftValuationAuthorisation authorisation;
    private readonly DraftValuationValidation validation;
    private readonly ICommandHandler<DraftValuation, Valuation> handler;
    public DraftValuationEndpoint(SignedInUserResolver users, DraftValuationAuthorisation authorisation, DraftValuationValidation validation, ICommandHandler<DraftValuation, Valuation> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DraftValuation))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/valuations")] HttpRequest request, string projectId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<DraftValuation>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
