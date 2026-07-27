namespace Jewel.JPMS.Models;

// A to-do item. Created directly on a project's To-do tab, from an email at the triage stage
// (several at once), or as a GENERAL company-wide item that belongs to no project (ProjectId is ""
// then) — captured from a company-wide email at triage or added on the To-dos browser page. Each
// item owns a sequential "TODO-0001" reference which is also its mailbox tag stem, so an email
// tagged "JPMS/TODO-0001" is the item's linked mail — the same live-read link mechanism the
// Request / Bid Package families use.
//
// Items are assigned to a ROLE, not a person (null = unassigned). Everyone currently holding the
// role sees the item on their list and may tick it off; when someone leaves and a new starter
// takes over the role, the open items are simply theirs — nothing needs re-assigning.
public sealed record TodoItem(
    string TodoItemId,
    string ProjectId,        // "" = general (company-wide) item with no project
    string Reference,        // sequential human reference, e.g. "TODO-0001" (also the tag stem)
    string Title,
    string Notes,
    Role? AssigneeRole,      // null = unassigned; otherwise a TodoRoles.AssignableAsTodoAssignee role
    string CreatedByEmail,
    bool IsComplete,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt);

// One row of the triage "create to-dos from this email" form. The command carries a list of these
// so several items can be captured from a single email in one action.
//
// AssigneeRoles names EVERY role the row is meant for, and the row FANS OUT into one TodoItem per
// role — same title, detail and due date, but its own TODO-#### reference, its own mail tag and its
// own tick-box. One internal email that needs the QS to price something and the site manager to
// book access is therefore two independent to-dos raised in one action, either completable without
// closing the other. Empty or null = a single unassigned item. Duplicates are collapsed.
public sealed record TodoItemDraft(
    string Title,
    string? Notes = null,
    IReadOnlyList<Role>? AssigneeRoles = null,
    DateTimeOffset? DueAt = null);

// One item the fan-out will create: the row it came from and the single role it lands on.
public sealed record TodoItemFanOut(TodoItemDraft Draft, Role? AssigneeRole);

// The fan-out rule itself, deliberately shared by the triage form — which promises a count on its
// "Create N to-do items from email" button — and the server, which actually creates the rows. Both
// call this, so the promise and the outcome cannot drift apart.
//
// Order is stable (drafts as given, roles as picked) because the items are numbered TODO-#### in
// exactly this sequence, and a triager reading the new references back should find them in the order
// they filled the form in.
public static class TodoItemDrafts
{
    public static IReadOnlyList<TodoItemFanOut> FanOutByRole(IEnumerable<TodoItemDraft> drafts) =>
        drafts.SelectMany(draft =>
        {
            // Distinct: the picker already stops a role being added twice, but a hand-rolled request
            // must not be able to raise the same item twice either.
            var roles = (draft.AssigneeRoles ?? Array.Empty<Role>()).Distinct().ToList();
            return roles.Count == 0
                ? new[] { new TodoItemFanOut(draft, null) }
                : roles.Select(role => new TodoItemFanOut(draft, role)).ToArray();
        }).ToList();
}
