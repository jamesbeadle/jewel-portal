namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>The nine section titles in PLG's order — the one place they are spelt, shared by the
/// composer's findings, the page's preview and both renderers.</summary>
public static class ContractorsReportSections
{
    public const string Progress = "1. Progress Against Programme";
    public const string LookAhead = "2. Look Ahead";
    public const string Decisions = "3. Decisions / Instructions Needed";
    public const string Variations = "4. Variations";
    public const string Neighbours = "5. Neighbours";
    public const string HealthAndSafety = "6. Health & Safety";
    public const string BuildingControl = "7. Building Control";
    public const string Subcontractors = "8. Specialist Subcontractors";
    public const string Photographs = "9. Site Photographs";

    public static readonly IReadOnlyList<string> InOrder = new[]
    {
        Progress, LookAhead, Decisions, Variations, Neighbours, HealthAndSafety, BuildingControl, Subcontractors, Photographs
    };
}
