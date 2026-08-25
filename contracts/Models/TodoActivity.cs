namespace Jewel.JPMS.Models;

// What happened to a to-do item, in order — the timeline on the item's own page. Every change the
// commands make writes one line here (created, started, chased, reassigned, moved, due date,
// completed, reopened) and so does every email sent from the item's page, so an assignee can show
// "I emailed Justine on the 24th" without the item being done. Stored as its own rows because the
// item only ever carries its LATEST facts; the story of how it got there lives here.
public enum TodoActivityKind
{
    Created = 0,
    Started = 1,      // "Working on it" — the item moved from Open to In progress
    Chased = 2,       // the assignee logged a chase (email, call, visit) with a note
    Note = 3,         // a plain progress note, no chase implied
    Reassigned = 4,
    Moved = 5,
    DueChanged = 6,
    Completed = 7,
    Reopened = 8,
    EmailSent = 9,    // an email sent from the item's page (files itself under the item's tag)
    Edited = 10,      // title or detail changed
}

// One timeline line. Summary is the sentence shown on the page ("Emailed justine@plg.uk — Re:
// Coombe Lane"); ActorEmail is who did it, stamped server-side from the signed-in user.
public sealed record TodoActivity(
    string TodoActivityId,
    string TodoItemId,
    TodoActivityKind Kind,
    string Summary,
    string ActorEmail,
    DateTimeOffset OccurredAt);

// The three activity kinds a person may log by hand from the item's page; every other kind is
// written by the command that caused it. Started and Chased both move an Open item to In progress
// (chasing IS working on it); a Note records progress without changing the state.
public static class TodoProgressKinds
{
    public static readonly IReadOnlyList<TodoActivityKind> LoggableByHand =
        new[] { TodoActivityKind.Started, TodoActivityKind.Chased, TodoActivityKind.Note };

    public static bool StartsTheItem(TodoActivityKind kind) =>
        kind is TodoActivityKind.Started or TodoActivityKind.Chased or TodoActivityKind.EmailSent;
}
