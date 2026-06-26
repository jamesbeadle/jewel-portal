using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

public sealed class ListOpenIntakeHandler : IQueryHandler<ListOpenIntake, IReadOnlyList<IntakeEmail>>
{
    private readonly JpmsContext context;
    public ListOpenIntakeHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<IntakeEmail>> HandleAsync(ListOpenIntake query, CancellationToken cancellationToken)
    {
        var open = new[] { (int)IntakeStatus.NeedsTriage, (int)IntakeStatus.Claimed };
        var entities = await context.IntakeEmails
            .Where(e => open.Contains(e.Status))
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
