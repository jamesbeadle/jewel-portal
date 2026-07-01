using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CashCalls.Queries;

public sealed class GetProjectCashCallSummaryHandler : IQueryHandler<GetProjectCashCallSummary, ProjectCashCallSummary>
{
    private readonly JpmsContext context;
    public GetProjectCashCallSummaryHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectCashCallSummary> HandleAsync(GetProjectCashCallSummary query, CancellationToken cancellationToken)
    {
        var calls = await context.CashCalls
            .Where(call => call.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var requested = calls.Sum(call => call.AmountRequested);
        var received = calls.Sum(call => call.AmountReceived);

        return new ProjectCashCallSummary(
            ProjectId: query.ProjectId,
            TotalRequested: requested,
            TotalReceived: received,
            Outstanding: requested - received);
    }
}
