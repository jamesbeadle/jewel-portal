using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestMessagesHandler : IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>>
{
    private readonly JpmsContext context;
    private readonly RequestEmailReader emails;
    private readonly MailboxIntakeOptions mailboxOptions;
    public ListRequestMessagesHandler(
        JpmsContext context, RequestEmailReader emails, MailboxIntakeOptions mailboxOptions)
    { this.context = context; this.emails = emails; this.mailboxOptions = mailboxOptions; }

    public async Task<IReadOnlyList<RequestMessage>> HandleAsync(ListRequestMessages query, CancellationToken cancellationToken)
    {
        // No catch-up sync here: a reply that joins this request's threads after link time belongs
        // to the triage queue (every new arrival is its own triage decision) and appears in this
        // view only once triaged — tags spread across a thread solely at triage time.

        // Stored rows are the request's own in-app activity (notes, drafted-document audit lines).
        // The email legs — inbound AND the mailbox's own sent replies (read from Sent Items) — are
        // no longer stored; they're read live by tag, so exclude any stored Inbound rows (legacy
        // snapshots) and merge the live emails in their place, ordered by time.
        var stored = await context.RequestMessages
            .Where(m => m.RequestId == query.RequestId && m.Direction != (int)MessageDirection.Inbound)
            .ToListAsync(cancellationToken);

        var live = await emails.ForRequestAsync(query.RequestId, cancellationToken);

        return stored.Select(e => e.ToModel())
            .Concat(live.Select(e => e.ToConversationMessage(query.RequestId, mailboxOptions.Mailbox)))
            .OrderBy(m => m.PostedAt)
            .ToList()
            .AsReadOnly();
    }
}
