using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class RaiseChangeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RaiseChangeAuthorisation authorisation;
    private readonly RaiseChangeValidation validation;
    private readonly ICommandHandler<RaiseChange, ChangeRecord> handler;
    public RaiseChangeEndpoint(SignedInUserResolver users, RaiseChangeAuthorisation authorisation, RaiseChangeValidation validation, ICommandHandler<RaiseChange, ChangeRecord> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RaiseChange))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/changes")] HttpRequest request, string projectId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<RaiseChange>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
