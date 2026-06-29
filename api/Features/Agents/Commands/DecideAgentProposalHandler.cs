using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Agents;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Agents.Commands;

// The human-in-the-loop decision: accept or reject a pending proposal. Nothing in a proposal takes
// effect until accepted here. Only a Pending proposal can be decided; the decider is stamped.
public sealed class DecideAgentProposalHandler : ICommandHandler<DecideAgentProposal, AgentProposal>
{
    private readonly JpmsContext context;
    private readonly AgentRegistry registry;
    public DecideAgentProposalHandler(JpmsContext context, AgentRegistry registry) { this.context = context; this.registry = registry; }

    public async Task<AgentProposal> HandleAsync(DecideAgentProposal command, CancellationToken cancellationToken)
    {
        var entity = await context.AgentProposals
            .FirstOrDefaultAsync(p => p.ProposalId == command.ProposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal '{command.ProposalId}' not found.");

        if (entity.Status == (int)AgentProposalStatus.Pending)
        {
            entity.Status = (int)(command.Accept ? AgentProposalStatus.Accepted : AgentProposalStatus.Rejected);
            entity.DecidedByEmail = command.DecidedByEmail;
            entity.DecidedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        return entity.ToModel(registry.Find(entity.AgentKey));
    }
}
