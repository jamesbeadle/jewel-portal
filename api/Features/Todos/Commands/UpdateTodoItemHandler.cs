using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UpdateTodoItemHandler : ICommandHandler<UpdateTodoItem, TodoItem>
{
    private readonly JpmsContext context;
    public UpdateTodoItemHandler(JpmsContext context) { this.context = context; }

    public async Task<TodoItem> HandleAsync(UpdateTodoItem command, CancellationToken cancellationToken)
    {
        var entity = await context.TodoItems.FindAsync(new object[] { command.TodoItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"To-do item {command.TodoItemId} not found.");

        entity.Title = Clamp(command.Title.Trim(), 256);
        entity.Notes = Clamp(command.Notes?.Trim() ?? "", 2048);
        entity.AssigneeRole = (int?)command.AssigneeRole;
        entity.DueAt = command.DueAt;

        var wasComplete = entity.IsComplete;
        entity.IsComplete = command.IsComplete;
        if (!wasComplete && command.IsComplete) entity.CompletedAt = DateTimeOffset.UtcNow;
        if (wasComplete && !command.IsComplete) entity.CompletedAt = null;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
