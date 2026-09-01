using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Deletes the to-do row. Any "JPMS/TODO-####" mailbox tags left behind are harmless — they simply no
// longer resolve to a record — and can be removed from the triage Tagged view like any other tag.
// To-do ↔ to-do link rows are NOT harmless leftovers (the other item would keep listing a ghost),
// so the ones naming this item go with it — manual sweep, because the house style declares no FKs.
public sealed class DeleteTodoItemHandler : ICommandHandler<DeleteTodoItem, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteTodoItemHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteTodoItem command, CancellationToken cancellationToken)
    {
        var entity = await context.TodoItems.FindAsync(new object[] { command.TodoItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"To-do item {command.TodoItemId} not found.");
        var links = await context.Touching(command.TodoItemId).ToListAsync(cancellationToken);
        context.TodoItemLinks.RemoveRange(links);
        var activity = await context.TodoItemActivities
            .Where(row => row.TodoItemId == command.TodoItemId)
            .ToListAsync(cancellationToken);
        context.TodoItemActivities.RemoveRange(activity);
        context.TodoItems.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.TodoItemId);
    }
}
