using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Links two EXISTING items: one stored row per pair, ids in canonical order (TodoItemLinkPairs),
// so re-linking an already-linked pair — from either direction — finds the row and quietly does
// nothing. Both items must exist: a link to a ghost would sit invisibly until the sweep in
// DeleteTodoItemHandler has nothing to sweep it with.
public sealed class LinkTodoItemsHandler : ICommandHandler<LinkTodoItems, Acknowledgement>
{
    private readonly JpmsContext context;
    public LinkTodoItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(LinkTodoItems command, CancellationToken cancellationToken)
    {
        var ids = new[] { command.TodoItemId, command.LinkedTodoItemId };
        var found = await context.TodoItems.AsNoTracking()
            .Where(item => ids.Contains(item.TodoItemId))
            .Select(item => item.TodoItemId)
            .ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            if (!found.Contains(id))
                throw new InvalidOperationException($"To-do item {id} not found.");
        }

        var (aId, bId) = TodoItemLinkPairs.Normalise(command.TodoItemId, command.LinkedTodoItemId);
        var alreadyLinked = await context.TodoItemLinks
            .AnyAsync(link => link.TodoItemAId == aId && link.TodoItemBId == bId, cancellationToken);
        if (alreadyLinked) return new Acknowledgement(command.TodoItemId);

        context.TodoItemLinks.Add(new TodoItemLinkEntity
        {
            TodoItemLinkId = TodosIdentifierFactory.Next(),
            TodoItemAId = aId,
            TodoItemBId = bId,
            LinkedAt = DateTimeOffset.UtcNow,
            LinkedByEmail = command.LinkedByEmail
        });
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.TodoItemId);
    }
}
