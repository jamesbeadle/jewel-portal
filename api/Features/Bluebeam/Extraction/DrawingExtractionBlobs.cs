using Jewel.JPMS.Api.Features.Drawings.Storage;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// Reads an extraction's JSON payload blobs back. A blob that has gone missing, or one that no
/// longer parses, degrades to null rather than failing the caller — the row's summary and status
/// still stand, and the caller says what it could not read. Shared by the api's data view and the
/// worker's rows-only rebuild.
/// </summary>
public static class DrawingExtractionBlobs
{
    public static async Task<T?> ReadJsonAsync<T>(
        IDrawingBlobStore drawingBlobs, string? blobRef, CancellationToken cancellationToken) where T : class
    {
        if (string.IsNullOrWhiteSpace(blobRef)) return null;
        var blob = await drawingBlobs.OpenAsync(blobRef, cancellationToken);
        if (blob is null) return null;

        await using var content = blob.Content;
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(content, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
