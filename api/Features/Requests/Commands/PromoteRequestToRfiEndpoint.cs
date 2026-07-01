using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// POST /api/requests/{requestId}/promote-to-rfi — promote a request to an RFI and issue it to the
/// architect. No request body is required.
/// </summary>
public sealed class PromoteRequestToRfiEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PromoteRequestToRfiAuthorisation authorisation;
    private readonly PromoteRequestToRfiValidation validation;
    private readonly ICommandHandler<PromoteRequestToRfi, Request> handler;

    public PromoteRequestToRfiEndpoint(
        SignedInUserResolver users,
        PromoteRequestToRfiAuthorisation authorisation,
        PromoteRequestToRfiValidation validation,
        ICommandHandler<PromoteRequestToRfi, Request> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(PromoteRequestToRfi))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/promote-to-rfi")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new PromoteRequestToRfi(requestId);

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
