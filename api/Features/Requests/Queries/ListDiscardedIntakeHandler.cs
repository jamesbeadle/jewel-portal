using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListDiscardedIntakeHandler : IQueryHandler<ListDiscardedIntake, PagedResult<IntakeEmail>>
{
    private readonly JpmsContext context;
    public ListDiscardedIntakeHandler(JpmsContext context) { this.context = context; }

    public async Task<PagedResult<IntakeEmail>> HandleAsync(ListDiscardedIntake query, CancellationToken cancellationToken)
    {
        // Only the in-app "Discarded" outcome — NOT RemovedFromMailbox (those vanished from the Inbox
        // outside the app and were never a deliberate triage decision). Keeps this view to emails a
        // human chose to discard and could sensibly want back.
        var baseQuery = context.IntakeEmails.Where(e => e.Status == (int)IntakeStatus.Discarded);

        // Clamp paging defensively so a bad query string can't ask for everything or a negative page.
        var skip = Math.Max(0, query.Skip);
        var take = Math.Clamp(query.Take, 1, 100);

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
