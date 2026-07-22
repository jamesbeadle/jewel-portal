using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CommercialInputs.Commands;

public sealed class RecordSubcontractorRetentionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordSubcontractorRetentionAuthorisation authorisation;
    private readonly RecordSubcontractorRetentionValidation validation;
    private readonly ICommandHandler<RecordSubcontractorRetention, SubcontractorRetention> handler;

    public RecordSubcontractorRetentionEndpoint(
        SignedInUserResolver users,
        RecordSubcontractorRetentionAuthorisation authorisation,
        RecordSubcontractorRetentionValidation validation,
        ICommandHandler<RecordSubcontractorRetention, SubcontractorRetention> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordSubcontractorRetention))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/subcontractor-retentions")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecordSubcontractorRetention>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var retention = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(retention);
    }
}
