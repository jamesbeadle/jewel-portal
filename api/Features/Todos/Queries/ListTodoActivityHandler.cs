using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// The item's timeline, newest first — the page reads the story top-down from the latest line.
public sealed class ListTodoActivityHandler : IQueryHandler<ListTodoActivity, IReadOnlyList<TodoActivity>>
{
    private readonly JpmsContext context;
    public ListTodoActivityHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoActivity>> HandleAsync(ListTodoActivity query, CancellationToken cancellationToken)
    {
        var rows = await context.TodoItemActivities.AsNoTracking()
            .Where(row => row.TodoItemId == query.TodoItemId)
            .OrderByDescending(row => row.OccurredAt)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}
