using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

public sealed class ConfirmRetentionReleaseEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ConfirmRetentionReleaseAuthorisation authorisation;
    private readonly ConfirmRetentionReleaseValidation validation;
    private readonly ICommandHandler<ConfirmRetentionRelease, ProjectRetention> handler;
    private readonly JpmsContext context;

    public ConfirmRetentionReleaseEndpoint(
        SignedInUserResolver users,
        ConfirmRetentionReleaseAuthorisation authorisation,
        ConfirmRetentionReleaseValidation validation,
        ICommandHandler<ConfirmRetentionRelease, ProjectRetention> handler,
        JpmsContext context)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
        this.context = context;
    }

    [Function(nameof(ConfirmRetentionRelease))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/retention-terms/releases")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<ConfirmRetentionRelease>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var exists = await context.ProjectRetentions.AnyAsync(
            retention => retention.ProjectId == projectId, request.HttpContext.RequestAborted);
        if (!exists) return new BadRequestObjectResult("The project has no retention terms to release against.");

        try
        {
            var retention = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
            return new OkObjectResult(retention);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
