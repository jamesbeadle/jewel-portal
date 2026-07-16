using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Add a single to-do item to a project from the Overview tab. Assignment is to a ROLE (null =
// unassigned) — see TodoItem. CreatedByEmail is stamped from the signed-in user server-side —
// never trusted from the client body.
public sealed record AddTodoItem(
    string ProjectId,
    string Title,
    string? Notes = null,
    Role? AssigneeRole = null,
    DateTimeOffset? DueAt = null,
    string CreatedByEmail = "") : ICommand<TodoItem>;
