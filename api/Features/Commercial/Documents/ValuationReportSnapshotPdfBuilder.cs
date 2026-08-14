using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

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
/// <see cref="BuildDraftAsync"/> renders the same statement from the LIVE report instead: the
/// capture maths compute what a snapshot would freeze right now (nothing is saved), and the
/// renderer stamps it as a working copy — so the preview and any later snapshot always agree.
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
            detail,
            CostCentreNames: await CostCentreNamesAsync(cancellationToken)));

        var fileName = SanitiseFileName(
            $"{project.Reference} - Valuation report - {snapshot.Label} - {snapshot.TakenAt:yyyy-MM-dd}.pdf");

        return new ValuationReportSnapshotPdf(pdf, fileName, snapshot.ProjectId, project.Name, snapshot);
    }

    /// <summary>
    /// The live valuation report as a working-copy PDF: computes the snapshot a capture would
    /// freeze right now (same maths, same line order — via ComputeAsync, which never touches the
    /// change tracker, so nothing persists) and renders it with the draft stamps. Labelled with
    /// the latest claim's period name so the accountant knows which claim they are reading.
    /// </summary>
    public async Task<ValuationReportSnapshotPdf> BuildDraftAsync(string projectId, CancellationToken cancellationToken)
    {
        var project = await context.Projects.FindAsync(new object[] { projectId }, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        // "June 2026 — working copy" when the latest claim is named, "Claim 3 — working copy"
        // otherwise, "Working copy" when the project has never been valued.
        var claim = await context.ValuationClaims
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var claimName = claim is null
            ? null
            : string.IsNullOrWhiteSpace(claim.Name) ? $"Claim {claim.ClaimNumber}" : claim.Name;
        var label = claimName is null ? "Working copy" : $"{claimName} — working copy";

        var (snapshotEntity, lineEntities) = await ValuationReportSnapshotCapture.ComputeAsync(
            context, projectId, label, valuationInvoiceId: null, cancellationToken);

        var detail = new ValuationReportSnapshotDetail(
            snapshotEntity.ToModel(),
            lineEntities.Select(line => line.ToModel()).ToList());

        var pdf = ValuationReportSnapshotRenderer.Render(new ValuationReportSnapshotDocument(
            project.Reference,
            project.Name,
            project.ClientName,
            detail,
            IsDraft: true,
            CostCentreNames: await CostCentreNamesAsync(cancellationToken)));

        var fileName = SanitiseFileName(
            $"{project.Reference} - Valuation report - Working copy - {DateTimeOffset.UtcNow:yyyy-MM-dd}.pdf");

        return new ValuationReportSnapshotPdf(pdf, fileName, projectId, project.Name, detail.Snapshot);
    }

    // Cost code → master name, for the bill's area sub-headings when a line carries no
    // estimate section (the ValuationReportAreas rule the renderer applies). Grouped rather
    // than ToDictionary so a duplicated code can never turn a PDF download into a 500.
    private async Task<IReadOnlyDictionary<string, string>> CostCentreNamesAsync(CancellationToken cancellationToken)
    {
        var centres = await context.CostCenters.AsNoTracking()
            .Select(centre => new { centre.Code, centre.Name })
            .ToListAsync(cancellationToken);
        return centres
            .GroupBy(centre => centre.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);
    }

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
