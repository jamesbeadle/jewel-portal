
namespace Jewel.JPMS.Features.Todos;

// The three states a to-do reads as — Open, In progress, Done — and the one pill every surface
// wears for them (the item page header, the board cards, the list rows). In progress is derived
// (started and not complete), so nothing here needs a fourth value.
public static class TodoStatus
{
    public static string Label(TodoItem item) =>
        item.IsComplete ? "Done" : item.IsInProgress ? "In progress" : "Open";

    public static string PillClass(TodoItem item) =>
        item.IsComplete ? "bg-positive/10 text-positive"
        : item.IsInProgress ? "bg-amber-400/10 text-amber-400"
        : "bg-accent/10 text-accent";

    public const string PillBaseClass =
        "shrink-0 inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium";
}
