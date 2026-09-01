using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;

namespace Jewel.JPMS.Api.Features.Todos;

// Writes the to-do timeline. Each Record call ADDS one TodoItemActivities row to the context and
// leaves saving to the calling handler, so the line lands in the same SaveChanges as the change
// it describes — never a change without its line, never a line without its change. The actor is
// the signed-in user the endpoint stamped into AuditActor, unless the caller names one (commands
// that already carry CreatedByEmail / ActorEmail pass it through).
//
// Starting is a side effect of some kinds (TodoProgressKinds.StartsTheItem): an Open item that is
// chased, started or emailed from becomes In progress here, in one place, so every path agrees.
public sealed class TodoActivityRecorder
{
    private readonly JpmsContext context;
    private readonly AuditActor actor;

    public TodoActivityRecorder(JpmsContext context, AuditActor actor)
    {
        this.context = context;
        this.actor = actor;
    }

    public void Record(TodoItemEntity item, TodoActivityKind kind, string summary, string? actorEmail = null, DateTimeOffset? occurredAt = null)
    {
        var when = occurredAt ?? DateTimeOffset.UtcNow;
        var who = string.IsNullOrWhiteSpace(actorEmail) ? actor.Email : actorEmail;
        context.TodoItemActivities.Add(new TodoItemActivityEntity
        {
            TodoItemActivityId = TodosIdentifierFactory.Next(),
            TodoItemId = item.TodoItemId,
            Kind = (int)kind,
            Summary = Clamp(summary, 512),
            ActorEmail = Clamp(who, 256),
            OccurredAt = when,
        });
        if (TodoProgressKinds.StartsTheItem(kind)) MarkStarted(item, who, when);
    }

    private static void MarkStarted(TodoItemEntity item, string who, DateTimeOffset when)
    {
        if (item.IsComplete || item.StartedAt is not null) return;
        item.StartedAt = when;
        item.StartedByEmail = who;
    }

    private static string Clamp(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
