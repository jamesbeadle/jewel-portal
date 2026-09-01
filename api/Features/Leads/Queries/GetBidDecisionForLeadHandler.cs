using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetBidDecisionForLeadHandler : IQueryHandler<GetBidDecisionForLead, BidDecision?>
{
    private readonly JpmsContext context;

    public GetBidDecisionForLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<BidDecision?> HandleAsync(GetBidDecisionForLead query, CancellationToken cancellationToken)
    {
        var entity = await context.BidDecisions.AsNoTracking()
            .FirstOrDefaultAsync(decision => decision.LeadId == query.LeadId, cancellationToken);
        return entity?.ToModel();
    }
}
