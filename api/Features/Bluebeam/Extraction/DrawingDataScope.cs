using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>One revision whose rows a query may read — with what the reader needs to label it.</summary>
public sealed record DrawingDataSheet(
    string DrawingRevisionId, string DrawingId, string DrawingCode, string Title, string RevisionLabel,
    bool HasRows);

/// <summary>
/// Which revisions a data query reads: ONE revision by id; a document's newest successfully
/// extracted revision; or, project-wide, the newest extracted revision of EVERY document on the
/// project — so a question can be asked across the set without naming each sheet. Only succeeded
/// extractions qualify; HasRows says whether that revision has been transcribed into rows yet
/// (false on one extracted before the row tables existed — the rebuild fills it).
/// </summary>
public static class DrawingDataScope
{
    public static async Task<IReadOnlyList<DrawingDataSheet>> ResolveAsync(
        JpmsContext context, string? revisionId, string? drawingId, string? projectId, CancellationToken cancellationToken)
    {
        var extractions = context.DrawingExtractions.AsNoTracking()
            .Where(row => row.Status == (int)DrawingExtractionStatus.Succeeded);
        if (!string.IsNullOrWhiteSpace(revisionId))
            extractions = extractions.Where(row => row.DrawingRevisionId == revisionId);
        else if (!string.IsNullOrWhiteSpace(drawingId))
            extractions = extractions.Where(row => row.DrawingId == drawingId);
        else
            extractions = extractions.Where(row => row.ProjectId == projectId);

        var candidates = await extractions
            .Join(context.DrawingRevisions.AsNoTracking(),
                extraction => extraction.DrawingRevisionId, revision => revision.DrawingRevisionId,
                (extraction, revision) => new
                {
                    extraction.DrawingRevisionId, extraction.DrawingId, extraction.RowsWrittenAt,
                    revision.RevisionLabel, revision.ReceivedAt
                })
            .ToListAsync(cancellationToken);

        var newestPerDrawing = candidates
            .GroupBy(row => row.DrawingId)
            .Select(group => group.OrderByDescending(row => row.ReceivedAt).First())
            .ToList();
        var drawingIds = newestPerDrawing.Select(row => row.DrawingId).ToList();
        var drawings = await context.Drawings.AsNoTracking()
            .Where(drawing => drawingIds.Contains(drawing.DrawingId))
            .ToDictionaryAsync(drawing => drawing.DrawingId, cancellationToken);

        return newestPerDrawing
            .Select(row => new DrawingDataSheet(
                row.DrawingRevisionId, row.DrawingId,
                drawings.TryGetValue(row.DrawingId, out var drawing) ? drawing.DrawingCode : "",
                drawings.TryGetValue(row.DrawingId, out var titled) ? titled.Title : "",
                row.RevisionLabel, row.RowsWrittenAt is not null))
            .OrderBy(sheet => sheet.DrawingCode).ThenBy(sheet => sheet.Title)
            .ToList();
    }
}
