using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class AgreeVatAnalysisEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AgreeVatAnalysisAuthorisation authorisation;
    private readonly AgreeVatAnalysisValidation validation;
    private readonly ICommandHandler<AgreeVatAnalysis, VatAnalysis> handler;
    public AgreeVatAnalysisEndpoint(SignedInUserResolver users, AgreeVatAnalysisAuthorisation authorisation, AgreeVatAnalysisValidation validation, ICommandHandler<AgreeVatAnalysis, VatAnalysis> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(AgreeVatAnalysis))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/vat")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<AgreeVatAnalysis>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
