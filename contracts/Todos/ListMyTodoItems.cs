using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Every to-do item assigned to any of the signed-in user's ROLES — general (no-project) items and
// project items alike. Backs the "My to-dos" dashboard panel and the To-dos browser for non-admin
// roles. Roles is stamped from the signed-in user's session server-side — the client never
// chooses whose items it reads.
public sealed record ListMyTodoItems(IReadOnlyList<Role>? Roles = null) : IQuery<IReadOnlyList<TodoItem>>;
