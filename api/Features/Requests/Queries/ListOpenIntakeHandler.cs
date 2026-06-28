using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListOpenIntakeHandler : IQueryHandler<ListOpenIntake, PagedResult<IntakeEmail>>
{
    private readonly JpmsContext context;
    public ListOpenIntakeHandler(JpmsContext context) { this.context = context; }

    public async Task<PagedResult<IntakeEmail>> HandleAsync(ListOpenIntake query, CancellationToken cancellationToken)
    {
        // Mirror of the Inbox: only un-resolved rows. RemovedFromMailbox/Linked/Discarded drop out.
        var open = new[] { (int)IntakeStatus.NeedsTriage, (int)IntakeStatus.Claimed };

        // Clamp paging defensively so a bad query string can't ask for everything or a negative page.
        var skip = Math.Max(0, query.Skip);
        var take = Math.Clamp(query.Take, 1, 100);

        var baseQuery = context.IntakeEmails.Where(e => open.Contains(e.Status));

        var total = await baseQuery.CountAsync(cancellationToken);

        // Newest first, matching the triage UI. Skip/Take run server-side in SQL.
        var entities = await baseQuery
            .OrderByDescending(e => e.ReceivedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var items = entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
        return new PagedResult<IntakeEmail>(items, total, skip, take);
    }
}
