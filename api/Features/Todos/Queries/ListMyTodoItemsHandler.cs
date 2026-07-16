using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// Every to-do item assigned to the signed-in user — general (no-project) and project items alike.
// The endpoint stamps AssigneeEmail from the session, so the client never chooses whose items it
// reads. Backs the "My to-dos" dashboard panel and the To-dos browser for non-admin roles.
public sealed class ListMyTodoItemsHandler : IQueryHandler<ListMyTodoItems, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    public ListMyTodoItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListMyTodoItems query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.AssigneeEmail)) return Array.Empty<TodoItem>();

        var email = query.AssigneeEmail.Trim();
        var entities = await context.TodoItems.AsNoTracking()
            .Where(t => t.AssigneeEmail == email)
            .ToListAsync(cancellationToken);

        return entities
            .InListOrder()
            .Select(t => t.ToModel())
            .ToList()
            .AsReadOnly();
    }
}
