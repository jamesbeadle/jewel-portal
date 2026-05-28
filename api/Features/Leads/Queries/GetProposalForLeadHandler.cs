using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetProposalForLeadHandler
    : IQueryHandler<GetProposalForLead, Proposal?>
{
    private readonly JpmsContext context;

    public GetProposalForLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<Proposal?> HandleAsync(GetProposalForLead query, CancellationToken cancellationToken)
    {
        var entity = await context.Proposals.FirstOrDefaultAsync(proposal => proposal.LeadId == query.LeadId, cancellationToken);
        return entity?.ToModel();
    }
}
