using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RestoreIntakeEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RestoreIntakeEmailAuthorisation authorisation;
    private readonly RestoreIntakeEmailValidation validation;
    private readonly ICommandHandler<RestoreIntakeEmail, IntakeEmail> handler;
    public RestoreIntakeEmailEndpoint(SignedInUserResolver users, RestoreIntakeEmailAuthorisation authorisation, RestoreIntakeEmailValidation validation, ICommandHandler<RestoreIntakeEmail, IntakeEmail> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RestoreIntakeEmail))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "intake/{intakeId}/restore")] HttpRequest request, string intakeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RestoreIntakeEmail(intakeId);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
