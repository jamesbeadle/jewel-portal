using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListRequestMessagesHandler : IQueryHandler<ListRequestMessages, IReadOnlyList<RequestMessage>>
{
    private readonly JpmsContext context;
    public ListRequestMessagesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<RequestMessage>> HandleAsync(ListRequestMessages query, CancellationToken cancellationToken)
    {
        var entities = await context.RequestMessages
            .Where(m => m.RequestId == query.RequestId)
            .OrderBy(m => m.PostedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
