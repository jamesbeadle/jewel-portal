using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AddProgrammeTaskLinkEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AddProgrammeTaskLinkAuthorisation authorisation;
    private readonly AddProgrammeTaskLinkValidation validation;
    private readonly ICommandHandler<AddProgrammeTaskLink, ProgrammeTaskLink> handler;
    public AddProgrammeTaskLinkEndpoint(SignedInUserResolver users, AddProgrammeTaskLinkAuthorisation authorisation, AddProgrammeTaskLinkValidation validation, ICommandHandler<AddProgrammeTaskLink, ProgrammeTaskLink> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AddProgrammeTaskLink))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/programme/links")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<AddProgrammeTaskLink>();
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
