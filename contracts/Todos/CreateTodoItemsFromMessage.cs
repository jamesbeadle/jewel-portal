using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// Create one or more to-do items from a mailbox message at the triage stage. The email (and its
// thread) is tagged "JPMS/TODO-####" for every item created, so each item reads its linked mail
// back live by its own tag — the same mechanism as requests and bid packages, and the reason a
// single email can feed several to-dos. Each Items row may name SEVERAL assignees — roles, each
// optionally pinned to a named holder — and fans out into one item per assignee (see
// TodoItemDraft), so the number of items created is normally larger than Items.Count. ProjectId null/blank creates GENERAL (company-wide) items
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
    // The pathway the triager worked down ("Internal" / "Subcontractor"). To-dos are pathway-NEUTRAL:
    // this only files the thread under that pathway when the thread has no pathway yet — a to-do
    // raised from a client email leaves the thread Client. "Client" is ignored (the wall is only
    // crossed into by an explicit client record). Null = no pathway involvement.
    string? Pathway = null,
    // How far the to-do tags (and the optional request link / pathway stamp) spread across the
    // email's conversation -- the same LinkThreadScope as LinkMessageToRecord. Default keeps the
    // long-standing anchor+thread-behind sweep for existing callers; the Control Centre passes
    // MessageOnly, or EntireThread when its "triage the entire thread" box is ticked.
    LinkThreadScope Scope = LinkThreadScope.ThreadBehindAnchor) : ICommand<IReadOnlyList<TodoItem>>;
