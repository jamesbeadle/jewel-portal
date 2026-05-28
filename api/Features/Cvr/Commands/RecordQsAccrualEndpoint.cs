using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordQsAccrualEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordQsAccrualAuthorisation authorisation;
    private readonly RecordQsAccrualValidation validation;
    private readonly ICommandHandler<RecordQsAccrual, QsAccrual> handler;
    public RecordQsAccrualEndpoint(SignedInUserResolver users, RecordQsAccrualAuthorisation authorisation, RecordQsAccrualValidation validation, ICommandHandler<RecordQsAccrual, QsAccrual> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RecordQsAccrual))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/qs-accruals")] HttpRequest request, string projectId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<RecordQsAccrual>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
