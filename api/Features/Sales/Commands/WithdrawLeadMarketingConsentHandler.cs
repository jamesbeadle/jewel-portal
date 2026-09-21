using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Commands;

// The prospect asked us to stop — on the phone, in a reply — and a member of the sales team
// records it here; the prospect's own door on their imagine page lands the same stamp. Read by
// every follow-up send (SendSalesProposal) through LeadMarketingConsents.

public sealed class WithdrawLeadMarketingConsentAuthorisation
{
    public bool Allows(SignedInUser user, WithdrawLeadMarketingConsent command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class WithdrawLeadMarketingConsentValidation
{
    public ValidationOutcome Check(WithdrawLeadMarketingConsent command) =>
        string.IsNullOrWhiteSpace(command.LeadId) ? ValidationOutcome.Failed("LeadId is required.") : ValidationOutcome.Passed;
}

public sealed class WithdrawLeadMarketingConsentHandler : ICommandHandler<WithdrawLeadMarketingConsent, Lead>
{
    private readonly JpmsContext context;
    public WithdrawLeadMarketingConsentHandler(JpmsContext context) { this.context = context; }

    public async Task<Lead> HandleAsync(WithdrawLeadMarketingConsent command, CancellationToken cancellationToken)
    {
        var lead = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        var isAlreadyWithdrawn = LeadMarketingConsents.Of(lead) == LeadMarketingConsent.Withdrawn;
        if (isAlreadyWithdrawn) return lead.ToModel(null);

        var now = DateTimeOffset.UtcNow;
        LeadMarketingConsents.RecordWithdrawn(lead, now);
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Kind = (int)LeadActivityKind.Note,
            Summary = "Asked us to stop keeping in touch — recorded; no further follow-up goes out. Their concepts and any open proposal are unaffected.",
            OccurredAt = now,
            RecordedByEmail = command.WithdrawnByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        return lead.ToModel(null);
    }
}
