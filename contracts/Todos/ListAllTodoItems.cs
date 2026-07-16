using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Every to-do item in the system — general (no-project) and project items alike, regardless of
// assignee. Backs the To-dos browser for managing directors and administrators only (see
// TodoRoles.AllowedToSeeAllTodos in the api); everyone else reads ListMyTodoItems instead.
public sealed record ListAllTodoItems : IQuery<IReadOnlyList<TodoItem>>;
