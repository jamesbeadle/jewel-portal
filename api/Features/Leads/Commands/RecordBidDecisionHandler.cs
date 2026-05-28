using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordBidDecisionHandler
    : ICommandHandler<RecordBidDecision, BidDecision>
{
    private readonly JpmsContext context;

    public RecordBidDecisionHandler(JpmsContext context) { this.context = context; }

    public async Task<BidDecision> HandleAsync(RecordBidDecision command, CancellationToken cancellationToken)
    {
        var existing = await context.BidDecisions.FindAsync(new object[] { command.LeadId }, cancellationToken);
        var decidedAt = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var entity = new BidDecisionEntity
            {
                LeadId = command.LeadId,
                ShouldBid = command.ShouldBid,
                Reason = command.Reason,
                DecidedByEmail = command.DecidedByEmail,
                DecidedAt = decidedAt
            };
            context.BidDecisions.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            return entity.ToModel();
        }

        existing.ShouldBid = command.ShouldBid;
        existing.Reason = command.Reason;
        existing.DecidedByEmail = command.DecidedByEmail;
        existing.DecidedAt = decidedAt;
        await context.SaveChangesAsync(cancellationToken);
        return existing.ToModel();
    }
}
