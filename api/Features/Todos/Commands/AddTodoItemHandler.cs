using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class AddTodoItemHandler : ICommandHandler<AddTodoItem, TodoItem>
{
    private readonly JpmsContext context;
    public AddTodoItemHandler(JpmsContext context) { this.context = context; }

    public async Task<TodoItem> HandleAsync(AddTodoItem command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var nextNumber = (await context.TodoItems.MaxAsync(t => (int?)t.Number, cancellationToken) ?? 0) + 1;

        var entity = new TodoItemEntity
        {
            TodoItemId = TodosIdentifierFactory.Next(),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Title = Clamp(command.Title.Trim(), 256),
            Notes = Clamp(command.Notes?.Trim() ?? "", 2048),
            AssigneeEmail = Clamp(command.AssigneeEmail?.Trim() ?? "", 256),
            CreatedByEmail = command.CreatedByEmail,
            IsComplete = false,
            CreatedAt = DateTimeOffset.UtcNow,
            DueAt = command.DueAt
        };

        context.TodoItems.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
