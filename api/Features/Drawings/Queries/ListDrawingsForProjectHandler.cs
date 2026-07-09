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
        var drawings = await context.Drawings
            .Where(drawing => drawing.ProjectId == query.ProjectId)
            .OrderBy(drawing => drawing.DrawingCode)
            .ToListAsync(cancellationToken);

        var drawingIds = drawings.Select(drawing => drawing.DrawingId).ToList();
        var revisionStatuses = await context.DrawingRevisions
            .Where(revision => drawingIds.Contains(revision.DrawingId))
            .Select(revision => new
            {
                revision.DrawingId,
                revision.ApprovalStatus,
                revision.ReceivedAt,
                revision.MetadataExtractedAt,
                revision.AnalysedAt
            })
            .ToListAsync(cancellationToken);

        var byDrawing = revisionStatuses
            .GroupBy(revision => revision.DrawingId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var result = new List<Drawing>();
        foreach (var drawing in drawings)
        {
            byDrawing.TryGetValue(drawing.DrawingId, out var statuses);
            statuses ??= new();

            var unapproved = statuses.Count(status => status.ApprovalStatus == (int)DrawingApprovalStatus.Unapproved);
            var archived = statuses.Count(status => status.ApprovalStatus == (int)DrawingApprovalStatus.Archived);
            var hasApproved = statuses.Any(status => status.ApprovalStatus == (int)DrawingApprovalStatus.Approved);

            if (query.ApprovedOnly && !hasApproved) continue;

            // Pipeline status rolls up from the LATEST revision — that's the one the register
            // cares about: has the newest issue been extracted and analysed yet?
            var latest = statuses.OrderByDescending(status => status.ReceivedAt).FirstOrDefault();
            result.Add(drawing.ToModel(unapproved, archived,
                latest?.MetadataExtractedAt, latest?.AnalysedAt));
        }

        return result.AsReadOnly();
    }
}
