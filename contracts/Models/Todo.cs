namespace Jewel.JPMS.Models;

// A project to-do item. Created either directly on the project's Overview tab or from an email at
// the triage stage (several at once). Each item owns a sequential "TODO-0001" reference which is
// also its mailbox tag stem, so an email tagged "JPMS/TODO-0001" is the item's linked mail — the
// same live-read link mechanism the Request / Bid Package families use.
public sealed record TodoItem(
    string TodoItemId,
    string ProjectId,
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
