using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

/// <summary>Free-text mailbox search for the record pages' "Find emails" dialog. A thin pass-through
/// to the Graph client's $search — relevance order preserved, one page, drafts already excluded.</summary>
public sealed class SearchMailboxMessagesHandler
    : IQueryHandler<SearchMailboxMessages, IReadOnlyList<MailboxMessage>>
{
    private readonly IMailboxGraphClient graph;

    public SearchMailboxMessagesHandler(IMailboxGraphClient graph)
    {
        this.graph = graph;
    }

    public async Task<IReadOnlyList<MailboxMessage>> HandleAsync(
        SearchMailboxMessages query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Query)) return Array.Empty<MailboxMessage>();
        var page = await graph.SearchAsync(query.Query.Trim(), query.Take, cancellationToken);
        return page.Items;
    }
}
