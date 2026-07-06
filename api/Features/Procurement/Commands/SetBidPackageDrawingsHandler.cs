using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Drawings;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Wholesale-replaces the package's linked drawings, keeping existing rows (and their LinkedAt) for
// drawings that stay. Only drawings belonging to the package's project may be linked.
public sealed class SetBidPackageDrawingsHandler
    : ICommandHandler<SetBidPackageDrawings, IReadOnlyList<Drawing>>
{
    private readonly JpmsContext context;

    public SetBidPackageDrawingsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<Drawing>> HandleAsync(SetBidPackageDrawings command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        var wanted = command.DrawingIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Guard: every linked drawing must live on the package's project.
        var valid = await context.Drawings
            .Where(d => d.ProjectId == package.ProjectId && wanted.Contains(d.DrawingId))
            .Select(d => d.DrawingId)
            .ToListAsync(cancellationToken);
        var validSet = valid.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await context.BidPackageDrawings
            .Where(link => link.BidPackageId == command.BidPackageId)
            .ToListAsync(cancellationToken);

        context.BidPackageDrawings.RemoveRange(
            existing.Where(link => !validSet.Contains(link.DrawingId)));

        var already = existing.Select(link => link.DrawingId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var drawingId in valid.Where(id => !already.Contains(id)))
        {
            context.BidPackageDrawings.Add(new BidPackageDrawingEntity
            {
                BidPackageDrawingId = Guid.NewGuid().ToString("N"),
                BidPackageId = command.BidPackageId,
                DrawingId = drawingId,
                LinkedAt = DateTimeOffset.UtcNow
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return await LoadLinkedAsync(command.BidPackageId, cancellationToken);
    }

    private async Task<IReadOnlyList<Drawing>> LoadLinkedAsync(string bidPackageId, CancellationToken cancellationToken)
    {
        var drawings = await (
            from link in context.BidPackageDrawings
            where link.BidPackageId == bidPackageId
            join drawing in context.Drawings on link.DrawingId equals drawing.DrawingId
            orderby link.LinkedAt descending
            select drawing)
            .ToListAsync(cancellationToken);
        return drawings.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
