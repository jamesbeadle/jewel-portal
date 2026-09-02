using Jewel.JPMS.Contracts.Commercial.Export;
using Jewel.JPMS.Contracts.Documents.Excel;
using Jewel.JPMS.Contracts.Variations;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>The rendered spreadsheet plus the name it travels under.</summary>
public sealed record ValuationReportWorkbook(byte[] Content, string FileName);

/// <summary>
/// The valuation report as the spreadsheet the portal's Export button produces — built
/// server-side (2026-09-02, the accountant's ask: pull the portal's own file through the
/// connector rather than rebuild it). The workbook shape, cell styles and line mapping are the
/// shared ones the browser uses (<see cref="ValuationReportExportWorkbook"/>,
/// <see cref="ValuationSnapshotExport"/>), so the file is the same file whichever way it is
/// fetched. Renders a statement <see cref="ValuationReportSnapshotPdfBuilder"/> loaded, so a
/// PDF and a workbook exported together read the same figures.
/// </summary>
public sealed class ValuationReportWorkbookBuilder
{
    private readonly IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>> variations;

    public ValuationReportWorkbookBuilder(
        IQueryHandler<ListVariationOrdersForProject, IReadOnlyList<VariationOrder>> variations)
    {
        this.variations = variations;
    }

    public async Task<ValuationReportWorkbook> BuildAsync(ValuationReportStatement statement, CancellationToken cancellationToken)
    {
        // The Pending variations tab reads the LIVE register at export time (a snapshot freezes
        // the report, not the register). If the register cannot be read the export still runs and
        // the tab says so outright — the same courtesy the page's export gives.
        IReadOnlyList<VariationOrder>? orders;
        try
        {
            orders = await variations.HandleAsync(new ListVariationOrdersForProject(statement.ProjectId), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            orders = null;
        }

        var detail = statement.Detail;
        var workbook = ValuationReportExportWorkbook.Build(
            ValuationSnapshotExport.Meta(detail.Snapshot, statement.IsDraft),
            ValuationSnapshotExport.Lines(detail.Lines,
                code => statement.CostCentreNames.TryGetValue(code, out var name) ? name : null),
            ValuationSnapshotExport.Summary(detail.Snapshot, detail.Lines),
            orders is null ? null : ValuationExportPendingVariations.From(orders));

        return new ValuationReportWorkbook(
            ExcelWorkbookWriter.Write(workbook),
            ValuationReportSnapshotPdfBuilder.SanitiseFileName($"{statement.FileNameStem}.xlsx"));
    }
}
