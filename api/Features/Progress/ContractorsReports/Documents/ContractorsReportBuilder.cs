using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>A built report file, or the findings that refused it.</summary>
public sealed record ContractorsReportFile(byte[] Content, string ContentType, string FileName);

public sealed record ContractorsReportBuildOutcome(ContractorsReportFile? File, IReadOnlyList<ContractorsReportFinding> Findings)
{
    public bool IsRefused => File is null;
}

public enum ContractorsReportFormat { Pdf, Word }

/// <summary>Composes, gates, loads the photographs and renders — the one path both downloads
/// take, so a report that the page shows as refused is refused here too.</summary>
public sealed class ContractorsReportBuilder
{
    private readonly ContractorsReportComposer composer;
    private readonly ContractorsReportPhotoLoader photos;

    public ContractorsReportBuilder(ContractorsReportComposer composer, ContractorsReportPhotoLoader photos)
    {
        this.composer = composer;
        this.photos = photos;
    }

    public async Task<ContractorsReportBuildOutcome?> BuildAsync(string contractorsReportId, ContractorsReportFormat format, CancellationToken cancellationToken)
    {
        var view = await composer.ViewAsync(contractorsReportId, cancellationToken);
        if (view is null) return null;
        var document = view.Document;
        if (!document.CanBeBuilt) return new ContractorsReportBuildOutcome(null, document.Findings);

        var images = await photos.LoadAsync(document, cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;
        var file = format == ContractorsReportFormat.Pdf
            ? new ContractorsReportFile(ContractorsReportPdfRenderer.Render(document, images, generatedAt), ContractorsReportFileNames.PdfContentType, ContractorsReportFileNames.Pdf(document.Header))
            : new ContractorsReportFile(ContractorsReportWordRenderer.Render(document, images, generatedAt), ContractorsReportFileNames.WordContentType, ContractorsReportFileNames.Word(document.Header));
        return new ContractorsReportBuildOutcome(file, document.Findings);
    }
}
