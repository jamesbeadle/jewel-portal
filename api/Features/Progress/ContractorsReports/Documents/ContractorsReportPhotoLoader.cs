using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;

/// <summary>Fetches every photograph the document names from the store, keyed by photo id.
/// A photograph that cannot be opened, or is not a JPEG or PNG, is left out rather than
/// corrupting the file.</summary>
public sealed class ContractorsReportPhotoLoader
{
    private readonly JpmsContext context;
    private readonly IProgressPhotoStore photoStore;

    public ContractorsReportPhotoLoader(JpmsContext context, IProgressPhotoStore photoStore)
    {
        this.context = context;
        this.photoStore = photoStore;
    }

    public async Task<IReadOnlyDictionary<string, ContractorsReportImage>> LoadAsync(
        ContractorsReportDocument document, CancellationToken cancellationToken)
    {
        var photoIds = document.Progress.SelectMany(day => day.Photos).Select(photo => photo.ProgressPhotoId).Distinct().ToList();
        var rows = await context.ProgressPhotos.AsNoTracking()
            .Where(row => photoIds.Contains(row.ProgressPhotoId))
            .Select(row => new { row.ProgressPhotoId, row.BlobRef, row.ContentType })
            .ToListAsync(cancellationToken);

        var images = new Dictionary<string, ContractorsReportImage>();
        foreach (var row in rows)
        {
            var image = await OpenAsync(row.ProgressPhotoId, row.BlobRef, row.ContentType, cancellationToken);
            if (image is not null) images[row.ProgressPhotoId] = image;
        }
        return images;
    }

    private async Task<ContractorsReportImage?> OpenAsync(string photoId, string blobRef, string contentType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobRef)) return null;
        var blob = await photoStore.OpenAsync(blobRef, cancellationToken);
        if (blob is null) return null;

        await using var content = blob.Content;
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var size = ImagePixelSize.Of(bytes);
        if (size is null) return null;
        return new ContractorsReportImage(photoId, bytes, contentType, size.Value.Width, size.Value.Height);
    }
}
