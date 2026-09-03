using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Kpi.Queries;

public sealed class ListKpiEmailsHandler : IQueryHandler<ListKpiEmails, IReadOnlyList<KpiEmail>>
{
    private readonly JpmsContext context;
    public ListKpiEmailsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<KpiEmail>> HandleAsync(ListKpiEmails query, CancellationToken cancellationToken)
    {
        var rows = context.KpiEmails.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.PersonId))
        {
            var personId = query.PersonId.Trim();
            rows = rows.Where(row => row.PersonId == personId);
        }
        var entities = await rows.OrderByDescending(row => row.MarkedAt).ToListAsync(cancellationToken);
        var people = await context.KpiPeople.AsNoTracking().ToDictionaryAsync(row => row.KpiPersonId, cancellationToken);
        return entities
            .Select(entity => entity.ToModel(people.GetValueOrDefault(entity.PersonId)))
            .ToList().AsReadOnly();
    }
}

public sealed class ListKpiPeopleHandler : IQueryHandler<ListKpiPeople, IReadOnlyList<KpiPerson>>
{
    private readonly JpmsContext context;
    public ListKpiPeopleHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<KpiPerson>> HandleAsync(ListKpiPeople query, CancellationToken cancellationToken)
    {
        var counts = (await context.KpiEmails.AsNoTracking()
                .GroupBy(row => row.PersonId)
                .Select(group => new { PersonId = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => row.PersonId, row => row.Count);
        var people = await context.KpiPeople.AsNoTracking().ToListAsync(cancellationToken);
        return people
            .Select(person => person.ToModel(counts.GetValueOrDefault(person.KpiPersonId)))
            .OrderBy(person => person.Name, StringComparer.OrdinalIgnoreCase)
            .ToList().AsReadOnly();
    }
}
