using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool ListDrawings()
    {
        return new(
            "list_drawings",
            "A project's drawing register: the folder tree (folders nest via parent id) and "
            + "every drawing with its current-revision standing — the approved revision label "
            + "when one is approved (a revision can be approved with a BLANK label, so trust "
            + "hasApprovedRevision, not the label), else the newest revision by file name, plus "
            + "unapproved and archived counts. Pass drawingId instead for that one drawing's "
            + "full revision history with the approval trail (who approved what, when, and what "
            + "was superseded).",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false),
                ("drawingId", "string", "One drawing's full revision history instead of the register.", false)),
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
