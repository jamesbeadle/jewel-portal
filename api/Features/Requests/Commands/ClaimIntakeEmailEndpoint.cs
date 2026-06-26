using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ClaimIntakeEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ClaimIntakeEmailAuthorisation authorisation;
    private readonly ClaimIntakeEmailValidation validation;
    private readonly ICommandHandler<ClaimIntakeEmail, IntakeEmail> handler;
    public ClaimIntakeEmailEndpoint(SignedInUserResolver users, ClaimIntakeEmailAuthorisation authorisation, ClaimIntakeEmailValidation validation, ICommandHandler<ClaimIntakeEmail, IntakeEmail> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ClaimIntakeEmail))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "intake/{intakeId}/claim")] HttpRequest request, string intakeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        // The claimant is always the signed-in user — never trusted from the client body.
        var command = new ClaimIntakeEmail(intakeId, signedInUser.Email);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
