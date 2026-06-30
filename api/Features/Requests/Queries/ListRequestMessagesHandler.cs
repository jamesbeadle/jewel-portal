using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestMessagesHandler : IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>>
{
    private readonly JpmsContext context;
    private readonly RequestEmailReader emails;
    private readonly RecordThreadTagger threadTagger;
    public ListRequestMessagesHandler(JpmsContext context, RequestEmailReader emails, RecordThreadTagger threadTagger)
    { this.context = context; this.emails = emails; this.threadTagger = threadTagger; }

    public async Task<IReadOnlyList<RequestMessage>> HandleAsync(ListRequestMessages query, CancellationToken cancellationToken)
    {
        // Catch-up: before reading, pull in any replies that joined this request's email threads since it
        // was linked, so the conversation view shows the full, current thread. Best-effort — a mailbox
        // hiccup must never break the read; it only tags genuinely-new messages, so it's cheap when idle.
        var request = await context.Requests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == query.RequestId, cancellationToken);
        if (request is not null)
        {
            try { await threadTagger.SyncThreadsAsync(TriageCategories.ForRecord(request.TagReference), cancellationToken); }
            catch { /* best-effort: never fail the conversation view on a tagging hiccup */ }
        }

        // Stored rows are the request's own in-app activity (notes, outbound sends). The inbound email
        // leg is no longer stored — it's read live by tag — so exclude any stored Inbound rows (legacy
        // snapshots) and merge the live emails in their place, ordered by time.
        var stored = await context.RequestMessages
            .Where(m => m.RequestId == query.RequestId && m.Direction != (int)MessageDirection.Inbound)
            .ToListAsync(cancellationToken);

        var live = await emails.ForRequestAsync(query.RequestId, cancellationToken);

        return stored.Select(e => e.ToModel())
            .Concat(live.Select(e => e.ToInboundMessage(query.RequestId)))
            .OrderBy(m => m.PostedAt)
            .ToList()
            .AsReadOnly();
    }
}
