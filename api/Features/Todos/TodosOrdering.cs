using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Todos;

// The canonical to-do list order, shared by every list query: open items first in NUMBER order
// (TODO-0001 upwards — the reference is what people quote and scan for, so the list reads in
// the same sequence it was raised in; due dates are a badge, not the sort); completed items
// follow, most recently completed at the top of the done pile.
internal static class TodosOrdering
{
    public static IEnumerable<TodoItemEntity> InListOrder(this IEnumerable<TodoItemEntity> items) =>
        items
            .OrderBy(t => t.IsComplete)
            .ThenByDescending(t => t.IsComplete ? (t.CompletedAt ?? DateTimeOffset.MinValue) : DateTimeOffset.MinValue)
            .ThenBy(t => t.Number);
}
