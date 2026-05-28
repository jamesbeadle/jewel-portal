using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetBidDecisionForLeadHandler : IQueryHandler<GetBidDecisionForLead, BidDecision?>
{
    private readonly JpmsContext context;

    public GetBidDecisionForLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<BidDecision?> HandleAsync(GetBidDecisionForLead query, CancellationToken cancellationToken)
    {
        var entity = await context.BidDecisions
            .FirstOrDefaultAsync(decision => decision.LeadId == query.LeadId, cancellationToken);
        return entity?.ToModel();
    }
}
