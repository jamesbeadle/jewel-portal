using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

public sealed class UpdateTodoItemHandler : ICommandHandler<UpdateTodoItem, TodoItem>
{
    private readonly JpmsContext context;
    private readonly TodoActivityRecorder activity;
    private readonly TodoCompletionRecordTagger completionTagger;
    public UpdateTodoItemHandler(JpmsContext context, TodoActivityRecorder activity, TodoCompletionRecordTagger completionTagger)
    {
        this.context = context;
        this.activity = activity;
        this.completionTagger = completionTagger;
    }

    public async Task<TodoItem> HandleAsync(UpdateTodoItem command, CancellationToken cancellationToken)
    {
        var entity = await context.TodoItems.FindAsync(new object[] { command.TodoItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"To-do item {command.TodoItemId} not found.");

        // A pinned person must currently hold the assigned role in the directory.
        await TodoAssigneeGuard.EnsurePersonHoldsRoleAsync(
            context, command.AssigneeRole, command.AssigneePersonEmail, cancellationToken);

        var before = Snapshot(entity);
        entity.Title = Clamp(command.Title.Trim(), 256);
        entity.Notes = Clamp(command.Notes?.Trim() ?? "", 2048);
        entity.AssigneeRole = (int?)command.AssigneeRole;
        entity.AssigneePersonEmail = TodoAssigneeGuard.NormalisePersonEmail(command.AssigneePersonEmail);
        entity.DueAt = command.DueAt;

        var wasComplete = entity.IsComplete;
        entity.IsComplete = command.IsComplete;
        if (!wasComplete && command.IsComplete) entity.CompletedAt = DateTimeOffset.UtcNow;
        if (wasComplete && !command.IsComplete) ReopenAsOpen(entity);

        // Every changed fact is its own timeline line, in one save with the change.
        foreach (var line in TodoActivitySummaries.ForUpdate(before, entity))
            activity.Record(entity, line.Kind, line.Summary);

        await context.SaveChangesAsync(cancellationToken);

        // Ticking the item off also files its source email(s) to the records the item or the
        // email names (the variation it asked for, the work order it raised) — best-effort, after
        // the completion itself is safely saved. See TodoCompletionRecordTagger.
        if (!wasComplete && command.IsComplete)
            await completionTagger.TagSourceEmailsAsync(entity, actorEmail: null, cancellationToken);

        return entity.ToModel(await context.PersonNamesForAsync(new[] { entity }, cancellationToken));
    }

    // Reopening puts the item back to Open, not In progress: whoever picks it up again says so.
    private static void ReopenAsOpen(TodoItemEntity entity)
    {
        entity.CompletedAt = null;
        entity.StartedAt = null;
        entity.StartedByEmail = null;
    }

    private static TodoItemEntity Snapshot(TodoItemEntity entity) => new()
    {
        Title = entity.Title,
        Notes = entity.Notes,
        AssigneeRole = entity.AssigneeRole,
        AssigneePersonEmail = entity.AssigneePersonEmail,
        DueAt = entity.DueAt,
        IsComplete = entity.IsComplete,
    };

    private static string Clamp(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
