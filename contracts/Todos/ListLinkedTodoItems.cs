using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// The to-do items LINKED to one item: every other item that shares a tagged email with it. The
// link is the mail tag itself — an email tagged "JPMS/TODO-0007" and "JPMS/TODO-0012" ties those
// two items together — so linking happens wherever tagging happens (several items raised from one
// email in a Control Centre apply, or an existing item ticked in System Tags), and untagging the
// last shared email unlinks. Read live, ordered like every to-do list (open first). Empty when the
// item is gone or shares no mail.
public sealed record ListLinkedTodoItems(string TodoItemId) : IQuery<IReadOnlyList<TodoItem>>;
