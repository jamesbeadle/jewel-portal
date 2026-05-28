using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class IssueValuationEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IssueValuationAuthorisation authorisation;
    private readonly IssueValuationValidation validation;
    private readonly ICommandHandler<IssueValuation, Valuation> handler;
    public IssueValuationEndpoint(SignedInUserResolver users, IssueValuationAuthorisation authorisation, IssueValuationValidation validation, ICommandHandler<IssueValuation, Valuation> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(IssueValuation))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "valuations/{valuationId}/issue")] HttpRequest request, string valuationId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new IssueValuation(valuationId);
        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
