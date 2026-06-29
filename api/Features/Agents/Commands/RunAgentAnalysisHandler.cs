using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

// Asks the agent to analyse the request and persists the structured proposal it returns for human
// review. Any earlier still-pending proposal from the same agent is marked Superseded so only the
// latest awaits a decision. A stub agent yields an Unavailable proposal.
public sealed class RunAgentAnalysisHandler : ICommandHandler<RunAgentAnalysis, AgentProposal>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    private readonly RequestContextAssembler assembler;
    public RunAgentAnalysisHandler(JpmsContext context, AgentRegistry registry, RequestContextAssembler assembler)
    { this.context = context; this.registry = registry; this.assembler = assembler; }

    public async Task<AgentProposal> HandleAsync(RunAgentAnalysis command, CancellationToken cancellationToken)
    {
        var agent = registry.Find(command.AgentKey)
            ?? throw new InvalidOperationException($"Unknown agent '{command.AgentKey}'.");

        var requestContext = await assembler.AssembleAsync(command.RequestId, cancellationToken)
            ?? new RequestAgentContext(command.RequestId, "(request not found)", "", "");
        var result = await agent.AnalyseAsync(requestContext, cancellationToken);

        var superseded = await context.AgentProposals
            .Where(p => p.RequestId == command.RequestId
                        && p.AgentKey == command.AgentKey
                        && p.Status == (int)AgentProposalStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var old in superseded)
            old.Status = (int)AgentProposalStatus.Superseded;

        var entity = new AgentProposalEntity
        {
            ProposalId = AgentsIdentifierFactory.Next(),
            RequestId = command.RequestId,
            AgentKey = command.AgentKey,
            Status = (int)result.Status,
            Summary = result.Summary,
            StructuredJson = result.StructuredJson,
            Rationale = result.Rationale,
            CreatedAt = DateTimeOffset.UtcNow,
            DecidedByEmail = null,
            DecidedAt = null
        };
        context.AgentProposals.Add(entity);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(agent);
    }
}
