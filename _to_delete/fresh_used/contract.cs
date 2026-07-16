using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Create one or more to-do items from a mailbox message at the triage stage. The email (and its
// thread) is tagged "JPMS/TODO-####" for every item created, so each item reads its linked mail
// back live by its own tag — the same mechanism as requests and bid packages, and the reason a
// single email can feed several to-dos. ProjectId null/blank creates GENERAL (company-wide) items
// that belong to no project — the triage "General to-do" path for company-wide emails.
// CreatedByEmail is stamped from the signed-in user server-side.
public sealed record CreateTodoItemsFromMessage(
    string MessageId,
    string? ProjectId,
    IReadOnlyList<TodoItemDraft> Items,
    string? InternetMessageId = null,
    string CreatedByEmail = "") : ICommand<IReadOnlyList<TodoItem>>;
