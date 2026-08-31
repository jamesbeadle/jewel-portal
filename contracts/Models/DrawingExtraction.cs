namespace Jewel.JPMS.Models;

/// <summary>Where a revision's extraction is. Queued and Running are transient (the worker owns
/// the move); Succeeded and Failed are terminal until someone re-extracts.</summary>
public enum DrawingExtractionStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

/// <summary>
/// One drawing revision's extraction — pipeline status plus, once it succeeds, the shape of what
/// came out (page count, markup count). The payloads themselves ride on DrawingExtractionView.
/// </summary>
public sealed record DrawingExtraction(
    string DrawingExtractionId,
    string DrawingRevisionId,
    string DrawingId,
    string ProjectId,
    DrawingExtractionStatus Status,
    string QueuedBy,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int Attempts,
    string? ErrorMessage,
    int? PageCount,
    int? MarkupCount);

/// <summary>One markup from an extraction, as the data view renders it.</summary>
public sealed record DrawingMarkup(
    string DrawingMarkupId,
    string BluebeamMarkupId,
    int PageNumber,
    string MarkupType,
    string Subject,
    string Author,
    string Comment,
    string Colour,
    decimal? MeasurementValue,
    string? MeasurementUnit);

/// <summary>One page's embedded text layer (PdfPig's read of the PDF, not OCR — a scanned
/// drawing with no text layer legitimately comes back empty).</summary>
public sealed record DrawingTextPage(int Page, string Text);

/// <summary>The full data view for a revision: the extraction row, its markups, and the per-page
/// text. Null markups/text simply mean the run hasn't succeeded yet.</summary>
public sealed record DrawingExtractionView(
    DrawingExtraction Extraction,
    IReadOnlyList<DrawingMarkup> Markups,
    IReadOnlyList<DrawingTextPage> TextPages);
