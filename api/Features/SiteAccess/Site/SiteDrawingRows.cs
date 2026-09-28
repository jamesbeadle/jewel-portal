using System.Globalization;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings;

namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>Turns the reached drawings and their revisions into the page's rows, folder by folder,
/// named as the register names them.</summary>
public static class SiteDrawingRows
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static IReadOnlyList<SiteDrawingRow> Of(
        IReadOnlyList<DrawingEntity> drawings,
        IReadOnlyList<DrawingRevisionEntity> revisions,
        IReadOnlyList<DrawingFolderEntity> folders)
    {
        var paths = SiteFolderPaths.Of(folders);
        var revisionsByDrawing = revisions.ToLookup(revision => revision.DrawingId);
        var rows = new List<SiteDrawingRow>();
        foreach (var drawing in drawings)
        {
            var shown = SiteDrawingRevisionChoice.Shown(revisionsByDrawing[drawing.DrawingId]);
            if (shown is null) continue;
            rows.Add(RowOf(drawing, shown, paths));
        }
        return rows
            .OrderBy(row => row.FolderPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static SiteDrawingRow RowOf(DrawingEntity drawing, DrawingRevisionEntity shown, IReadOnlyDictionary<string, string> paths)
    {
        var named = drawing.ToModel() with { LatestFileName = shown.FileName };
        return new SiteDrawingRow(
            FolderPathOf(drawing, paths),
            DrawingNaming.Label(named),
            DrawingNaming.RevisionText(shown.RevisionLabel),
            SiteDrawingRevisionChoice.IsApproved(shown),
            shown.ReceivedAt.ToString("dd MMM yyyy", Uk),
            shown.DrawingRevisionId);
    }

    private static string FolderPathOf(DrawingEntity drawing, IReadOnlyDictionary<string, string> paths)
    {
        if (drawing.DrawingFolderId is not { } folderId) return "";
        return paths.TryGetValue(folderId, out var path) ? path : "";
    }
}
