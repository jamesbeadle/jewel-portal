using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

public sealed class ListSalesInboxHandler : IQueryHandler<ListSalesInbox, SalesInboxPage>
{
    private readonly JpmsContext context;
    private readonly ISalesMailbox mailbox;
    public ListSalesInboxHandler(JpmsContext context, ISalesMailbox mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<SalesInboxPage> HandleAsync(ListSalesInbox query, CancellationToken cancellationToken)
    {
        if (!mailbox.IsConfigured)
            return new SalesInboxPage(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0), Array.Empty<SalesInboxLeadMatch>(), mailbox.Address, false,
                "The sales mailbox isn't connected: the API needs the MailboxIntake Graph credentials, and the Exchange access policy must include " + mailbox.Address + ".");

        var page = string.IsNullOrWhiteSpace(query.Search)
            ? await mailbox.ListInboxAsync(query.Cursor, query.Take, query.NewestFirst, cancellationToken)
            : await mailbox.SearchAsync(query.Search.Trim(), Math.Clamp(query.Take, 1, 50), cancellationToken);

        var matches = await MatchesAsync(context, page.Items.Select(item => item.FromEmail), cancellationToken);
        var notice = page.Items.Count == 0 && page.Total == 0 && string.IsNullOrWhiteSpace(query.Search)
            ? "Nothing in the inbox — or Graph refused the read. If the mailbox has mail, the app registration's access policy probably doesn't include " + mailbox.Address + " yet."
            : null;
        return new SalesInboxPage(page, matches, mailbox.Address, true, notice);
    }

    /// <summary>The leads whose contact email is one of the senders — one match per address.</summary>
    internal static async Task<IReadOnlyList<SalesInboxLeadMatch>> MatchesAsync(JpmsContext context, IEnumerable<string> emails, CancellationToken ct)
    {
        var wanted = emails.Where(email => !string.IsNullOrWhiteSpace(email)).Select(email => email.Trim().ToLowerInvariant()).Distinct().ToList();
        if (wanted.Count == 0) return Array.Empty<SalesInboxLeadMatch>();
        var leads = await context.Leads.AsNoTracking()
            .Where(row => wanted.Contains(row.ContactEmail.ToLower()))
            .OrderByDescending(row => row.CapturedAt)
            .ToListAsync(ct);
        return leads
            .GroupBy(row => row.ContactEmail.Trim().ToLowerInvariant())
            .Select(group => group.First())
            .Select(row => new SalesInboxLeadMatch(row.ContactEmail.Trim().ToLowerInvariant(), row.LeadId, row.DisplayReference, row.ContactName, (LeadStage)row.Stage))
            .ToList();
    }
}

public sealed class GetSalesInboxConversationHandler : IQueryHandler<GetSalesInboxConversation, MailboxPage>
{
    private readonly ISalesMailbox mailbox;
    public GetSalesInboxConversationHandler(ISalesMailbox mailbox) { this.mailbox = mailbox; }

    public Task<MailboxPage> HandleAsync(GetSalesInboxConversation query, CancellationToken cancellationToken) =>
        mailbox.ListConversationAsync(query.ConversationId, cancellationToken);
}

public sealed class GetSalesInboxMessageHandler : IQueryHandler<GetSalesInboxMessage, MailboxMessageDetail>
{
    private readonly ISalesMailbox mailbox;
    public GetSalesInboxMessageHandler(ISalesMailbox mailbox) { this.mailbox = mailbox; }

    public Task<MailboxMessageDetail> HandleAsync(GetSalesInboxMessage query, CancellationToken cancellationToken) =>
        mailbox.GetDetailAsync(query.MessageId, cancellationToken);
}

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

public sealed class ReplyToSalesEmailHandler : ICommandHandler<ReplyToSalesEmail, SalesReplyOutcome>
{
    private readonly JpmsContext context;
    private readonly ISalesMailbox mailbox;
    public ReplyToSalesEmailHandler(JpmsContext context, ISalesMailbox mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<SalesReplyOutcome> HandleAsync(ReplyToSalesEmail command, CancellationToken cancellationToken)
    {
        if (!mailbox.IsConfigured) throw new InvalidOperationException("The sales mailbox isn't connected on the API.");
        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, cancellationToken);
        var outcome = await mailbox.ReplyAsync(command.MessageId, ComposeHtmlPipeline.FromPlainText(command.Body), cancellationToken);
        if (!string.IsNullOrWhiteSpace(command.LeadId) && (outcome.Sent || outcome.DraftWebLink is not null))
        {
            var lead = await context.Leads.AsNoTracking().FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken);
            if (lead is not null)
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
        }
        return outcome;
    }

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}

public sealed class LogSalesEmailToLeadAuthorisation
{
    public bool Allows(SignedInUser user, LogSalesEmailToLead command) => SalesRoles.SalesTeam.IncludesAny(user.Roles);
}

public sealed class LogSalesEmailToLeadValidation
{
    public ValidationOutcome Check(LogSalesEmailToLead command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (string.IsNullOrWhiteSpace(command.LeadId)) errors.Add("LeadId is required.");
        SalesFieldLimits.Check(errors, command.Note ?? "", 2000, "Note");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class LogSalesEmailToLeadHandler : ICommandHandler<LogSalesEmailToLead, LeadActivity>
{
    private readonly JpmsContext context;
    private readonly ISalesMailbox mailbox;
    public LogSalesEmailToLeadHandler(JpmsContext context, ISalesMailbox mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<LeadActivity> HandleAsync(LogSalesEmailToLead command, CancellationToken cancellationToken)
    {
        var lead = await context.Leads.FirstOrDefaultAsync(row => row.LeadId == command.LeadId, cancellationToken)
            ?? throw new InvalidOperationException($"Lead {command.LeadId} not found.");
        var snapshot = await mailbox.GetSnapshotAsync(command.MessageId, cancellationToken)
            ?? throw new InvalidOperationException("That email couldn't be read from the sales mailbox.");
        // A lead found by letter often has no email until they write in.
        if (string.IsNullOrWhiteSpace(lead.ContactEmail) && !string.IsNullOrWhiteSpace(snapshot.FromEmail))
            lead.ContactEmail = snapshot.FromEmail;
        var entity = new LeadActivityEntity
        {
            LeadActivityId = Guid.NewGuid().ToString("N"),
            LeadId = lead.LeadId,
            Kind = (int)LeadActivityKind.Email,
            Summary = $"Email from {snapshot.FromName} <{snapshot.FromEmail}> — \"{snapshot.Subject}\" ({snapshot.ReceivedAt.ToLocalTime():d MMM HH:mm}) in {mailbox.Address}."
                + (string.IsNullOrWhiteSpace(command.Note) ? (string.IsNullOrWhiteSpace(snapshot.BodyPreview) ? "" : $"\n{snapshot.BodyPreview.Trim()}") : $"\n{command.Note.Trim()}"),
            OccurredAt = snapshot.ReceivedAt,
            RecordedByEmail = command.RecordedByEmail
        };
        context.LeadActivities.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
