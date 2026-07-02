using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoItemsForProjectHandler : IQueryHandler<ListTodoItemsForProject, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    public ListTodoItemsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListTodoItemsForProject query, CancellationToken cancellationToken)
    {
        // Open items first (earliest due date, then oldest first); completed items follow, most
        // recently completed at the top of the done pile.
        var entities = await context.TodoItems.AsNoTracking()
            .Where(t => t.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(t => t.IsComplete)
            .ThenBy(t => t.IsComplete ? DateTimeOffset.MaxValue : (t.DueAt ?? DateTimeOffset.MaxValue))
            .ThenByDescending(t => t.IsComplete ? (t.CompletedAt ?? DateTimeOffset.MinValue) : DateTimeOffset.MinValue)
            .ThenBy(t => t.Number)
            .Select(t => t.ToModel())
            .ToList()
            .AsReadOnly();
    }
}
