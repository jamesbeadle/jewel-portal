using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class CaptureLeadEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CaptureLeadAuthorisation authorisation;
    private readonly CaptureLeadValidation validation;
    private readonly ICommandHandler<CaptureLead, Lead> handler;

    public CaptureLeadEndpoint(
        SignedInUserResolver users,
        CaptureLeadAuthorisation authorisation,
        CaptureLeadValidation validation,
        ICommandHandler<CaptureLead, Lead> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CaptureLead))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<CaptureLead>();
        if (command is null) return new BadRequestResult();

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var lead = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(lead);
    }
}
