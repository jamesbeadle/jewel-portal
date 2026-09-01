using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Queries;

public sealed class ListUsefulInformationForProjectHandler : IQueryHandler<ListUsefulInformationForProject, IReadOnlyList<UsefulInformationNote>>
{
    private readonly JpmsContext context;
    public ListUsefulInformationForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<UsefulInformationNote>> HandleAsync(ListUsefulInformationForProject query, CancellationToken cancellationToken)
    {
        // A–Z by title (then oldest first for duplicate titles): the notes are reference material
        // looked up by name — "Front gate code" — not a feed read by recency.
        var entities = await context.UsefulInformationNotes.AsNoTracking()
            .Where(note => note.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(note => note.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(note => note.CreatedAt)
            .Select(note => note.ToModel())
            .ToList()
            .AsReadOnly();
    }
}
