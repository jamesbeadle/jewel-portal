using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// One to-do item by id — the read behind the item's own page (/todos/{id}), which is reached by a
// link rather than from an already-loaded list. Null when the item doesn't exist (deleted, or a
// stale link), which the page turns into its own "this item is gone" answer.
public sealed record GetTodoItemById(string TodoItemId) : IQuery<TodoItem?>;
