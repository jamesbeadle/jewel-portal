using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

public sealed class RunAgentAnalysisEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RunAgentAnalysisAuthorisation authorisation;
    private readonly RunAgentAnalysisValidation validation;
    private readonly ICommandHandler<RunAgentAnalysis, AgentProposal> handler;
    public RunAgentAnalysisEndpoint(SignedInUserResolver users, RunAgentAnalysisAuthorisation authorisation, RunAgentAnalysisValidation validation, ICommandHandler<RunAgentAnalysis, AgentProposal> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RunAgentAnalysis))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/agents/{agentKey}/analyse")] HttpRequest request, string requestId, string agentKey)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RunAgentAnalysis(requestId, agentKey);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
