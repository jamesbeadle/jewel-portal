using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class ReviseValuationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReviseValuationAuthorisation authorisation;
    private readonly ReviseValuationValidation validation;
    private readonly ICommandHandler<ReviseValuation, Valuation> handler;
    public ReviseValuationEndpoint(SignedInUserResolver users, ReviseValuationAuthorisation authorisation, ReviseValuationValidation validation, ICommandHandler<ReviseValuation, Valuation> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ReviseValuation))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "valuations/{valuationId}")] HttpRequest request, string valuationId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<ReviseValuation>();
        if (command is null) return new BadRequestResult();
        if (command.ValuationId != valuationId) return new BadRequestObjectResult("Route valuationId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
