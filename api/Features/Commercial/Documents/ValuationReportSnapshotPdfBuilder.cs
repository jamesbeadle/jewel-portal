using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// The rendered snapshot PDF plus the identity the callers need around it: the filename it should
/// travel under and the project the snapshot belongs to (for authorisation and addressing).
/// </summary>
public sealed record ValuationReportSnapshotPdf(
    byte[] Content,
    string FileName,
    string ProjectId,
    string ProjectName,
    ValuationReportSnapshot Snapshot);

/// <summary>
/// Assembles and renders the snapshot PDF in one place — the download endpoint streams exactly the
/// bytes the email command attaches, so what's downloaded and what's sent never diverge. Loads the
/// frozen detail through the existing query handler and the project header for the document's
/// identity, then hands both to <see cref="ValuationReportSnapshotRenderer"/>.
/// </summary>
public sealed class ValuationReportSnapshotPdfBuilder
{
    private readonly IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail> snapshots;
    private readonly JpmsContext context;

    public ValuationReportSnapshotPdfBuilder(
        IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail> snapshots,
        JpmsContext context)
    {
        this.snapshots = snapshots; this.context = context;
    }

    public async Task<ValuationReportSnapshotPdf> BuildAsync(string snapshotId, CancellationToken cancellationToken)
    {
        var detail = await snapshots.HandleAsync(new GetValuationReportSnapshot(snapshotId), cancellationToken);
        var snapshot = detail.Snapshot;

        var project = await context.Projects.FindAsync(new object[] { snapshot.ProjectId }, cancellationToken)
            ?? throw new InvalidOperationException($"Project {snapshot.ProjectId} for snapshot {snapshotId} not found.");

        var pdf = ValuationReportSnapshotRenderer.Render(new ValuationReportSnapshotDocument(
            project.Reference,
            project.Name,
            project.ClientName,
            detail));

        var fileName = SanitiseFileName(
            $"{project.Reference} - Valuation report - {snapshot.Label} - {snapshot.TakenAt:yyyy-MM-dd}.pdf");

        return new ValuationReportSnapshotPdf(pdf, fileName, snapshot.ProjectId, project.Name, snapshot);
    }

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
