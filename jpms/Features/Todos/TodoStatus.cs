
namespace Jewel.JPMS.Features.Todos;

// The three states a to-do reads as — Open, In progress, Done — and the one pill every surface
// wears for them (the item page header, the board cards, the list rows). In progress is derived
// (started and not complete), so nothing here needs a fourth value.
public static class TodoStatus
{
    public static string Label(TodoItem item) =>
        item.IsComplete ? "Done" : item.IsInProgress ? "In progress" : "Open";

    public static Jewel.JPMS.Components.Tone Tone(TodoItem item) =>
        item.IsComplete ? Jewel.JPMS.Components.Tone.Positive
        : item.IsInProgress ? Jewel.JPMS.Components.Tone.Warning
        : Jewel.JPMS.Components.Tone.Info;
}
