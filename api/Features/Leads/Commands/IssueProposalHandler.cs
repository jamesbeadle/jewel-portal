using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class IssueProposalHandler
    : ICommandHandler<IssueProposal, Proposal>
{
    private readonly JpmsContext context;

    public IssueProposalHandler(JpmsContext context) { this.context = context; }

    public async Task<Proposal> HandleAsync(IssueProposal command, CancellationToken cancellationToken)
    {
        var existing = await context.Proposals.FirstOrDefaultAsync(p => p.LeadId == command.LeadId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Lead {command.LeadId} already has a proposal. Use ReviseProposal instead.");

        var entity = new ProposalEntity
        {
            ProposalId = LeadIdentifierFactory.NextProposalId(),
            LeadId = command.LeadId,
            Value = command.Value,
            IssuedAt = DateTimeOffset.UtcNow,
            NegotiationRoundsJson = "[]"
        };
        context.Proposals.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
