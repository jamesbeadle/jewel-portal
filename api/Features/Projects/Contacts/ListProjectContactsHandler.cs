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
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
