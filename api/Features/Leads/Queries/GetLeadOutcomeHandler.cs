using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetLeadOutcomeHandler
    : IQueryHandler<GetLeadOutcome, LeadOutcome?>
{
    private readonly JpmsContext context;

    public GetLeadOutcomeHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadOutcome?> HandleAsync(GetLeadOutcome query, CancellationToken cancellationToken)
    {
        var entity = await context.LeadOutcomes.FindAsync(new object[] { query.LeadId }, cancellationToken);
        return entity?.ToModel();
    }
}
