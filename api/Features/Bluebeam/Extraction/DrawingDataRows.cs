using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>
/// The structured read, transcribed into rows (2026-09-16). One call replaces a revision's
/// dimension, callout and shape rows wholesale — exactly as the Revu markup rows are replaced —
/// and stamps RowsWrittenAt, so a re-extraction or a rebuild from the blob leaves the tables
/// saying the same thing as the blob. Shared by the worker's result writer (every successful
/// run) and the rows-only rebuild (revisions extracted before the rows existed). Adds and
/// removes on the context; the caller owns the SaveChanges.
/// </summary>
public static class DrawingDataRows
{
    public static async Task ReplaceAsync(
        JpmsContext context, DrawingExtractionEntity extraction, DrawingStructure structure,
        CancellationToken cancellationToken)
    {
        await RemoveExistingAsync(context, extraction.DrawingRevisionId, cancellationToken);
        context.DrawingDimensions.AddRange(structure.Dimensions.Select(dimension => Dimension(extraction, dimension)));
        context.DrawingCallouts.AddRange(structure.Callouts.Select(callout => Callout(extraction, callout)));
        context.DrawingShapes.AddRange(structure.Shapes.Select(shape => Shape(extraction, shape)));
        extraction.RowsWrittenAt = DateTimeOffset.UtcNow;
    }

    private static async Task RemoveExistingAsync(JpmsContext context, string revisionId, CancellationToken cancellationToken)
    {
        context.DrawingDimensions.RemoveRange(await context.DrawingDimensions
            .Where(row => row.DrawingRevisionId == revisionId).ToListAsync(cancellationToken));
        context.DrawingCallouts.RemoveRange(await context.DrawingCallouts
            .Where(row => row.DrawingRevisionId == revisionId).ToListAsync(cancellationToken));
        context.DrawingShapes.RemoveRange(await context.DrawingShapes
            .Where(row => row.DrawingRevisionId == revisionId).ToListAsync(cancellationToken));
    }

    public static DrawingDimensionEntity Dimension(DrawingExtractionEntity extraction, DrawingDimension dimension) => new()
    {
        DrawingDimensionId = NewId(),
        DrawingExtractionId = extraction.DrawingExtractionId,
        DrawingRevisionId = extraction.DrawingRevisionId,
        DrawingId = extraction.DrawingId,
        ProjectId = extraction.ProjectId,
        Page = dimension.Page,
        ValueMm = dimension.ValueMm,
        Axis = dimension.Axis.Length == 0 ? "?" : dimension.Axis[..1],
        FromX = dimension.From.X, FromY = dimension.From.Y,
        ToX = dimension.To.X, ToY = dimension.To.Y,
        LabelX = dimension.LabelAt.X, LabelY = dimension.LabelAt.Y
    };

    public static DrawingCalloutEntity Callout(DrawingExtractionEntity extraction, DrawingCallout callout) => new()
    {
        DrawingCalloutId = NewId(),
        DrawingExtractionId = extraction.DrawingExtractionId,
        DrawingRevisionId = extraction.DrawingRevisionId,
        DrawingId = extraction.DrawingId,
        ProjectId = extraction.ProjectId,
        Page = callout.Page,
        X = callout.At.X, Y = callout.At.Y,
        Text = callout.Text.Length <= DrawingCalloutEntity.TextLength
            ? callout.Text
            : callout.Text[..DrawingCalloutEntity.TextLength]
    };

    public static DrawingShapeEntity Shape(DrawingExtractionEntity extraction, DrawingShape shape) => new()
    {
        DrawingShapeId = NewId(),
        DrawingExtractionId = extraction.DrawingExtractionId,
        DrawingRevisionId = extraction.DrawingRevisionId,
        DrawingId = extraction.DrawingId,
        ProjectId = extraction.ProjectId,
        Page = shape.Page,
        PointCount = shape.PointCount,
        IsRectangle = shape.IsRectangle,
        HasCurves = shape.HasCurves,
        CentreX = shape.Centre.X, CentreY = shape.Centre.Y,
        WidthMm = shape.WidthMm, HeightMm = shape.HeightMm,
        PerimeterMm = shape.PerimeterMm, AreaSqM = shape.AreaSqM
    };

    private static string NewId() => Guid.NewGuid().ToString("N");
}
