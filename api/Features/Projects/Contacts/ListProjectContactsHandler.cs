using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

public sealed class ListProjectContactsHandler : IQueryHandler<ListProjectContacts, IReadOnlyList<ProjectContact>>
{
    private readonly JpmsContext context;
    public ListProjectContactsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProjectContact>> HandleAsync(ListProjectContacts query, CancellationToken cancellationToken)
    {
        var entities = await context.ProjectContacts
            .Where(contact => contact.ProjectId == query.ProjectId)
            .OrderBy(contact => contact.Role)
            .ThenBy(contact => contact.Name)
            .ToListAsync(cancellationToken);

        // Linked rows (per-project overrides of a party contact) render with the party contact's
        // current name/email so party-level edits show through on every project.
        var linkedIds = entities
            .Where(e => e.PartyContactId is not null)
            .Select(e => e.PartyContactId!)
            .Distinct()
            .ToList();
        var sources = linkedIds.Count == 0
            ? new Dictionary<string, Data.Entities.PartyContactEntity>()
            : await context.PartyContacts
                .Where(p => linkedIds.Contains(p.PartyContactId))
                .ToDictionaryAsync(p => p.PartyContactId, cancellationToken);

        return entities
            .Select(entity => entity.PartyContactId is not null && sources.TryGetValue(entity.PartyContactId, out var source)
                ? entity.ToModel(source)
                : entity.ToModel())
            .ToList()
            .AsReadOnly();
    }
}
