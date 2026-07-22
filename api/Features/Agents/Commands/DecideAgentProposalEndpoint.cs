using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class DecideAgentProposalEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly DecideAgentProposalAuthorisation authorisation;
    private readonly DecideAgentProposalValidation validation;
    private readonly ICommandHandler<DecideAgentProposal, AgentProposal> handler;
    public DecideAgentProposalEndpoint(SignedInUserResolver users, DecideAgentProposalAuthorisation authorisation, DecideAgentProposalValidation validation, ICommandHandler<DecideAgentProposal, AgentProposal> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(DecideAgentProposal))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "agent-proposals/{proposalId}/decide")] HttpRequest request, string proposalId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<DecideAgentProposal>();
        if (posted is null) return new BadRequestResult();
        if (posted.ProposalId != proposalId) return new BadRequestObjectResult("Route proposalId does not match body.");

        // The decider is always the signed-in user — never trusted from the client body.
        var command = posted with { DecidedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
