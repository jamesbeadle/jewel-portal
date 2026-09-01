using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.ClientPortal;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ClientPortal.Queries;

public sealed class ListMyClientRequestMessagesHandler
    : IQueryHandler<ListMyClientRequestMessages, IReadOnlyList<RequestMessage>>
{
    private readonly JpmsContext context;
    public ListMyClientRequestMessagesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<RequestMessage>> HandleAsync(
        ListMyClientRequestMessages query, CancellationToken cancellationToken)
    {
        var isMine = await ClientProjects.OwnsRequestAsync(
            context, query.ClientId, query.RequestId, cancellationToken);
        if (!isMine) return Array.Empty<RequestMessage>();

        // Typed shared messages only. Internal notes stay internal, and email legs — stored or
        // live — never reach a client session: the client's thread is the in-app conversation.
        var stored = await context.RequestMessages
            .AsNoTracking()
            .Where(row => row.RequestId == query.RequestId
                && row.Visibility == (int)MessageVisibility.Shared
                && row.Direction == (int)MessageDirection.System)
            .OrderBy(row => row.PostedAt)
            .ToListAsync(cancellationToken);
        return stored.Select(row => row.ToModel()).ToList().AsReadOnly();
    }
}
