namespace Jewel.JPMS.Features.Todos;

// The move picker's value for the company-wide (general, no-project) destination — a real pick,
// distinct from the picker's own blank "nothing chosen yet" row. Callers map it to the blank
// ProjectId the MoveTodoItem command stores. Lives here (not on any one view) because every
// surface that builds move options — the item's page, the To-dos browser, the project tab —
// encodes the destination the same way.
public static class TodoScope
{
    public const string General = "__general__";
}
