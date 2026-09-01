using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// Follows each of the record's tagged conversations live and returns the members that arrived
// AFTER the newest tagged email of that conversation and don't carry the record's tag — the
// replies the page is blind to. Bounded: a record rarely spans more than a handful of threads,
// and each thread is one capped Graph page (ListConversationAsync), so the cost is a few reads.
// The selection rule itself is pure (UnfiledReplies.Select) and pinned by tests.
public sealed class ListUnfiledRepliesHandler : IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>>
{
    private const int MaxConversationsFollowed = 8;

    private readonly RecordProviderRegistry providers;
    private readonly RecordEmailReader emails;
    private readonly IMailboxGraphClient graph;
    private readonly ILogger<ListUnfiledRepliesHandler> logger;

    public ListUnfiledRepliesHandler(RecordProviderRegistry providers, RecordEmailReader emails, IMailboxGraphClient graph, ILogger<ListUnfiledRepliesHandler> logger)
    {
        this.providers = providers;
        this.emails = emails;
        this.graph = graph;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<MailboxMessage>> HandleAsync(ListUnfiledReplies query, CancellationToken cancellationToken)
    {
        if (!providers.TryGet(query.Type, out var provider)) return Array.Empty<MailboxMessage>();
        var record = await provider.FindAsync(query.RecordId, cancellationToken);
        if (record is null) return Array.Empty<MailboxMessage>();

        var tag = TriageCategories.ForRecord(record.TagReference);
        var tagged = await emails.ForRecordAsync(query.Type, query.RecordId, cancellationToken);
        var found = new List<MailboxMessage>();
        foreach (var conversation in UnfiledReplies.ConversationsOf(tagged).Take(MaxConversationsFollowed))
        {
            // Best-effort per thread: one throttled Graph read must not turn the whole notice —
            // an aside on the page — into a red bar. A thread that couldn't be read simply
            // contributes nothing this time.
            try
            {
                var thread = await graph.ListConversationAsync(conversation.ConversationId, cancellationToken);
                found.AddRange(UnfiledReplies.Select(thread.Items, tag, conversation.NewestTaggedAt));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Unfiled-replies read skipped conversation {ConversationId}.", conversation.ConversationId);
            }
        }
        return found.OrderByDescending(email => email.ReceivedAt).ToList();
    }
}
