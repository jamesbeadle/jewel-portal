using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class ReviseProposalHandler
    : ICommandHandler<ReviseProposal, Proposal>
{
    private readonly JpmsContext context;

    public ReviseProposalHandler(JpmsContext context) { this.context = context; }

    public async Task<Proposal> HandleAsync(ReviseProposal command, CancellationToken cancellationToken)
    {
        var entity = await context.Proposals.FirstOrDefaultAsync(p => p.LeadId == command.LeadId, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException($"Lead {command.LeadId} has no proposal. Use IssueProposal first.");

        entity.Value = command.RevisedValue;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
