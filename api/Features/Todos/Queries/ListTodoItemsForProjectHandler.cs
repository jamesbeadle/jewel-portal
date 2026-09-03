using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

public sealed class ListTodoItemsForProjectHandler : IQueryHandler<ListTodoItemsForProject, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;
    public ListTodoItemsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListTodoItemsForProject query, CancellationToken cancellationToken)
    {
        // TodosOrdering.InListOrder: open items in number order, then the done pile newest-first.
        var entities = await context.TodoItems.AsNoTracking()
            .Where(t => t.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var personNames = await context.PersonNamesForAsync(entities, cancellationToken);
        return entities
            .InListOrder()
            .Select(t => t.ToModel(personNames))
            .ToList()
            .AsReadOnly();
    }
}
