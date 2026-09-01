using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Add a single GENERAL (company-wide) to-do item — one that belongs to no project (ProjectId "").
// Mirrors AddTodoItemHandler minus the project; the item shares the same global TODO-#### number
// sequence, so its reference can still be used as a mailbox tag stem later.
public sealed class AddGeneralTodoItemHandler : ICommandHandler<AddGeneralTodoItem, TodoItem>
{
    private readonly JpmsContext context;
    private readonly TodoActivityRecorder activity;
    public AddGeneralTodoItemHandler(JpmsContext context, TodoActivityRecorder activity) { this.context = context; this.activity = activity; }

    public async Task<TodoItem> HandleAsync(AddGeneralTodoItem command, CancellationToken cancellationToken)
    {
        // A pinned person must currently hold the assigned role in the directory.
        await TodoAssigneeGuard.EnsurePersonHoldsRoleAsync(
            context, command.AssigneeRole, command.AssigneePersonEmail, cancellationToken);

        var nextNumber = (await context.TodoItems.MaxAsync(t => (int?)t.Number, cancellationToken) ?? 0) + 1;

        var entity = new TodoItemEntity
        {
            TodoItemId = TodosIdentifierFactory.Next(),
            ProjectId = "",
            Number = nextNumber,
            Title = Clamp(command.Title.Trim(), 256),
            Notes = Clamp(command.Notes?.Trim() ?? "", 2048),
            AssigneeRole = (int?)command.AssigneeRole,
            AssigneePersonEmail = TodoAssigneeGuard.NormalisePersonEmail(command.AssigneePersonEmail),
            CreatedByEmail = command.CreatedByEmail,
            IsComplete = false,
            CreatedAt = DateTimeOffset.UtcNow,
            DueAt = command.DueAt
        };

        context.TodoItems.Add(entity);
        activity.Record(entity, TodoActivityKind.Created, TodoActivitySummaries.CreatedSummary(entity), command.CreatedByEmail);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(await context.PersonNamesForAsync(new[] { entity }, cancellationToken));
    }

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
