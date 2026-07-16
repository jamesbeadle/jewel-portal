using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Todos;

// The canonical to-do list order, shared by every list query: open items first (earliest due date,
// then oldest number first); completed items follow, most recently completed at the top of the
// done pile. Mirrors ListTodoItemsForProjectHandler's original inline ordering.
internal static class TodosOrdering
{
    public static IEnumerable<TodoItemEntity> InListOrder(this IEnumerable<TodoItemEntity> items) =>
        items
            .OrderBy(t => t.IsComplete)
            .ThenBy(t => t.IsComplete ? DateTimeOffset.MaxValue : (t.DueAt ?? DateTimeOffset.MaxValue))
            .ThenByDescending(t => t.IsComplete ? (t.CompletedAt ?? DateTimeOffset.MinValue) : DateTimeOffset.MinValue)
            .ThenBy(t => t.Number);
}
