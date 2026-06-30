using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestMessagesHandler : IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>>
{
    private readonly JpmsContext context;
    private readonly RequestEmailReader emails;
    public ListRequestMessagesHandler(JpmsContext context, RequestEmailReader emails)
    { this.context = context; this.emails = emails; }

    public async Task<IReadOnlyList<RequestMessage>> HandleAsync(ListRequestMessages query, CancellationToken cancellationToken)
    {
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
