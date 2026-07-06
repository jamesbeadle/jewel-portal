using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class TakeProgrammeBaselineEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly TakeProgrammeBaselineAuthorisation authorisation;
    private readonly TakeProgrammeBaselineValidation validation;
    private readonly ICommandHandler<TakeProgrammeBaseline, ProgrammeBaseline> handler;
    public TakeProgrammeBaselineEndpoint(SignedInUserResolver users, TakeProgrammeBaselineAuthorisation authorisation, TakeProgrammeBaselineValidation validation, ICommandHandler<TakeProgrammeBaseline, ProgrammeBaseline> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(TakeProgrammeBaseline))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/programme/baselines")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<TakeProgrammeBaseline>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException reason)
        {
            return new BadRequestObjectResult(new[] { reason.Message });
        }
    }
}
