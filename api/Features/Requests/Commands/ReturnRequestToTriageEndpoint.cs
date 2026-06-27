using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ReturnRequestToTriageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReturnRequestToTriageAuthorisation authorisation;
    private readonly ReturnRequestToTriageValidation validation;
    private readonly ICommandHandler<ReturnRequestToTriage, Acknowledgement> handler;

    public ReturnRequestToTriageEndpoint(SignedInUserResolver users, ReturnRequestToTriageAuthorisation authorisation, ReturnRequestToTriageValidation validation, ICommandHandler<ReturnRequestToTriage, Acknowledgement> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(ReturnRequestToTriage))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/return-to-triage")] HttpRequest request, string requestId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new ReturnRequestToTriage(requestId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
