using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>POST /api/requests/{requestId}/enable-rfq — mark an RFI as carrying an RFQ.</summary>
public sealed class EnableRfqOnRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly EnableRfqOnRequestAuthorisation authorisation;
    private readonly EnableRfqOnRequestValidation validation;
    private readonly ICommandHandler<EnableRfqOnRequest, Request> handler;

    public EnableRfqOnRequestEndpoint(
        SignedInUserResolver users,
        EnableRfqOnRequestAuthorisation authorisation,
        EnableRfqOnRequestValidation validation,
        ICommandHandler<EnableRfqOnRequest, Request> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(EnableRfqOnRequest))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/enable-rfq")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new EnableRfqOnRequest(requestId);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
