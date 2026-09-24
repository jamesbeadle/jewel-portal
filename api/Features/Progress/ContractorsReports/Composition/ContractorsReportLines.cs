using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>Every line the document will print, named by its section — what the wording gate reads.</summary>
internal static class ContractorsReportLines
{
    public static IEnumerable<(string Section, string Line)> Of(ContractorsReportDocument document)
    {
        foreach (var entry in document.Progress.SelectMany(day => day.Entries))
            yield return (ContractorsReportSections.Progress, entry.Description);
        foreach (var instruction in document.InstructionsReceived)
            yield return (ContractorsReportSections.Progress, instruction);
        foreach (var item in document.LookAhead)
            yield return (ContractorsReportSections.LookAhead, item.Text);
        foreach (var decision in document.Decisions)
            yield return (ContractorsReportSections.Decisions, decision.Title);
        foreach (var variation in document.Variations)
            yield return (ContractorsReportSections.Variations, variation.Title);
        yield return (ContractorsReportSections.Neighbours, document.Neighbours);
        yield return (ContractorsReportSections.HealthAndSafety, document.HealthAndSafety);
        yield return (ContractorsReportSections.BuildingControl, document.BuildingControl.Liaison);
        foreach (var subcontractor in document.Subcontractors)
            yield return (ContractorsReportSections.Subcontractors, subcontractor.Scope);
    }
}
