using Jewel.JPMS.Api.Features.Bluebeam.Extraction;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private sealed record DocumentDataScope(IReadOnlyList<DrawingDataSheet> Sheets, string? Problem);

    private const string NothingExtracted =
        "Nothing has been extracted here yet — extract_document_data queues a revision (or Extract data on the document page); every revision that lands is queued automatically.";

    private const string NothingTranscribed =
        "These revisions were extracted before the row tables existed, so there are no rows to query yet — rebuild_document_data transcribes them from what was already read (no PDF is re-read), or get_document_extraction still returns the summary.";

    // Which revisions the query reads, or the reply that says why none can be — one of
    // revisionId / drawingId / projectId (the project in scope counts), succeeded extractions
    // only, and at least one of them transcribed into rows.
    private static async Task<DocumentDataScope> ResolveDocumentDataScopeAsync(
        AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var revisionId = AiToolSchema.Text(input, "revisionId")?.Trim();
        var drawingId = AiToolSchema.Text(input, "drawingId")?.Trim();
        var projectId = ProjectId(context, input)?.Trim();
        if (string.IsNullOrWhiteSpace(revisionId) && string.IsNullOrWhiteSpace(drawingId) && string.IsNullOrWhiteSpace(projectId))
            return new DocumentDataScope(Array.Empty<DrawingDataSheet>(), Fail("Pass revisionId, drawingId or projectId."));

        var sheets = await DrawingDataScope.ResolveAsync(context.Db, revisionId, drawingId, projectId, ct);
        if (sheets.Count == 0)
            return new DocumentDataScope(sheets, Serialise(new { ok = true, status = "NotExtracted", note = NothingExtracted }));
        if (sheets.All(sheet => !sheet.HasRows))
            return new DocumentDataScope(sheets, Serialise(new { ok = true, status = "NotTranscribed", sheets, note = NothingTranscribed }));
        return new DocumentDataScope(sheets, null);
    }
}
