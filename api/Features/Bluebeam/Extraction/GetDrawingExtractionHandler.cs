using System.Text.Json;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

// A revision's data view. Markups come from SQL; the text pages come back off the blob (they can
// run long, and the register never queries into them). A text blob that has gone missing degrades
// to an empty list rather than failing the whole view — the markups and status still render.
public sealed class GetDrawingExtractionHandler : IQueryHandler<GetDrawingExtraction, DrawingExtractionView?>
{
    private readonly JpmsContext context;
    private readonly IDrawingBlobStore drawingBlobs;

    public GetDrawingExtractionHandler(JpmsContext context, IDrawingBlobStore drawingBlobs)
    {
        this.context = context; this.drawingBlobs = drawingBlobs;
    }

    public async Task<DrawingExtractionView?> HandleAsync(GetDrawingExtraction query, CancellationToken cancellationToken)
    {
        var extraction = await context.DrawingExtractions
            .FirstOrDefaultAsync(row => row.DrawingRevisionId == query.DrawingRevisionId, cancellationToken);
        if (extraction is null) return null;

        var markups = await context.DrawingMarkups
            .Where(row => row.DrawingExtractionId == extraction.DrawingExtractionId)
            .OrderBy(row => row.PageNumber)
            .ThenBy(row => row.MarkupType)
            .ToListAsync(cancellationToken);

        var textPages = await ReadTextPagesAsync(extraction.TextBlobRef, cancellationToken);
        return new DrawingExtractionView(
            extraction.ToModel(),
            markups.Select(markup => markup.ToModel()).ToList(),
            textPages);
    }

    private async Task<IReadOnlyList<DrawingTextPage>> ReadTextPagesAsync(
        string? textBlobRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(textBlobRef)) return Array.Empty<DrawingTextPage>();
        var blob = await drawingBlobs.OpenAsync(textBlobRef, cancellationToken);
        if (blob is null) return Array.Empty<DrawingTextPage>();

        await using var content = blob.Content;
        try
        {
            return await JsonSerializer.DeserializeAsync<List<DrawingTextPage>>(
                content, cancellationToken: cancellationToken) ?? new List<DrawingTextPage>();
        }
        catch (JsonException)
        {
            return Array.Empty<DrawingTextPage>();
        }
    }
}
