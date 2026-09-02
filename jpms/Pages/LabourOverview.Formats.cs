using static Jewel.JPMS.Features.Labour.LabourDisplay;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class LabourOverview
{
    // ---- Site colours: a muted categorical palette, stable per project within the session ----
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
