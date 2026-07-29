using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Every to-do item assigned to any of the signed-in user's ROLES — general (no-project) items and
// project items alike, EXCEPT items pinned to a different named person: a pinned item is on the
// pinned person's list only, everyone else holding the role reads past it. Backs the "My to-dos"
// dashboard panel and the To-dos browser for non-admin roles. Roles and Email are stamped from the
// signed-in user's session server-side — the client never chooses whose items it reads.
public sealed record ListMyTodoItems(IReadOnlyList<Role>? Roles = null, string? Email = null) : IQuery<IReadOnlyList<TodoItem>>;
