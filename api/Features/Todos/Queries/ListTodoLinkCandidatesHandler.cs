using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// The pool a "link to a to-do" picker offers: every item on the given project PLUS the general
// (company-wide, blank-project) items — the same scope a Control Centre draft can land on.
// Blank/null ProjectId = the company-wide items alone. Canonical list order (open items lead),
// so the picker reads like the list pages.
public sealed class ListTodoLinkCandidatesHandler : IQueryHandler<ListTodoLinkCandidates, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    public ListTodoLinkCandidatesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListTodoLinkCandidates query, CancellationToken cancellationToken)
    {
        // Blank-to-"" plus trim, mirroring how the rows store the general (no-project) value.
        var projectId = query.ProjectId?.Trim() ?? "";
        var entities = await context.TodoItems.AsNoTracking()
            .Where(item => item.ProjectId == projectId || item.ProjectId == "")
            .ToListAsync(cancellationToken);

        var personNames = await context.PersonNamesForAsync(entities, cancellationToken);
        return entities
            .InListOrder()
            .Select(entity => entity.ToModel(personNames))
            .ToList()
            .AsReadOnly();
    }
}
