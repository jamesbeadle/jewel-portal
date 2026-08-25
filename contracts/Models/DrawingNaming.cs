namespace Jewel.JPMS.Models;

/// <summary>
/// How a drawing is named on screen now that code, title and revision are all optional. One
/// place for the fallbacks so the register, pickers, dialogs and filed-document labels agree.
/// </summary>
public static class DrawingNaming
{
    public const string Blank = "—";
    public const string UntitledDrawing = "Untitled drawing";
    public const string NoRevision = "No revision";
    public const string ApprovedWithoutRevision = "Approved";

    private const string CodeTitleSeparator = " — ";

    public static string Code(Drawing drawing) =>
        string.IsNullOrWhiteSpace(drawing.DrawingCode) ? Blank : drawing.DrawingCode;

    /// <summary>Title, else the original file name, else the code, else "Untitled drawing".</summary>
    public static string Name(Drawing drawing)
    {
        if (!string.IsNullOrWhiteSpace(drawing.Title)) return drawing.Title;
        if (!string.IsNullOrWhiteSpace(drawing.LatestFileName)) return drawing.LatestFileName;
        if (!string.IsNullOrWhiteSpace(drawing.DrawingCode)) return drawing.DrawingCode;
        return UntitledDrawing;
    }

    public static bool IsNamedByFile(Drawing drawing) =>
        string.IsNullOrWhiteSpace(drawing.Title) && !string.IsNullOrWhiteSpace(drawing.LatestFileName);

    /// <summary>"A-100 — Ground floor plan", or just the name when there is no code.</summary>
    public static string Label(Drawing drawing) =>
        string.IsNullOrWhiteSpace(drawing.DrawingCode) || Name(drawing) == drawing.DrawingCode
            ? Name(drawing)
            : drawing.DrawingCode + CodeTitleSeparator + Name(drawing);

    /// <summary>"Rev A", or "No revision" for a blank label.</summary>
    public static string RevisionText(string revisionLabel) =>
        string.IsNullOrWhiteSpace(revisionLabel) ? NoRevision : "Rev " + revisionLabel;

    /// <summary>"Rev A" / "Approved" (approved, no label) / null when nothing is approved.</summary>
    public static string? ApprovedRevisionText(Drawing drawing)
    {
        if (!drawing.HasApprovedRevision) return null;
        if (string.IsNullOrWhiteSpace(drawing.CurrentApprovedRevisionLabel)) return ApprovedWithoutRevision;
        return "Rev " + drawing.CurrentApprovedRevisionLabel;
    }
}
