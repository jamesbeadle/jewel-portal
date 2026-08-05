namespace Jewel.JPMS.Features.Triage;

/// <summary>
/// One drafted to-do row in the triage to-dos modal. The ASSIGNEES are held as
/// TodoAssigneePicker values — a role, optionally pinned to a named holder. Empty = unassigned.
/// A row with several assignees is raised as one to-do PER ASSIGNEE — same title, detail and due
/// date, separate TODO-#### references and separate tick-boxes — so an email that needs two
/// people to act becomes two items in one apply.
/// </summary>
public sealed class TodoDraftRow
{
    public string Title { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<string> Assignees { get; } = new();
    public string Due { get; set; } = "";
}

/// <summary>
/// A NEW system record staged in the System Tags modal, created (and the email tagged to it) when
/// the page's Apply runs. Which fields matter depends on <see cref="Kind"/>: a Client-side General
/// request carries the request fields; a Subcontractor bid package carries Title + Trade.
/// </summary>
public sealed class StagedRecordCreate
{
    public StagedRecordKind Kind { get; set; } = StagedRecordKind.Request;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string RaisedTo { get; set; } = "";
    public string DrawingRef { get; set; } = "";
    public string ResponseDue { get; set; } = "";
    public bool AddToProgramme { get; set; }
    public string Trade { get; set; } = "";

    public string Label => Kind == StagedRecordKind.BidPackage ? "new bid package" : "new request";
}

public enum StagedRecordKind { Request, BidPackage }
