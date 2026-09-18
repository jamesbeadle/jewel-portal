using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

public sealed class ReplyToSalesEmailAuthorisation
{
    public bool Allows(SignedInUser user, ReplyToSalesEmail command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class ReplyToSalesEmailValidation
{
    public ValidationOutcome Check(ReplyToSalesEmail command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (string.IsNullOrWhiteSpace(command.Body)) errors.Add("Write something to send.");
        if ((command.Body ?? "").Length > 20000) errors.Add("Keep the reply under 20,000 characters.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

/// <summary>The reply leaves through <see cref="SalesOutboundEmail"/>, so it is staged, sent and
/// audited exactly as every other portal email is. The lead is read first because the audit row
/// files under it, and the activity on the lead's own timeline is written afterwards — the
/// timeline is what the sales team reads, the audit row is what the trail can be searched by.</summary>
public sealed class ReplyToSalesEmailHandler : ICommandHandler<ReplyToSalesEmail, SalesReplyOutcome>
{
    private readonly JpmsContext context;
    private readonly ISalesMailbox mailbox;
    private readonly SalesOutboundEmail outbound;

    public ReplyToSalesEmailHandler(JpmsContext context, ISalesMailbox mailbox, SalesOutboundEmail outbound)
    {
        this.context = context;
        this.mailbox = mailbox;
        this.outbound = outbound;
    }

    public async Task<SalesReplyOutcome> HandleAsync(ReplyToSalesEmail command, CancellationToken cancellationToken)
    {
        if (!mailbox.IsConfigured) throw new InvalidOperationException("The sales mailbox isn't connected on the API.");
        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, cancellationToken);
        var lead = await LeadAsync(command.LeadId, cancellationToken);
        var outcome = await outbound.ReplyAsync(
            command.MessageId,
            ComposeHtmlPipeline.FromPlainText(command.Body),
            lead?.LeadId,
            lead?.DisplayReference ?? "",
            cancellationToken);
        if (lead is not null && (outcome.Sent || outcome.DraftWebLink is not null))
            await LogOnLeadAsync(lead, command, snapshot, outcome, cancellationToken);
        return outcome;
    }

    private Task<LeadEntity?> LeadAsync(string? leadId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leadId)) return Task.FromResult<LeadEntity?>(null);
        return context.Leads.AsNoTracking().FirstOrDefaultAsync(row => row.LeadId == leadId, cancellationToken);
    }

    private async Task LogOnLeadAsync(
        LeadEntity lead,
        ReplyToSalesEmail command,
        MailboxSnapshot? snapshot,
        SalesReplyOutcome outcome,
        CancellationToken cancellationToken)
    {
        context.LeadActivities.Add(new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Kind = (int)LeadActivityKind.Email,
            Summary = (outcome.Sent ? "Replied from " : "Drafted a reply from ") + mailbox.Address
                + (snapshot is null ? "" : $" to {snapshot.FromEmail} re \"{snapshot.Subject}\"")
                + ": " + Clip(command.Body.Trim(), 1500),
            OccurredAt = DateTimeOffset.UtcNow,
            RecordedByEmail = command.SentByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}
