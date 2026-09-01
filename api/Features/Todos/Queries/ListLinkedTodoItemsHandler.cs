using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Queries;

// The to-do items linked to one item: both directions of the stored undirected pairs
// (TodoItemLinks — one row per pair in canonical id order, TodoItemLinkPairs), in the canonical
// list order. Stored rows replaced the earlier mail-tag derivation (an item "linked" whenever an
// email carried both TODO-#### tags): a link is now something somebody chose — made when a
// Control Centre draft names existing items, or from the detail modal — and it survives the
// mail tags changing underneath it.
public sealed class ListLinkedTodoItemsHandler : IQueryHandler<ListLinkedTodoItems, IReadOnlyList<TodoItem>>
{
    private readonly JpmsContext context;

    public ListLinkedTodoItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TodoItem>> HandleAsync(ListLinkedTodoItems query, CancellationToken cancellationToken)
    {
        var itemExists = await context.TodoItems.AsNoTracking()
            .AnyAsync(item => item.TodoItemId == query.TodoItemId, cancellationToken);
        if (!itemExists) return Array.Empty<TodoItem>();

        var entities = await context.LinkedItemsForAsync(query.TodoItemId, cancellationToken);
        if (entities.Count == 0) return Array.Empty<TodoItem>();

        var personNames = await context.PersonNamesForAsync(entities, cancellationToken);
        return entities
            .InListOrder()
            .Select(entity => entity.ToModel(personNames))
            .ToList()
            .AsReadOnly();
    }
}
