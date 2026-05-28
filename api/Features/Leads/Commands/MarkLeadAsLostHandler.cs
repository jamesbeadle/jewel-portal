using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class MarkLeadAsLostHandler
    : ICommandHandler<MarkLeadAsLost, LeadOutcome>
{
    private readonly JpmsContext context;

    public MarkLeadAsLostHandler(JpmsContext context) { this.context = context; }

    public async Task<LeadOutcome> HandleAsync(MarkLeadAsLost command, CancellationToken cancellationToken)
    {
        var lead = await context.Leads.FindAsync(new object[] { command.LeadId }, cancellationToken);
        if (lead is null) throw new InvalidOperationException($"Lead {command.LeadId} not found.");

        var outcome = new LeadOutcomeEntity
        {
            LeadId = command.LeadId,
            IsWon = false,
            Reason = command.Reason,
            DecidedByEmail = command.DecidedByEmail,
            DecidedAt = DateTimeOffset.UtcNow,
            CreatedProjectId = null
        };
        context.LeadOutcomes.Add(outcome);
        lead.Stage = (int)LeadStage.Lost;
        await context.SaveChangesAsync(cancellationToken);
        return outcome.ToModel();
    }
}
