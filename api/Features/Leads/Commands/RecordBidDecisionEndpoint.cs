using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordBidDecisionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RecordBidDecisionAuthorisation authorisation;
    private readonly RecordBidDecisionValidation validation;
    private readonly ICommandHandler<RecordBidDecision, BidDecision> handler;

    public RecordBidDecisionEndpoint(
        SignedInUserResolver users,
        RecordBidDecisionAuthorisation authorisation,
        RecordBidDecisionValidation validation,
        ICommandHandler<RecordBidDecision, BidDecision> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RecordBidDecision))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "leads/{leadId}/bid-decision")] HttpRequest request,
        string leadId)
    {
        var signedInUser = users.Resolve(request);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<RecordBidDecision>();
        if (command is null) return new BadRequestResult();
        if (command.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        var decision = await handler.HandleAsync(command, request.HttpContext.RequestAborted);
        return new OkObjectResult(decision);
    }
}
