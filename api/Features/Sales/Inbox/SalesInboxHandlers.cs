using Jewel.JPMS.Api.Data.Entities;
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
