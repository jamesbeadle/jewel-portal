using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

public sealed class SetProjectRetentionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetProjectRetentionAuthorisation authorisation;
    private readonly SetProjectRetentionValidation validation;
    private readonly ICommandHandler<SetProjectRetention, ProjectRetention> handler;

    public SetProjectRetentionEndpoint(
        SignedInUserResolver users,
        SetProjectRetentionAuthorisation authorisation,
        SetProjectRetentionValidation validation,
        ICommandHandler<SetProjectRetention, ProjectRetention> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetProjectRetention))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/retention-terms")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetProjectRetention>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var retention = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(retention);
    }
}
