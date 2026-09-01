using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Todos.Commands;

// Writes the timeline line and — for Started and Chased — moves an Open item to In progress.
// Nothing here completes an item: "I've chased it" is the whole point of the command, and the
// item stays open until the other side comes back and someone marks it done. Logging "Started"
// on an item that is already in progress adds nothing (the button is hidden then anyway).
public sealed class LogTodoProgressHandler : ICommandHandler<LogTodoProgress, TodoItem>
{
    private readonly JpmsContext context;
    private readonly TodoActivityRecorder activity;

    public LogTodoProgressHandler(JpmsContext context, TodoActivityRecorder activity)
    {
        this.context = context;
        this.activity = activity;
    }

    public async Task<TodoItem> HandleAsync(LogTodoProgress command, CancellationToken cancellationToken)
    {
        var entity = await context.TodoItems.FindAsync(new object[] { command.TodoItemId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"To-do item {command.TodoItemId} not found.");
        if (entity.IsComplete) throw new InvalidOperationException("This item is done — reopen it before logging progress on it.");

        var isRedundantStart = command.Kind == TodoActivityKind.Started && entity.StartedAt is not null;
        if (!isRedundantStart)
        {
            activity.Record(entity, command.Kind, SummaryFor(command), command.ActorEmail);
            await context.SaveChangesAsync(cancellationToken);
        }
        return entity.ToModel(await context.PersonNamesForAsync(new[] { entity }, cancellationToken));
    }

    private static string SummaryFor(LogTodoProgress command) => command.Kind switch
    {
        TodoActivityKind.Chased => TodoActivitySummaries.ChaseSummary(command.Note),
        TodoActivityKind.Note => command.Note!.Trim(),
        _ => string.IsNullOrWhiteSpace(command.Note) ? "Working on it" : $"Working on it — {command.Note.Trim()}",
    };
}
