using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class ResolveAccessRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ResolveAccessRequestAuthorisation authorisation;
    private readonly ResolveAccessRequestValidation validation;
    private readonly ICommandHandler<ResolveAccessRequest, Acknowledgement> handler;

    public ResolveAccessRequestEndpoint(
        SignedInUserResolver users,
        ResolveAccessRequestAuthorisation authorisation,
        ResolveAccessRequestValidation validation,
        ICommandHandler<ResolveAccessRequest, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(ResolveAccessRequest))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "access-requests/{email}/resolve")] HttpRequest request,
        string email)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new ResolveAccessRequest(email);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var acknowledgement = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(acknowledgement);
    }
}
