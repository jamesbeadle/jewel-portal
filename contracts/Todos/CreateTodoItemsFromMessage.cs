using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Create one or more to-do items from a mailbox message at the triage stage. The email (and its
// thread) is tagged "JPMS/TODO-####" for every item created, so each item reads its linked mail
// back live by its own tag — the same mechanism as requests and bid packages, and the reason a
// single email can feed several to-dos. ProjectId null/blank creates GENERAL (company-wide) items
// that belong to no project — the triage "General to-do" path for company-wide emails.
// CreatedByEmail is stamped from the signed-in user server-side.
//
// LinkRequestId optionally names an existing open request on the same project: the email is then
// ALSO tagged to that request (one request tag + one tag per item), so a single triage action can
// feed the request's conversation and capture its follow-up to-dos in one go. The request tag is
// applied and verified first — if it can't be stamped, no items are created. Requires a ProjectId
// (general, no-project items can't link to a request — every request belongs to a project).
public sealed record CreateTodoItemsFromMessage(
    string MessageId,
    string? ProjectId,
    IReadOnlyList<TodoItemDraft> Items,
    string? LinkRequestId = null,
    string? InternetMessageId = null,
    string CreatedByEmail = "",
    // The pathway the triager worked down. To-dos are pathway-NEUTRAL: this only files the thread
    // under Internal when it is "Internal" AND the thread has no pathway yet — a to-do raised from
    // a client email leaves the thread Client. Null = no pathway involvement.
    string? Pathway = null) : ICommand<IReadOnlyList<TodoItem>>;
