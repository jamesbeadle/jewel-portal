using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Deletes the to-do row. Any "JPMS/TODO-####" mailbox tags left behind are harmless — they simply no
// longer resolve to a record — and can be removed from the triage Tagged view like any other tag.
public sealed class DeleteTodoItemHandler : ICommandHandler<DeleteTodoItem, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteTodoItemHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteTodoItem command, CancellationToken cancellationToken)
    {
        var entity = await context.TodoItems.FindAsync(new object[] { command.TodoItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"To-do item {command.TodoItemId} not found.");
        context.TodoItems.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.TodoItemId);
    }
}
