using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Every to-do item assigned to the signed-in user — general (no-project) items and project items
// alike. Backs the "My to-dos" dashboard panel and the To-dos browser for non-admin roles.
// AssigneeEmail is stamped from the signed-in user server-side — the client never chooses whose
// items it reads.
public sealed record ListMyTodoItems(string AssigneeEmail = "") : IQuery<IReadOnlyList<TodoItem>>;
