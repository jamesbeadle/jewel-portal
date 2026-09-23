namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>The workstation queue's narrowing: every NO from every assessment, open first.</summary>
public static class WorkstationActionFilters
{
    public const string Open = "open";
    public const string Closed = "closed";
    public const string Everything = "all";

    public static IReadOnlyList<TabItem> Chips(IReadOnlyList<WorkstationAction>? actions) => new[]
    {
        new TabItem(Open, "Open", Count: actions?.Count(action => action.State == WorkstationActionState.Open)),
        new TabItem(Closed, "Fixed or accepted"),
        new TabItem(Everything, "Everything")
    };

    public static IReadOnlyList<WorkstationAction> Apply(IEnumerable<WorkstationAction> actions, string filter) =>
        actions.Where(action => filter switch
        {
            Open => action.State == WorkstationActionState.Open,
            Closed => action.State != WorkstationActionState.Open,
            _ => true
        }).ToList();
}
