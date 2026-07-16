namespace Jewel.JPMS.Models;

// A to-do item. Created directly on a project's To-do tab, from an email at the triage stage
// (several at once), or as a GENERAL company-wide item that belongs to no project (ProjectId is ""
// then) — captured from a company-wide email at triage or added on the To-dos browser page. Each
// item owns a sequential "TODO-0001" reference which is also its mailbox tag stem, so an email
// tagged "JPMS/TODO-0001" is the item's linked mail — the same live-read link mechanism the
// Request / Bid Package families use.
public sealed record TodoItem(
    string TodoItemId,
    string ProjectId,        // "" = general (company-wide) item with no project
    string Reference,        // sequential human reference, e.g. "TODO-0001" (also the tag stem)
    string Title,
    string Notes,
    string AssigneeEmail,
    string CreatedByEmail,
    bool IsComplete,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt);

// One row of the triage "create to-dos from this email" form. The command carries a list of these
// so several items can be captured from a single email in one action.
public sealed record TodoItemDraft(
    string Title,
    string? Notes = null,
    string? AssigneeEmail = null,
    DateTimeOffset? DueAt = null);
