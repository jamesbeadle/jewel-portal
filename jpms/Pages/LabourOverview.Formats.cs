using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    // ---- Formatting & colours -----------------------------------------------------------------

    private static string Pounds(decimal value) => value.ToString("£#,##0");
    private static string Days(decimal value) => value == 0m ? "0" : value.ToString("0.##");

    private static string ConfidenceLabel(LabourOverviewTotals totals) =>
        totals.ElapsedWorkerDays == 0 ? "0%"
            : $"{(double)totals.ConfirmedWorkerDays / totals.ElapsedWorkerDays:P0}";

    private static string BarWidth(decimal value, decimal max) =>
        max <= 0m ? "0%" : $"{(double)(value / max) * 100:0}%";

    private static string StatusLabel(TimesheetStatus status) => status switch
    {
        TimesheetStatus.Approved => "approved",
        TimesheetStatus.Rejected => "sent back",
        _ => "waiting",
    };

    private static string AbsenceLabel(AbsenceKind kind) => kind switch
    {
        AbsenceKind.Holiday => "Holiday",
        AbsenceKind.HalfDay => "Half day",
        AbsenceKind.NotWorked => "Not worked",
        _ => "Sick",
    };

    // A muted categorical palette for site chips; stable per project within the session.
    private static readonly string[] SiteColours =
    {
        "#5B8DEF", "#57C4AD", "#C4884A", "#9C7BD8", "#D96C8A", "#6BA85E", "#4FA8C7", "#B0A24E",
        "#D0805B", "#7E93A8",
    };
    private readonly Dictionary<string, string> colourByProject = new();

    private string ColourOf(string projectId)
    {
        if (string.IsNullOrEmpty(projectId)) return "#34373B";
        if (!colourByProject.TryGetValue(projectId, out var colour))
        {
            colour = SiteColours[colourByProject.Count % SiteColours.Length];
            colourByProject[projectId] = colour;
        }
        return colour;
    }

}
