using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class CreateRequestFromIntakeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateRequestFromIntakeAuthorisation authorisation;
    private readonly CreateRequestFromIntakeValidation validation;
    private readonly ICommandHandler<CreateRequestFromIntake, Request> handler;
    public CreateRequestFromIntakeEndpoint(SignedInUserResolver users, CreateRequestFromIntakeAuthorisation authorisation, CreateRequestFromIntakeValidation validation, ICommandHandler<CreateRequestFromIntake, Request> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(CreateRequestFromIntake))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "intake/{intakeId}/create-request")] HttpRequest request, string intakeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateRequestFromIntake>();
        if (posted is null) return new BadRequestResult();

        // IntakeId comes from the route; the raiser is always the signed-in triager.
        var command = posted with { IntakeId = intakeId, RaisedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
