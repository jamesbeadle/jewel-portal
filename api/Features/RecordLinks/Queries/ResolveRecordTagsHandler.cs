using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Queries;

// Mailbox tag stems back to the records they name, for the tagged-email search's chips. Each stem
// is offered to every tag-resolving provider (each recognises its own grammar cheaply before
// touching the database — see ITagResolvingProvider); the first non-null answer wins, and a stem
// nobody claims is simply absent from the result. Workflow tags ("Discarded", "Replied") match no
// grammar and fall out the same way, so callers can pass an email's categories wholesale.
public sealed class ResolveRecordTagsHandler
    : IQueryHandler<ResolveRecordTags, IReadOnlyList<LinkableRecord>>
{
    // A UI asks about one search page's worth of tags; anything past this cap is a runaway caller.
    private const int MaxTags = 50;

    private readonly RecordProviderRegistry providers;

    public ResolveRecordTagsHandler(RecordProviderRegistry providers) { this.providers = providers; }

    public async Task<IReadOnlyList<LinkableRecord>> HandleAsync(
        ResolveRecordTags query, CancellationToken cancellationToken)
    {
        var resolvers = providers.All.OfType<ITagResolvingProvider>().ToList();
        var results = new List<LinkableRecord>();
        foreach (var tag in query.Tags
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTags))
        {
            foreach (var resolver in resolvers)
            {
                var record = await resolver.FindByTagAsync(tag, cancellationToken);
                if (record is not null) { results.Add(record); break; }
            }
        }
        return results;
    }
}
