namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>The scan's page: the project, the poster's label, when it was composed, and the rows.</summary>
public sealed record SiteDrawingsPage(
    string ProjectName,
    string LinkLabel,
    string ShownAtText,
    IReadOnlyList<SiteDrawingRow> Rows);
