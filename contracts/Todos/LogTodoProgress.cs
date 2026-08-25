using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Log progress on a to-do by hand from its page: "Working on it" (Started), a chase with a note
// ("Emailed Justine for the payment certificate" — Chased), or a plain Note. Started and Chased
// move an Open item to In progress; nothing here completes it — the item stays open until the
// other side comes back and someone marks it done. Every call writes one timeline line
// (TodoActivity). ActorEmail is stamped server-side from the signed-in user.
//
// Who may log: the manage gate, or anyone the item is currently assigned to — the same pair of
// gates that let an assignee tick their own item off.
public sealed record LogTodoProgress(
    string TodoItemId,
    TodoActivityKind Kind,
    string? Note = null,
    string ActorEmail = "") : ICommand<TodoItem>;

// The item's timeline, newest first — every line the commands and the page's emails have written.
public sealed record ListTodoActivity(string TodoItemId) : IQuery<IReadOnlyList<TodoActivity>>;
