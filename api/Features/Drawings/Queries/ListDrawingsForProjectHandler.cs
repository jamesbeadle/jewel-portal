using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Drawings.Queries;

public sealed class ListDrawingsForProjectHandler
    : IQueryHandler<ListDrawingsForProject, IReadOnlyList<Drawing>>
{
    private readonly JpmsContext context;

    public ListDrawingsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Drawing>> HandleAsync(ListDrawingsForProject query, CancellationToken cancellationToken)
    {
        var drawings = await context.Drawings.AsNoTracking()
            .Where(drawing => drawing.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var drawingIds = drawings.Select(drawing => drawing.DrawingId).ToList();
        var revisionSummaries = await context.DrawingRevisions.AsNoTracking()
            .Where(revision => drawingIds.Contains(revision.DrawingId))
            .Select(revision => new DrawingRevisionRollup.RevisionSummary(
                revision.DrawingId, revision.ApprovalStatus, revision.ReceivedAt, revision.FileName,
                revision.MetadataExtractedAt, revision.AnalysedAt))
            .ToListAsync(cancellationToken);

        var summariesByDrawing = revisionSummaries
            .GroupBy(revision => revision.DrawingId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<DrawingRevisionRollup.RevisionSummary>)group.ToList());

        var result = new List<Drawing>();
        foreach (var drawing in drawings)
        {
            summariesByDrawing.TryGetValue(drawing.DrawingId, out var summaries);
            var rollup = DrawingRevisionRollup.Of(summaries ?? Array.Empty<DrawingRevisionRollup.RevisionSummary>());
            if (query.ApprovedOnly && !rollup.HasApprovedRevision) continue;
            result.Add(drawing.ToModel(rollup));
        }

        // Register order: coded drawings by code, then the uncoded ones by title, then file — so
        // drawings uploaded without a code sit after the coded set, sorted by something a person
        // can see rather than landing in insertion order.
        return result
            .OrderBy(drawing => string.IsNullOrEmpty(drawing.DrawingCode))
            .ThenBy(drawing => drawing.DrawingCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(drawing => drawing.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(drawing => drawing.LatestFileName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}
