using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// Every to-do item assigned to any ROLE the signed-in user holds — general (no-project) and
// project items alike — EXCEPT items pinned to a different named person: a pin narrows an item
// from "everyone holding the role" to one person's list, so only that person (and the MD's
// see-everything read) finds it here. The endpoint stamps Roles and Email from the session, so
// the client never chooses whose items it reads. Backs the "My to-dos" dashboard panel and the
// To-dos browser for non-admin roles.
public sealed class ListMyTodoItemsHandler : IQueryHandler<ListMyTodoItems, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    public ListMyTodoItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListMyTodoItems query, CancellationToken cancellationToken)
    {
        if (query.Roles is null || query.Roles.Count == 0) return Array.Empty<TodoItem>();

        var roleValues = query.Roles.Select(role => (int)role).ToList();
        var email = (query.Email ?? "").Trim().ToLower();
        var entities = await context.TodoItems.AsNoTracking()
            .Where(t => t.AssigneeRole != null && roleValues.Contains(t.AssigneeRole.Value)
                && (t.AssigneePersonEmail == null || t.AssigneePersonEmail.ToLower() == email))
            .ToListAsync(cancellationToken);

        var personNames = await context.PersonNamesForAsync(entities, cancellationToken);
        return entities
            .InListOrder()
            .Select(t => t.ToModel(personNames))
            .ToList()
            .AsReadOnly();
    }
}
