using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class SubmitAccessRequestEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SubmitAccessRequestAuthorisation authorisation;
    private readonly SubmitAccessRequestValidation validation;
    private readonly ICommandHandler<SubmitAccessRequest, AccessRequest> handler;

    public SubmitAccessRequestEndpoint(
        SignedInUserResolver users,
        SubmitAccessRequestAuthorisation authorisation,
        SubmitAccessRequestValidation validation,
        ICommandHandler<SubmitAccessRequest, AccessRequest> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SubmitAccessRequest))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "access-requests")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SubmitAccessRequest>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var accessRequest = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(accessRequest);
    }
}
