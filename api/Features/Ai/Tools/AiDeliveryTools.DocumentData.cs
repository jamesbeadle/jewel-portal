using Jewel.JPMS.Api.Features.Bluebeam.Extraction;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private const int DocumentDataDefaultLimit = 50;
    private const int DocumentDataMaxLimit = 200;

    /// <summary>The transcription rows, filtered in SQL — the way to work over a drawing without
    /// reading its whole extraction (a real A0 sheet's is ~67k characters).</summary>
    private static AiTool QueryDocumentData()
    {
        return new(
            "query_document_data",
            "Queries what the portal transcribed from a drawing's PDF as ROWS — figured dimensions, "
            + "notes/callouts, closed shapes — filtered and totalled server-side, so a question about "
            + "a sheet costs a page of rows, not the whole extraction. Scope: revisionId (one "
            + "revision), drawingId (the document's newest extracted revision) or projectId (the "
            + "newest extracted revision of EVERY document on the project — ask across the set). "
            + "kind is dimensions | callouts | shapes. Filters combine: page; contains (callouts "
            + "whose text includes the words, case-insensitive); axis H/V/D and minValueMm/"
            + "maxValueMm (dimensions); minAreaSqM/maxAreaSqM and rectanglesOnly (shapes); nearX/"
            + "nearY/withinMm (anything within that many mm of a point — how to find the shapes and "
            + "dimensions belonging to a callout: take its at, ask within 1500). limit (default 50, "
            + "max 200) and offset page the rows; totals cover EVERY matching row (count, and the "
            + "summed mm / m² / perimeter), so a take-off sums a sheet in one call. Positions are "
            + "[x, y] in real-world mm from the sheet's bottom-left corner at the page's proven "
            + "scale — check scaleVerified on get_document_extraction first; use a dimension's "
            + "valueMm, never the distance between its ends. Rows are labelled by revisionId; the "
            + "reply's sheets[] maps each to its document code, title and revision.",
            AiToolSchema.Object(
                ("kind", "string", "dimensions, callouts or shapes.", true),
                ("revisionId", "string", "One revision (list_documents with drawingId gives revision ids).", false),
                ("drawingId", "string", "Instead of revisionId: the document's newest extracted revision.", false),
                ("projectId", "string", "Instead of either: every document on the project, newest extracted revision each.", false),
                ("page", "integer", "Only this page of the sheet.", false),
                ("contains", "string", "Callouts only: text the note must include.", false),
                ("axis", "string", "Dimensions only: H, V or D.", false),
                ("minValueMm", "integer", "Dimensions only: smallest figure to include.", false),
                ("maxValueMm", "integer", "Dimensions only: largest figure to include.", false),
                ("minAreaSqM", "number", "Shapes only: smallest area (m²) to include.", false),
                ("maxAreaSqM", "number", "Shapes only: largest area (m²) to include.", false),
                ("rectanglesOnly", "boolean", "Shapes only: rectangles alone.", false),
                ("nearX", "number", "With nearY and withinMm: only rows within that distance of this point (mm).", false),
                ("nearY", "number", "See nearX.", false),
                ("withinMm", "number", "See nearX.", false),
                ("limit", "integer", "Rows to return (default 50, max 200).", false),
                ("offset", "integer", "Rows to skip, for the next page.", false)),
            AiToolKind.Read,
            DrawingReaders,
            QueryDocumentDataAsync);
    }

    private static async Task<string> QueryDocumentDataAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var kind = (AiToolSchema.Text(input, "kind") ?? "").Trim().ToLowerInvariant();
        if (kind is not ("dimensions" or "callouts" or "shapes"))
            return Fail("kind must be dimensions, callouts or shapes.");
        var scope = await ResolveDocumentDataScopeAsync(context, input, ct);
        if (scope.Problem is not null) return scope.Problem;

        var filter = DocumentDataFilter(input, scope.Sheets.Where(sheet => sheet.HasRows).Select(sheet => sheet.DrawingRevisionId).ToList());
        object page = kind switch
        {
            "dimensions" => await DrawingDataQuery.DimensionsAsync(context.Db, filter, ct),
            "callouts" => await DrawingDataQuery.CalloutsAsync(context.Db, filter, ct),
            _ => await DrawingDataQuery.ShapesAsync(context.Db, filter, ct)
        };
        return Serialise(new
        {
            ok = true,
            kind,
            units = "Positions [x, y] and lengths in mm from the sheet's bottom-left corner; areas in m². Totals cover every matching row, rows are one page of them.",
            sheets = scope.Sheets,
            notTranscribed = scope.Sheets.Any(sheet => !sheet.HasRows)
                ? "Some sheets were extracted before the row tables existed and are not in these rows — rebuild_document_data transcribes them from what was already read."
                : null,
            page = new { filter.Offset, filter.Limit },
            data = page
        });
    }

    private static DrawingDataFilter DocumentDataFilter(JsonElement input, IReadOnlyList<string> revisionIds)
    {
        var nearX = AiToolSchema.Decimal(input, "nearX");
        var nearY = AiToolSchema.Decimal(input, "nearY");
        var within = AiToolSchema.Decimal(input, "withinMm");
        var near = nearX is { } x && nearY is { } y && within is { } radius ? new DrawingDataNear(x, y, radius) : null;
        return new DrawingDataFilter(
            revisionIds,
            Page: AiToolSchema.Number(input, "page"),
            Contains: AiToolSchema.Text(input, "contains"),
            Axis: AiToolSchema.Text(input, "axis"),
            MinValueMm: AiToolSchema.Number(input, "minValueMm"),
            MaxValueMm: AiToolSchema.Number(input, "maxValueMm"),
            MinAreaSqM: AiToolSchema.Decimal(input, "minAreaSqM"),
            MaxAreaSqM: AiToolSchema.Decimal(input, "maxAreaSqM"),
            RectanglesOnly: AiToolSchema.Flag(input, "rectanglesOnly") ?? false,
            Near: near,
            Offset: Math.Max(0, AiToolSchema.Number(input, "offset") ?? 0),
            Limit: Math.Clamp(AiToolSchema.Number(input, "limit") ?? DocumentDataDefaultLimit, 1, DocumentDataMaxLimit));
    }
}
