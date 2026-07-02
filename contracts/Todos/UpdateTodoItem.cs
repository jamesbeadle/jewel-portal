using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Full-row update of a to-do item (details + open/done state). Completing stamps CompletedAt
// server-side; reopening clears it.
public sealed record UpdateTodoItem(
    string TodoItemId,
    string Title,
    string? Notes,
    string? AssigneeEmail,
    DateTimeOffset? DueAt,
    bool IsComplete) : ICommand<TodoItem>;
