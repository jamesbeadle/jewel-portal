using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Add a single GENERAL (company-wide) to-do item — one that belongs to no project. Added directly
// from the To-dos browser page; the triage equivalent is CreateTodoItemsFromMessage with a blank
// ProjectId. CreatedByEmail is stamped from the signed-in user server-side — never trusted from
// the client body.
public sealed record AddGeneralTodoItem(
    string Title,
    string? Notes = null,
    string? AssigneeEmail = null,
    DateTimeOffset? DueAt = null,
    string CreatedByEmail = "") : ICommand<TodoItem>;
