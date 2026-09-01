using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetProposalForLeadHandler
    : IQueryHandler<GetProposalForLead, Proposal?>
{
    private readonly JpmsContext context;

    public GetProposalForLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<Proposal?> HandleAsync(GetProposalForLead query, CancellationToken cancellationToken)
    {
        var entity = await context.Proposals.AsNoTracking().FirstOrDefaultAsync(proposal => proposal.LeadId == query.LeadId, cancellationToken);
        return entity?.ToModel();
    }
}
