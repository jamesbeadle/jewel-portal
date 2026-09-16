using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private const int MarkupRows = 200;

    /// <summary>The SUMMARY of a drawing revision's read — title block, revision table, proven
    /// scale, counts, warnings, Revu markups if any. The dimensions, callouts and shapes
    /// themselves are rows, read through query_document_data (2026-09-16): this used to return
    /// them all and a real A0 sheet's reply was ~67k characters.</summary>
    private static AiTool GetDocumentExtraction()
    {
        return new(
            "get_document_extraction",
            "The summary of what the portal read from a drawing revision's PDF: its title block "
            + "(drawing number, title, revision, scale, date, drawn by, job, client), the revision "
            + "table, the scale each page PROVED (figured dimensions matched to drawn lines — trust "
            + "scaleVerified before measuring anything), how many dimensions, notes and shapes were "
            + "read, the warnings the reader raised, and any Revu markups if Bluebeam read some. "
            + "The dimensions, notes and shapes are NOT in this reply — they are rows, filtered and "
            + "totalled by query_document_data, so read this first for the sheet and its scale, "
            + "then query the rows you need. Pass revisionId, or drawingId for that document's "
            + "newest extracted revision. status tells you whether the read has run; every revision "
            + "that lands is queued automatically, and extract_document_data queues one by hand.",
            AiToolSchema.Object(
                ("revisionId", "string", "The revision to read (list_documents with drawingId gives revision ids).", false),
                ("drawingId", "string", "Instead of revisionId: the document whose newest extracted revision to read.", false)),
            AiToolKind.Read,
            DrawingReaders,
            GetDocumentExtractionAsync);
    }

    private static async Task<string> GetDocumentExtractionAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var revisionId = AiToolSchema.Text(input, "revisionId")?.Trim();
        var drawingId = AiToolSchema.Text(input, "drawingId")?.Trim();
        if (string.IsNullOrWhiteSpace(revisionId) && string.IsNullOrWhiteSpace(drawingId))
            return Fail("Pass revisionId or drawingId.");

        var view = string.IsNullOrWhiteSpace(revisionId)
            ? await NewestExtractedViewAsync(context, drawingId!, ct)
            : await Query<GetDrawingExtraction, DrawingExtractionView?>(context, new GetDrawingExtraction(revisionId), ct);
        if (view is null)
            return Serialise(new { ok = true, revisionId, drawingId, status = "NotExtracted", note = NothingExtracted });

        var extraction = view.Extraction;
        if (extraction.Status != DrawingExtractionStatus.Succeeded)
            return Serialise(new { ok = true, extraction.DrawingRevisionId, status = extraction.Status.ToString(), extraction.QueuedBy, extraction.QueuedAt, extraction.ErrorMessage });

        var structure = view.Structure;
        return Serialise(new
        {
            ok = true,
            extraction.DrawingRevisionId,
            extraction.DrawingId,
            status = "Succeeded",
            extraction.CompletedAt,
            extraction.PageCount,
            extraction.RowsWrittenAt,
            summary = new
            {
                extraction.DrawingNumber, extraction.RevisionLabel, extraction.Scale, extraction.ScaleVerified,
                extraction.DimensionCount, extraction.CalloutCount, extraction.ShapeCount,
                extraction.MarkupCount, extraction.MarkupsNote
            },
            titleBlock = structure?.TitleBlock,
            revisions = structure?.Revisions,
            scales = structure?.Scales,
            warnings = structure?.Warnings,
            markups = view.Markups.Take(MarkupRows),
            rows = extraction.RowsWrittenAt is null
                ? "Not transcribed into rows yet — rebuild_document_data does it from this read, no PDF re-read."
                : "query_document_data reads the dimensions, callouts and shapes (kind, page, contains, near…).",
            structureNote = structure is null
                ? "This revision was extracted before the portal read drawings itself — only the text layer is held. Extract data again on the document page to get the structured read."
                : null
        });
    }

    // The document's newest successfully extracted revision, else its newest extraction row of
    // any status (so a Queued / Failed one still reports), else null.
    private static async Task<DrawingExtractionView?> NewestExtractedViewAsync(AiToolContext context, string drawingId, CancellationToken ct)
    {
        var revisions = await Query<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>(
            context, new ListRevisionsForDrawing(drawingId), ct);
        DrawingExtractionView? fallback = null;
        foreach (var revision in revisions.OrderByDescending(row => row.ReceivedAt).Take(8))
        {
            var candidate = await Query<GetDrawingExtraction, DrawingExtractionView?>(
                context, new GetDrawingExtraction(revision.DrawingRevisionId), ct);
            if (candidate?.Extraction.Status == DrawingExtractionStatus.Succeeded) return candidate;
            fallback ??= candidate;
        }
        return fallback;
    }
}
