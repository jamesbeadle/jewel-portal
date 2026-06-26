using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class LinkIntakeToRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly LinkIntakeToRequestAuthorisation authorisation;
    private readonly LinkIntakeToRequestValidation validation;
    private readonly ICommandHandler<LinkIntakeToRequest, IntakeEmail> handler;
    public LinkIntakeToRequestEndpoint(SignedInUserResolver users, LinkIntakeToRequestAuthorisation authorisation, LinkIntakeToRequestValidation validation, ICommandHandler<LinkIntakeToRequest, IntakeEmail> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(LinkIntakeToRequest))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "intake/{intakeId}/link")] HttpRequest request, string intakeId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<LinkIntakeToRequest>();
        if (posted is null) return new BadRequestResult();
        var command = posted with { IntakeId = intakeId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
