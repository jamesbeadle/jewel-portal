using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class DiscardIntakeEmailEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DiscardIntakeEmailAuthorisation authorisation;
    private readonly DiscardIntakeEmailValidation validation;
    private readonly ICommandHandler<DiscardIntakeEmail, IntakeEmail> handler;
    public DiscardIntakeEmailEndpoint(SignedInUserResolver users, DiscardIntakeEmailAuthorisation authorisation, DiscardIntakeEmailValidation validation, ICommandHandler<DiscardIntakeEmail, IntakeEmail> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DiscardIntakeEmail))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "intake/{intakeId}/discard")] HttpRequest request, string intakeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<DiscardIntakeEmail>();
        var command = new DiscardIntakeEmail(intakeId, posted?.Notes);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
