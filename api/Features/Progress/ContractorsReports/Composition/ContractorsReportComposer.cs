using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>
/// Composes a report's document from the record and the register — the one read the page's
/// preview, the Word build and the PDF build all go through, so the three never disagree.
/// </summary>
public sealed class ContractorsReportComposer
{
    private readonly JpmsContext context;
    public ContractorsReportComposer(JpmsContext context) { this.context = context; }

    public async Task<ContractorsReportView?> ViewAsync(string contractorsReportId, CancellationToken cancellationToken)
    {
        var entity = await context.ContractorsReports.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ContractorsReportId == contractorsReportId, cancellationToken);
        if (entity is null) return null;

        var report = entity.ToModel();
        var week = new ReportingWeek(report.PeriodStart, report.PeriodEnd);
        var updates = await ContractorsReportProgressReader.UpdatesInPeriodAsync(context, report.ProjectId, week, cancellationToken);
        var document = await ComposeAsync(report, week, updates, cancellationToken);
        return new ContractorsReportView(report, document, Choices(report, updates));
    }

    private async Task<ContractorsReportDocument> ComposeAsync(
        ContractorsReport report, ReportingWeek week, IReadOnlyList<ContractorsReportUpdate> updates, CancellationToken cancellationToken)
    {
        var project = await context.Projects.AsNoTracking()
            .Where(row => row.ProjectId == report.ProjectId)
            .Select(row => new { row.Name, row.Reference })
            .FirstAsync(cancellationToken);
        var selected = updates.Where(update => report.SelectedUpdateIds.Contains(update.ProgressUpdateId)).ToList();
        var variations = await ContractorsReportRegisterReader.VariationsAsync(context, report.ProjectId, cancellationToken);

        var draft = new ContractorsReportDocument(
            Header: new ContractorsReportHeader(
                project.Name, project.Reference, report.DisplayTitle, report.ValuationNumber,
                await ContractorsReportCertificates.LastNumberAsync(context, report.ProjectId, cancellationToken),
                report.ProgrammeReference, report.PeriodStart, report.PeriodEnd,
                report.PreparedByName, report.IssuedTo, report.DateOfIssue),
            Progress: ContractorsReportDays.Group(week, selected),
            LookAhead: report.LookAhead,
            Decisions: await ContractorsReportRegisterReader.DecisionsAsync(context, report.ProjectId, cancellationToken),
            Variations: variations,
            VariationsTotal: variations.Sum(variation => variation.Value),
            Neighbours: report.Neighbours,
            HealthAndSafety: report.HealthAndSafety,
            BuildingControl: await ContractorsReportRegisterReader.BuildingControlAsync(context, report.ProjectId, report.BuildingControlLiaison, cancellationToken),
            Subcontractors: await ContractorsReportSubcontractorsReader.ReadAsync(context, report.ProjectId, week, report.Attendance, cancellationToken),
            Findings: Array.Empty<ContractorsReportFinding>());
        return draft with { Findings = ContractorsReportWording.Check(ContractorsReportLines.Of(draft)) };
    }

    private static IReadOnlyList<ContractorsReportUpdateChoice> Choices(ContractorsReport report, IReadOnlyList<ContractorsReportUpdate> updates) =>
        updates
            .Select(update => new ContractorsReportUpdateChoice(
                update.ProgressUpdateId, update.WorkDate, update.Title, update.Photos.Count,
                report.SelectedUpdateIds.Contains(update.ProgressUpdateId)))
            .ToList();
}
