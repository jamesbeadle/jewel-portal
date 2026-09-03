using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool ListDrawings()
    {
        return new(
            "list_documents",
            "A project's Documents register (called Drawings until 2026-09-03 — it holds "
            + "drawings AND party-wall awards, building-control letters, reports, anything issued "
            + "for the project): the folder tree (folders nest via parent id) and every document "
            + "with its current-revision standing — the approved revision label when one is "
            + "approved (a revision can be approved with a BLANK label, so trust "
            + "hasApprovedRevision, not the label), else the newest revision by file name, plus "
            + "unapproved and archived counts. Pass drawingId (the id parameter keeps the "
            + "register's old name) instead for that one document's full revision history with "
            + "the approval trail (who approved what, when, and what was superseded). Rows come "
            + "back under the drawings key; the detail page is /projects/{projectId}/documents/{drawingId}.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false),
                ("drawingId", "string", "One document's full revision history instead of the register.", false)),
            AiToolKind.Read,
            DrawingReaders,
            ListDrawingsAsync);
    }

    private static async Task<string> ListDrawingsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var drawingId = AiToolSchema.Text(input, "drawingId")?.Trim();
        if (!string.IsNullOrWhiteSpace(drawingId)) return await RevisionHistoryAsync(context, drawingId, ct);

        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);

        var drawings = await Query<ListDrawingsForProject, IReadOnlyList<Drawing>>(
            context, new ListDrawingsForProject(projectId), ct);
        var folders = await Query<ListDrawingFoldersForProject, IReadOnlyList<DrawingFolder>>(
            context, new ListDrawingFoldersForProject(projectId), ct);
        return Serialise(new
        {
            ok = true,
            projectId,
            folders = folders.Select(FolderRow),
            count = drawings.Count,
            drawings = drawings.Select(DrawingRow)
        });
    }

    private static async Task<string> RevisionHistoryAsync(AiToolContext context, string drawingId, CancellationToken ct)
    {
        var revisions = await Query<ListRevisionsForDrawing, IReadOnlyList<DrawingRevision>>(
            context, new ListRevisionsForDrawing(drawingId), ct);
        return Serialise(new { ok = true, drawingId, count = revisions.Count, revisions = revisions.Select(RevisionRow) });
    }

    private static object RevisionRow(DrawingRevision revision) => new
    {
        revision.DrawingRevisionId,
        revision.RevisionLabel,
        revision.FileName,
        revision.IssuedByEmail,
        revision.ReceivedAt,
        revision.SupersededAt,
        approvalStatus = revision.ApprovalStatus.ToString(),
        revision.ApprovedByEmail,
        revision.ApprovedAt
    };

    private static object FolderRow(DrawingFolder folder) => new
    {
        folder.DrawingFolderId,
        folder.Name,
        parentFolderId = folder.ParentDrawingFolderId
    };

    private static object DrawingRow(Drawing drawing) => new
    {
        drawing.DrawingId,
        drawing.DrawingCode,
        drawing.Title,
        folderId = drawing.DrawingFolderId,
        hasApprovedRevision = drawing.HasApprovedRevision,
        currentApprovedRevisionLabel = drawing.CurrentApprovedRevisionLabel,
        latestFileName = drawing.LatestFileName,
        unapprovedCount = drawing.UnapprovedCount,
        archivedCount = drawing.ArchivedCount
    };
}
