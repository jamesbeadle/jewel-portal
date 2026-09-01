using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Removes the stored link between two items, named in either order (the pair is normalised the
// same way it was written — TodoItemLinkPairs). A pair that isn't linked is a quiet no-op, never
// an error: the row the caller was looking at may simply have been unlinked from the other side
// first. Neither item needs to still exist — unlink is how a stray row would be cleared by hand.
public sealed class UnlinkTodoItemsHandler : ICommandHandler<UnlinkTodoItems, Acknowledgement>
{
    private readonly JpmsContext context;
    public UnlinkTodoItemsHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(UnlinkTodoItems command, CancellationToken cancellationToken)
    {
        var (aId, bId) = TodoItemLinkPairs.Normalise(command.TodoItemId, command.LinkedTodoItemId);
        var link = await context.TodoItemLinks
            .FirstOrDefaultAsync(row => row.TodoItemAId == aId && row.TodoItemBId == bId, cancellationToken);
        if (link is null) return new Acknowledgement(command.TodoItemId);

        context.TodoItemLinks.Remove(link);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.TodoItemId);
    }
}
