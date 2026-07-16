using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// Every to-do item in the system — general (no-project) and project items alike, regardless of
// assignee. The MD's / administrators' To-dos browser read; the endpoint carries the
// TodoRoles.AllowedToSeeAllTodos gate.
public sealed class ListAllTodoItemsHandler : IQueryHandler<ListAllTodoItems, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    public ListAllTodoItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListAllTodoItems query, CancellationToken cancellationToken)
    {
        var entities = await context.TodoItems.AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .InListOrder()
            .Select(t => t.ToModel())
            .ToList()
            .AsReadOnly();
    }
}
