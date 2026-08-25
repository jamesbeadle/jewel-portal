using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Re-files the item under a different project (or company-wide, blank ProjectId) and touches
// NOTHING else. The TODO-#### reference is global, so the mailbox tag — and with it the linked
// emails — moves with the item; assignee, due date and open/done state stay as they were.
public sealed class MoveTodoItemHandler : ICommandHandler<MoveTodoItem, TodoItem>
{
    private readonly JpmsContext context;
    private readonly TodoActivityRecorder activity;
    public MoveTodoItemHandler(JpmsContext context, TodoActivityRecorder activity) { this.context = context; this.activity = activity; }

    public async Task<TodoItem> HandleAsync(MoveTodoItem command, CancellationToken cancellationToken)
    {
        var entity = await context.TodoItems.FindAsync(new object[] { command.TodoItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"To-do item {command.TodoItemId} not found.");

        // Blank-to-"" plus trim, mirroring how the add paths store the general (no-project) value.
        var projectId = command.ProjectId?.Trim() ?? "";
        var projectLabel = "company-wide";
        if (projectId != "")
        {
            projectLabel = await context.Projects.AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.Reference + " — " + p.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Project '{projectId}' not found.");
        }

        entity.ProjectId = projectId;
        activity.Record(entity, TodoActivityKind.Moved, TodoActivitySummaries.MovedSummary(projectLabel));
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(await context.PersonNamesForAsync(new[] { entity }, cancellationToken));
    }
}
