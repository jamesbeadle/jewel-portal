using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>A built report file, or the findings that refused it.</summary>
public sealed record ContractorsReportFile(byte[] Content, string ContentType, string FileName);

public sealed record ContractorsReportBuildOutcome(ContractorsReportFile? File, IReadOnlyList<ContractorsReportFinding> Findings)
{
    public bool IsRefused => File is null;
}

/// <summary>Composes, gates, loads the photographs and renders the PDF — the one path the page's
/// download and the connector's export take, so a report the page shows as refused is refused
/// here too. The PDF is the issued document; there is no Word copy (Nigel, 24 Sep 2026).</summary>
public sealed class ContractorsReportBuilder
{
    private readonly ContractorsReportComposer composer;
    private readonly ContractorsReportPhotoLoader photos;

    public ContractorsReportBuilder(ContractorsReportComposer composer, ContractorsReportPhotoLoader photos)
    {
        this.composer = composer;
        this.photos = photos;
    }

    public async Task<ContractorsReportBuildOutcome?> BuildAsync(string contractorsReportId, CancellationToken cancellationToken)
    {
        var view = await composer.ViewAsync(contractorsReportId, cancellationToken);
        if (view is null) return null;
        var document = view.Document;
        if (!document.CanBeBuilt) return new ContractorsReportBuildOutcome(null, document.Findings);

        var images = await photos.LoadAsync(document, cancellationToken);
        var pdf = ContractorsReportPdfRenderer.Render(document, images);
        var file = new ContractorsReportFile(pdf, ContractorsReportFileNames.PdfContentType, ContractorsReportFileNames.Pdf(document.Header));
        return new ContractorsReportBuildOutcome(file, document.Findings);
    }
}
